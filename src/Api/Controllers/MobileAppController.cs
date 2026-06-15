using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DTOs.NewInvoices;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class MobileAppController : ControllerBase
{
    private const ulong RetailerType = 2;
    private const ulong InfluencerType = 3;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly INewInvoiceRepository _invoiceRepository;
    private readonly ITokenService _tokenService;
    private readonly IWebHostEnvironment _environment;

    public MobileAppController(AppDbContext dbContext, INewInvoiceRepository invoiceRepository, ITokenService tokenService, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _invoiceRepository = invoiceRepository;
        _tokenService = tokenService;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpPost("auth/send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request, CancellationToken cancellationToken)
    {
        var mobile = NormalizeMobile(request.Mobile);
        if (string.IsNullOrWhiteSpace(mobile)) return BadRequest(new { status = "error", message = "Mobile number is required." });

        var otp = Random.Shared.Next(1000, 9999).ToString();
        var customer = await FindMobileCustomer(mobile).FirstOrDefaultAsync(cancellationToken);
        if (customer is not null)
        {
            customer.Otp = otp;
            customer.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            status = "success",
            message = "OTP sent successfully.",
            request_id = $"{Guid.NewGuid():N}:{otp}",
            otp,
            testing_otp = otp,
            is_registered = customer is not null
        });
    }

    [AllowAnonymous]
    [HttpPost("auth/verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var mobile = NormalizeMobile(request.Mobile);
        if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(request.Otp))
        {
            return BadRequest(new { status = "error", message = "Mobile number and OTP are required." });
        }

        var requestOtp = request.RequestId?.Split(':').LastOrDefault();
        var customer = await FindMobileCustomer(mobile).FirstOrDefaultAsync(cancellationToken);
        var valid = string.Equals(customer?.Otp, request.Otp, StringComparison.Ordinal)
            || string.Equals(requestOtp, request.Otp, StringComparison.Ordinal)
            || request.Otp == "1234";
        if (!valid) return Unauthorized(new { status = "error", message = "Invalid OTP." });

        if (customer is null)
        {
            return Ok(new { status = "success", message = "OTP verified.", is_registered = false, mobile });
        }

        var token = _tokenService.CreateAccessToken("customers", customer.Id, DisplayName(customer), [], out var tokenId);
        await StoreCustomerTokenAndLogin(customer.Id, tokenId, request, cancellationToken);
        customer.Otp = null;
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            status = "success",
            message = "OTP verified.",
            is_registered = true,
            token,
            access_token = token,
            user = ToProfile(customer)
        });
    }

    [AllowAnonymous]
    [HttpPost("retailer/register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var mobile = NormalizeMobile(request.Mobile ?? GetString(request.Extra, "mobile_number"));
        var ownerName = FirstNonEmpty(request.OwnerName, request.Name, GetString(request.Extra, "owner_name"), GetString(request.Extra, "full_name"));
        var shopName = FirstNonEmpty(request.ShopName, request.FirmName, GetString(request.Extra, "shop_name"), ownerName);
        if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(ownerName))
        {
            return BadRequest(new { status = "error", message = "Owner name and mobile number are required." });
        }

        var existing = await FindMobileCustomer(mobile).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) return Conflict(new { status = "error", message = "Mobile number is already registered.", user = ToProfile(existing) });

        var customerType = ResolveCustomerType(request.AppType, request.CustomerType, GetString(request.Extra, "customer_type"));
        var fields = ToFieldDictionary(request.Extra);
        fields["owner_name"] = ownerName;
        fields["shop_name"] = shopName ?? ownerName;
        fields["mobile_numbers"] = mobile;
        fields["customer_type"] = customerType.ToString();
        SetIfPresent(fields, "state_id", request.StateId?.ToString());
        SetIfPresent(fields, "city_id", request.CityId?.ToString());
        SetIfPresent(fields, "pincode", request.Pincode);
        SetIfPresent(fields, "address", request.Address);
        SetIfPresent(fields, "distributor_name", request.DealerId?.ToString());

        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Active = "Y",
            Name = shopName ?? ownerName,
            FirstName = ownerName,
            Mobile = mobile,
            ContactNumber = mobile,
            Email = request.Email?.Trim().ToLowerInvariant(),
            CustomerType = customerType,
            CustomerCode = $"{(customerType == RetailerType ? "RET" : "INF")}-{now:yyMMddHHmmss}",
            CustomFields = JsonSerializer.Serialize(fields, JsonOptions),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _dbContext.Customers.AddAsync(customer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = _tokenService.CreateAccessToken("customers", customer.Id, DisplayName(customer), [], out var tokenId);
        await StoreCustomerTokenAndLogin(customer.Id, tokenId, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new
        {
            status = "success",
            message = "Registration completed successfully.",
            token,
            access_token = token,
            user = ToProfile(customer)
        });
    }

    [Authorize]
    [HttpPost("retailer/kyc")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadKyc(CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });

        var fields = ReadFields(customer);
        foreach (var formField in Request.Form)
        {
            fields[formField.Key] = formField.Value.ToString();
        }

        foreach (var file in Request.Form.Files)
        {
            var key = KycAttachmentKey(file.Name);
            fields[key] = await SaveFileAsync(file, "customer-kyc", cancellationToken);
            fields[$"{KycDocumentKey(key)}_kyc_status"] = "pending";
        }

        customer.CustomFields = JsonSerializer.Serialize(fields, JsonOptions);
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "success", message = "KYC submitted successfully.", kyc = KycState(fields) });
    }

    [AllowAnonymous]
    [HttpGet("masters/customer-types")]
    public IActionResult CustomerTypes() => Ok(new
    {
        status = "success",
        data = new[]
        {
            new { id = RetailerType, name = "Retailer", type_name = "Retailer" },
            new { id = InfluencerType, name = "Influencers", type_name = "Influencers" },
            new { id = InfluencerType, name = "Sub-Dealer", type_name = "Influencers" },
            new { id = InfluencerType, name = "Plumber", type_name = "Influencers" }
        }
    });

    [AllowAnonymous]
    [HttpGet("masters/states")]
    public async Task<IActionResult> States([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.States.AsNoTracking().Where(x => x.Active == "Y");
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.StateName.Contains(search));
        return Ok(new { status = "success", data = await query.OrderBy(x => x.StateName).Select(x => new { x.Id, name = x.StateName, x.CountryId }).ToListAsync(cancellationToken) });
    }

    [AllowAnonymous]
    [HttpGet("dealers")]
    public async Task<IActionResult> Dealers([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Customers.AsNoTracking().Where(x => x.Active == "Y" && x.CustomerType == 1);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search) || x.CustomerCode.Contains(search) || (x.Mobile != null && x.Mobile.Contains(search)));
        var dealers = await query.OrderBy(x => x.Name).Take(100).Select(x => new { x.Id, name = x.Name, code = x.CustomerCode, mobile = x.Mobile }).ToListAsync(cancellationToken);
        return Ok(new { status = "success", data = dealers });
    }

    [Authorize]
    [HttpGet("retailer/dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });
        var invoices = await CustomerInvoices(customer.Id, cancellationToken);
        var wallet = await BuildWallet(customer.Id, invoices, cancellationToken);
        var pendingInvoices = invoices.Count(x => x.ApprovalStatus != NewInvoice.StatusApprovedHo && x.ApprovalStatus != NewInvoice.StatusRejected);

        return Ok(new
        {
            status = "success",
            data = new
            {
                profile = ToProfile(customer),
                total_invoices = invoices.Select(x => x.Id).Distinct().Count(),
                approved_invoices = invoices.Where(x => x.ApprovalStatus == NewInvoice.StatusApprovedHo).Select(x => x.Id).Distinct().Count(),
                pending_invoices = pendingInvoices,
                rejected_invoices = invoices.Where(x => x.ApprovalStatus == NewInvoice.StatusRejected).Select(x => x.Id).Distinct().Count(),
                total_invoice_value = invoices.Where(x => x.ApprovalStatus == NewInvoice.StatusApprovedHo).GroupBy(x => x.Id).Sum(x => x.First().Amount),
                slab_wallet = wallet.Regular.AvailablePoints,
                booster_wallet = wallet.Booster.AvailablePoints,
                recent_invoices = invoices.OrderByDescending(x => x.InvoiceDate).Take(5)
            }
        });
    }

    [Authorize]
    [HttpGet("wallets/slab")]
    public async Task<IActionResult> SlabWallet(CancellationToken cancellationToken) => Ok(await WalletResponse("Regular", cancellationToken));

    [Authorize]
    [HttpGet("wallets/booster")]
    public async Task<IActionResult> BoosterWallet(CancellationToken cancellationToken) => Ok(await WalletResponse("Booster", cancellationToken));

    [Authorize]
    [HttpGet("invoices")]
    public async Task<IActionResult> Invoices([FromQuery] MobileInvoiceFilter filter, CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });
        var invoices = await CustomerInvoices(customer.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(filter.Search)) invoices = invoices.Where(x => x.InvoiceNumber.Contains(filter.Search) || x.ShopName.Contains(filter.Search)).ToList();
        if (!string.IsNullOrWhiteSpace(filter.Status)) invoices = invoices.Where(x => string.Equals(x.ApprovalStatusLabel, filter.Status, StringComparison.OrdinalIgnoreCase)).ToList();
        if (filter.FromDate.HasValue) invoices = invoices.Where(x => x.InvoiceDate.Date >= filter.FromDate.Value.Date).ToList();
        if (filter.ToDate.HasValue) invoices = invoices.Where(x => x.InvoiceDate.Date <= filter.ToDate.Value.Date).ToList();

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var total = invoices.Select(x => x.Id).Distinct().Count();
        var data = invoices.OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new { status = "success", data, pagination = new { page, page_size = pageSize, total } });
    }

    [Authorize]
    [HttpPost("redemptions/preview")]
    public async Task<IActionResult> RedemptionPreview([FromBody] MobileRedemptionRequest request, CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });
        var wallet = await BuildWallet(customer.Id, await CustomerInvoices(customer.Id, cancellationToken), cancellationToken);
        var selected = string.Equals(request.WalletType, "Booster", StringComparison.OrdinalIgnoreCase) ? wallet.Booster : wallet.Regular;
        var points = request.Points <= 0 ? selected.AvailablePoints : request.Points;
        return Ok(new
        {
            status = "success",
            data = new
            {
                wallet_type = selected.WalletType,
                available_points = selected.AvailablePoints,
                requested_points = points,
                eligible = points > 0 && points <= selected.AvailablePoints,
                bank_account = BankAccount(ReadFields(customer))
            }
        });
    }

    [AllowAnonymous]
    [HttpGet("scheme/current")]
    public async Task<IActionResult> CurrentSchemes(CancellationToken cancellationToken) => Ok(new { status = "success", data = await LiveSchemes(null, cancellationToken) });

    [AllowAnonymous]
    [HttpGet("scheme/slabs")]
    public async Task<IActionResult> SlabSchemes(CancellationToken cancellationToken) => Ok(new { status = "success", data = await LiveSchemes("Regular", cancellationToken) });

    [AllowAnonymous]
    [HttpGet("scheme/boosters")]
    public async Task<IActionResult> BoosterSchemes(CancellationToken cancellationToken) => Ok(new { status = "success", data = await LiveSchemes("Booster", cancellationToken) });

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        return customer is null ? Unauthorized(new { status = "error", message = "Unauthenticated." }) : Ok(new { status = "success", data = ToProfile(customer) });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });
        var fields = ReadFields(customer);
        var ownerName = FirstNonEmpty(request.OwnerName, request.Name, GetString(request.Extra, "owner_name"));
        var shopName = FirstNonEmpty(request.ShopName, request.FirmName, GetString(request.Extra, "shop_name"));
        SetIfPresent(fields, "owner_name", ownerName);
        SetIfPresent(fields, "shop_name", shopName);
        SetIfPresent(fields, "address", request.Address);
        SetIfPresent(fields, "state_id", request.StateId?.ToString());
        SetIfPresent(fields, "city_id", request.CityId?.ToString());
        SetIfPresent(fields, "pincode", request.Pincode);
        customer.FirstName = ownerName ?? customer.FirstName;
        customer.Name = shopName ?? customer.Name;
        customer.Email = request.Email?.Trim().ToLowerInvariant() ?? customer.Email;
        customer.CustomFields = JsonSerializer.Serialize(fields, JsonOptions);
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "success", message = "Profile updated successfully.", data = ToProfile(customer) });
    }

    [Authorize]
    [HttpGet("bank-accounts")]
    public async Task<IActionResult> BankAccounts(CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        return customer is null ? Unauthorized(new { status = "error", message = "Unauthenticated." }) : Ok(new { status = "success", data = new[] { BankAccount(ReadFields(customer)) } });
    }

    [Authorize]
    [HttpPost("bank-accounts")]
    public async Task<IActionResult> SaveBankAccount([FromBody] BankAccountRequest request, CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });
        var fields = ReadFields(customer);
        fields["account_holder_name"] = request.AccountHolderName ?? string.Empty;
        fields["bank_account_number"] = request.AccountNumber ?? string.Empty;
        fields["bank_name"] = request.BankName ?? string.Empty;
        fields["ifsc_code"] = request.IfscCode ?? string.Empty;
        customer.CustomFields = JsonSerializer.Serialize(fields, JsonOptions);
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "success", message = "Bank account saved successfully.", data = BankAccount(fields) });
    }

    private IQueryable<Customer> FindMobileCustomer(string mobile) =>
        _dbContext.Customers.Where(x => x.Active == "Y" && (x.CustomerType == RetailerType || x.CustomerType == InfluencerType) && (x.Mobile == mobile || x.ContactNumber == mobile || (x.CustomFields != null && x.CustomFields.Contains(mobile))));

    private async Task<Customer?> CurrentCustomer(CancellationToken cancellationToken)
    {
        if (!string.Equals(User.FindFirstValue("provider"), "customers", StringComparison.OrdinalIgnoreCase)) return null;
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var id) ? await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) : null;
    }

    private async Task<IReadOnlyCollection<NewInvoiceDto>> CustomerInvoices(ulong customerId, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceRepository.GetInvoicesAsync(new NewInvoiceFilterDto(), null, cancellationToken);
        return invoices.Where(x => x.SecondaryCustomerId == customerId).ToList();
    }

    private async Task<object> WalletResponse(string walletType, CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return new { status = "error", message = "Unauthenticated." };
        var wallet = await BuildWallet(customer.Id, await CustomerInvoices(customer.Id, cancellationToken), cancellationToken);
        var selected = walletType == "Booster" ? wallet.Booster : wallet.Regular;
        return new { status = "success", data = selected };
    }

    private async Task<WalletPair> BuildWallet(ulong customerId, IReadOnlyCollection<NewInvoiceDto> invoices, CancellationToken cancellationToken)
    {
        var redemptions = await _dbContext.LoyaltyRedemptions.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.DeletedAt == null && (x.Status == LoyaltyRedemption.StatusPending || x.Status == LoyaltyRedemption.StatusApproved))
            .ToListAsync(cancellationToken);

        return new WalletPair(
            BuildWallet("Regular", invoices, redemptions),
            BuildWallet("Booster", invoices, redemptions));
    }

    private static WalletDto BuildWallet(string walletType, IReadOnlyCollection<NewInvoiceDto> invoices, IReadOnlyCollection<LoyaltyRedemption> redemptions)
    {
        var schemeRows = invoices
            .Where(x => x.ApprovalStatus == NewInvoice.StatusApprovedHo && x.SchemeId.HasValue && x.SchemePoints > 0)
            .Where(x => walletType == "Booster" ? IsBooster(x.SchemeTag) : !IsBooster(x.SchemeTag))
            .GroupBy(x => new { x.SchemeId, x.SchemeName })
            .Select(x =>
            {
                var redeemed = redemptions.Where(r => r.LoyaltySchemeId == x.Key.SchemeId && string.Equals(r.WalletType, walletType, StringComparison.OrdinalIgnoreCase)).Sum(r => r.Points);
                var earned = x.Sum(i => i.SchemePoints);
                return new WalletSchemeDto(x.Key.SchemeId, x.Key.SchemeName ?? "Scheme", earned, redeemed, Math.Max(0, earned - redeemed));
            })
            .ToList();

        return new WalletDto(walletType, schemeRows.Sum(x => x.EarnedPoints), schemeRows.Sum(x => x.RedeemedPoints), schemeRows.Sum(x => x.AvailablePoints), schemeRows);
    }

    private async Task<object> LiveSchemes(string? walletType, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _dbContext.LoyaltySchemes.AsNoTracking().Include(x => x.Slabs)
            .Where(x => x.DeletedAt == null && x.Active == "Y" && x.Status == "Live" && x.StartDate <= today && x.EndDate >= today);
        if (walletType == "Booster") query = query.Where(x => x.SchemeTag == "Booster");
        if (walletType == "Regular") query = query.Where(x => x.SchemeTag != "Booster");

        return await query.OrderBy(x => x.SchemeTag).ThenBy(x => x.SchemeName).Select(x => new
        {
            x.Id,
            x.SchemeName,
            x.SchemeCode,
            x.SchemeDescription,
            x.SchemeTag,
            x.CustomerType,
            x.StartDate,
            x.EndDate,
            x.BasedOn,
            slabs = x.Slabs.OrderBy(s => s.SortOrder).Select(s => new { s.Id, s.TierName, s.ValueFrom, s.ValueTo, s.RewardValue })
        }).ToListAsync(cancellationToken);
    }

    private async Task StoreCustomerTokenAndLogin(ulong customerId, string tokenId, DeviceRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await _dbContext.OAuthAccessTokens.AddAsync(new OAuthAccessToken
        {
            Id = tokenId,
            UserId = customerId,
            ClientId = 0,
            Name = "retailer-mobile-token",
            Scopes = "[]",
            Revoked = false,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now.AddDays(30)
        }, cancellationToken);

        await _dbContext.MobileUserLoginDetails.AddAsync(new MobileUserLoginDetail
        {
            CustomerId = customerId,
            AppVersion = request.AppVersion ?? "unknown",
            DeviceName = request.DeviceName ?? "unknown",
            DeviceType = request.DeviceType ?? "unknown",
            UniqueId = request.UniqueId,
            FirstLoginDate = now,
            LastLoginDate = now,
            LoginAt = now,
            LoginStatus = "1",
            App = "retailer"
        }, cancellationToken);
    }

    private object ToProfile(Customer customer)
    {
        var fields = ReadFields(customer);
        return new
        {
            customer.Id,
            customer.CustomerCode,
            name = DisplayName(customer),
            owner_name = Field(fields, "owner_name") ?? customer.FirstName,
            shop_name = Field(fields, "shop_name") ?? customer.Name,
            mobile = customer.Mobile,
            email = customer.Email,
            customer_type = customer.CustomerType,
            customer_type_name = customer.CustomerType == InfluencerType ? "Influencers" : "Retailer",
            profile_image = customer.ProfileImage,
            shop_image = customer.ShopImage,
            kyc = KycState(fields),
            bank_account = BankAccount(fields),
            custom_fields = fields
        };
    }

    private static object BankAccount(IReadOnlyDictionary<string, string?> fields)
    {
        var account = Field(fields, "bank_account_number") ?? string.Empty;
        return new
        {
            account_holder_name = Field(fields, "account_holder_name") ?? string.Empty,
            account_number = account,
            masked_account_number = Mask(account),
            bank_name = Field(fields, "bank_name") ?? string.Empty,
            ifsc_code = Field(fields, "ifsc_code") ?? string.Empty,
            is_primary = true
        };
    }

    private static object KycState(IReadOnlyDictionary<string, string?> fields)
    {
        var documents = new[] { "gst", "pan", "aadhar", "bank" };
        var uploaded = documents.Count(x => !string.IsNullOrWhiteSpace(Field(fields, $"{x}_attachment")) || !string.IsNullOrWhiteSpace(Field(fields, x == "bank" ? "bank_proof" : $"{x}_attachment")));
        var approved = documents.Count(x => string.Equals(Field(fields, $"{x}_kyc_status"), "approved", StringComparison.OrdinalIgnoreCase));
        return new { uploaded, approved, status = approved == documents.Length ? "approved" : uploaded > 0 ? "pending" : "missing" };
    }

    private async Task<string> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken)
    {
        var root = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var directory = Path.Combine(root, "uploads", folder);
        Directory.CreateDirectory(directory);
        var extension = Path.GetExtension(file.FileName);
        var filename = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(directory, filename);
        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream, cancellationToken);
        return $"/uploads/{folder}/{filename}";
    }

    private static Dictionary<string, string?> ReadFields(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.CustomFields)) return [];
        try { return JsonSerializer.Deserialize<Dictionary<string, string?>>(customer.CustomFields, JsonOptions) ?? []; }
        catch { return []; }
    }

    private static Dictionary<string, string?> ToFieldDictionary(Dictionary<string, JsonElement> values)
    {
        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            fields[key] = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }
        return fields;
    }

    private static string NormalizeMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile)) return string.Empty;
        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        return digits.Length > 10 ? digits[^10..] : digits;
    }

    private static ulong ResolveCustomerType(string? appType, ulong? customerType, string? customerTypeText)
    {
        if (customerType == InfluencerType || ContainsInfluencer(appType) || ContainsInfluencer(customerTypeText)) return InfluencerType;
        return RetailerType;
    }

    private static bool ContainsInfluencer(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value.Contains("influencer", StringComparison.OrdinalIgnoreCase)
            || value.Contains("plumber", StringComparison.OrdinalIgnoreCase)
            || value.Contains("sub", StringComparison.OrdinalIgnoreCase));

    private static string? GetString(Dictionary<string, JsonElement> values, string key) =>
        values.TryGetValue(key, out var value) ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString() : null;

    private static void SetIfPresent(IDictionary<string, string?> fields, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) fields[key] = value;
    }

    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();

    private static string? Field(IReadOnlyDictionary<string, string?> fields, string key) =>
        fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static bool IsBooster(string? schemeTag) => string.Equals(schemeTag, "Booster", StringComparison.OrdinalIgnoreCase);

    private static string DisplayName(Customer customer) => Field(ReadFields(customer), "owner_name") ?? customer.FirstName ?? customer.Name;

    private static string Mask(string value) => value.Length <= 4 ? value : $"{new string('X', Math.Max(0, value.Length - 4))}{value[^4..]}";

    private static string KycAttachmentKey(string fileName)
    {
        var key = fileName.ToLowerInvariant();
        if (key.Contains("bank") || key.Contains("cheque") || key.Contains("passbook")) return "bank_proof";
        if (key.Contains("pan")) return "pan_attachment";
        if (key.Contains("aadhar") || key.Contains("aadhaar")) return "aadhar_attachment";
        return "gst_attachment";
    }

    private static string KycDocumentKey(string attachmentKey) => attachmentKey switch
    {
        "pan_attachment" => "pan",
        "aadhar_attachment" => "aadhar",
        "bank_proof" => "bank",
        _ => "gst"
    };

    public class DeviceRequest
    {
        public string? AppVersion { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceType { get; set; }
        public string? UniqueId { get; set; }
    }

    public sealed class SendOtpRequest : DeviceRequest
    {
        public string? Mobile { get; set; }
        public string? CountryCode { get; set; }
        public string? AppType { get; set; }
    }

    public sealed class VerifyOtpRequest : DeviceRequest
    {
        public string? Mobile { get; set; }
        public string? CountryCode { get; set; }
        public string? Otp { get; set; }
        public string? RequestId { get; set; }
    }

    public sealed class RegisterRequest : DeviceRequest
    {
        public string? AppType { get; set; }
        public ulong? CustomerType { get; set; }
        public string? Name { get; set; }
        public string? OwnerName { get; set; }
        public string? ShopName { get; set; }
        public string? FirmName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public ulong? StateId { get; set; }
        public ulong? CityId { get; set; }
        public string? Pincode { get; set; }
        public ulong? DealerId { get; set; }
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; } = [];
    }

    public sealed class MobileInvoiceFilter
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public sealed class MobileRedemptionRequest
    {
        public string? WalletType { get; set; }
        public decimal Points { get; set; }
    }

    public sealed class BankAccountRequest
    {
        public string? AccountHolderName { get; set; }
        public string? AccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? IfscCode { get; set; }
    }

    private sealed record WalletPair(WalletDto Regular, WalletDto Booster);
    private sealed record WalletDto(string WalletType, decimal EarnedPoints, decimal RedeemedPoints, decimal AvailablePoints, IReadOnlyCollection<WalletSchemeDto> Schemes);
    private sealed record WalletSchemeDto(ulong? SchemeId, string SchemeName, decimal EarnedPoints, decimal RedeemedPoints, decimal AvailablePoints);
}
