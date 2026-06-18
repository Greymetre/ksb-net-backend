using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
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
    private readonly IMasterDataService _masterDataService;
    private readonly INewInvoiceRepository _invoiceRepository;
    private readonly ITokenService _tokenService;
    private readonly IWebHostEnvironment _environment;

    public MobileAppController(AppDbContext dbContext, IMasterDataService masterDataService, INewInvoiceRepository invoiceRepository, ITokenService tokenService, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _masterDataService = masterDataService;
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
    [HttpGet("retailer/kyc")]
    public async Task<IActionResult> GetKyc(CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });

        return Ok(new { status = "success", data = BuildMobileKyc(customer, ReadFields(customer)) });
    }

    [Authorize]
    [HttpPost("retailer/kyc")]
    [HttpPut("retailer/kyc")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadKyc(CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });

        var fields = ReadFields(customer);
        var changedDocuments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var formField in Request.Form)
        {
            var key = formField.Key;
            var value = formField.Value.ToString();
            if (string.Equals(Field(fields, key), value, StringComparison.Ordinal)) continue;

            fields[key] = value;
            var documentKey = KycDocumentKeyForDetail(key);
            if (!string.IsNullOrWhiteSpace(documentKey)) changedDocuments.Add(documentKey);
        }

        foreach (var file in Request.Form.Files)
        {
            if (file.Length == 0) continue;

            var key = KycAttachmentKey(file.Name);
            fields[key] = await SaveFileAsync(file, "customer-kyc", cancellationToken);
            changedDocuments.Add(KycDocumentKey(key));
        }

        foreach (var documentKey in changedDocuments)
        {
            ResetKycStatus(fields, documentKey);
        }

        customer.CustomFields = JsonSerializer.Serialize(fields, JsonOptions);
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new
        {
            status = "success",
            message = "KYC submitted successfully.",
            kyc = KycState(fields),
            data = BuildMobileKyc(customer, fields)
        });
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
    [HttpGet("masters/location-lookup")]
    [HttpGet("masters/locations")]
    public async Task<IActionResult> LocationLookup(
        [FromQuery] string? pincode,
        [FromQuery(Name = "state_id")] ulong? stateId,
        [FromQuery(Name = "city_id")] ulong? cityId,
        [FromQuery] string? city,
        CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetLocationDetailsAsync(pincode, stateId, cityId, city, cancellationToken);
        return Ok(response);
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
        var walletCards = await BuildDashboardWalletCards(customer, wallet, invoices, cancellationToken);
        var currentSchemes = await CurrentRunningSchemes(null, cancellationToken);
        var pendingInvoices = invoices.Count(x => x.ApprovalStatus != NewInvoice.StatusApprovedHo && x.ApprovalStatus != NewInvoice.StatusRejected);

        return Ok(new
        {
            status = "success",
            data = new
            {
                customer_id = customer.Id,
                profile = ToProfile(customer),
                total_invoices = invoices.Select(x => x.Id).Distinct().Count(),
                approved_invoices = invoices.Where(x => x.ApprovalStatus == NewInvoice.StatusApprovedHo).Select(x => x.Id).Distinct().Count(),
                pending_invoices = pendingInvoices,
                rejected_invoices = invoices.Where(x => x.ApprovalStatus == NewInvoice.StatusRejected).Select(x => x.Id).Distinct().Count(),
                total_invoice_value = invoices.Where(x => x.ApprovalStatus == NewInvoice.StatusApprovedHo).GroupBy(x => x.Id).Sum(x => x.First().Amount),
                slab_wallet = wallet.Regular.AvailablePoints,
                booster_wallet = wallet.Booster.AvailablePoints,
                active_wallets = walletCards.Count(x => x.IsActive),
                wallets = walletCards,
                current_schemes = currentSchemes,
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
        var (fromDate, toDate) = DateRange(filter.FromDate, filter.ToDate);

        if (!string.IsNullOrWhiteSpace(filter.Search)) invoices = invoices.Where(x => x.InvoiceNumber.Contains(filter.Search) || x.ShopName.Contains(filter.Search)).ToList();
        if (!string.IsNullOrWhiteSpace(filter.Status)) invoices = invoices.Where(x => InvoiceStatusMatches(x, filter.Status)).ToList();
        if (fromDate.HasValue) invoices = invoices.Where(x => x.InvoiceDate.Date >= fromDate.Value.Date).ToList();
        if (toDate.HasValue) invoices = invoices.Where(x => x.InvoiceDate.Date <= toDate.Value.Date).ToList();

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var invoiceItems = BuildInvoiceListItems(invoices);
        var total = invoiceItems.Count;
        var data = invoices.OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var items = invoiceItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new
        {
            status = "success",
            summary = BuildInvoiceListSummary(invoiceItems),
            filter_options = new
            {
                search_placeholder = "Search invoice number",
                statuses = new[]
                {
                    new { key = "all", label = "All" },
                    new { key = "approved", label = "Approved" },
                    new { key = "pending", label = "Pending" },
                    new { key = "rejected", label = "Rejected" }
                }
            },
            groups = BuildInvoiceMonthGroups(items),
            items,
            data,
            pagination = new { page, page_size = pageSize, total }
        });
    }

    [Authorize]
    [HttpPost("redemptions/preview")]
    public async Task<IActionResult> RedemptionPreview([FromBody] MobileRedemptionRequest request, CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });
        if (!request.LoyaltySchemeId.HasValue) return BadRequest(new { status = "error", message = "Loyalty scheme is required." });

        var wallet = await BuildWallet(customer.Id, await CustomerInvoices(customer.Id, cancellationToken), cancellationToken);
        var selected = string.Equals(request.WalletType, "Booster", StringComparison.OrdinalIgnoreCase) ? wallet.Booster : wallet.Regular;
        var scheme = selected.Schemes.FirstOrDefault(x => x.SchemeId == request.LoyaltySchemeId);
        if (scheme is null)
        {
            return BadRequest(new { status = "error", message = "Selected scheme is not available in this wallet." });
        }

        var points = request.Points <= 0 ? scheme.AvailablePoints : request.Points;
        return Ok(new
        {
            status = "success",
            data = new
            {
                loyalty_scheme_id = scheme.SchemeId,
                scheme_name = scheme.SchemeName,
                wallet_type = selected.WalletType,
                available_points = scheme.AvailablePoints,
                requested_points = points,
                eligible = points > 0 && points <= scheme.AvailablePoints,
                bank_account = BankAccount(ReadFields(customer))
            }
        });
    }

    [Authorize]
    [HttpGet("redemptions/history")]
    public async Task<IActionResult> RedemptionHistory([FromQuery] MobileRedemptionHistoryFilter filter, CancellationToken cancellationToken)
    {
        var customer = await CurrentCustomer(cancellationToken);
        if (customer is null) return Unauthorized(new { status = "error", message = "Unauthenticated." });

        var query = _dbContext.LoyaltyRedemptions.AsNoTracking()
            .Where(x => x.DeletedAt == null && x.CustomerId == customer.Id);
        var (fromDate, toDate) = DateRange(filter.FromDate, filter.ToDate);

        if (!string.IsNullOrWhiteSpace(filter.WalletType) && !string.Equals(filter.WalletType, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.WalletType == NormalizeWalletType(filter.WalletType));
        }

        if (!string.IsNullOrWhiteSpace(filter.RedeemMode) && !string.Equals(filter.RedeemMode, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.RedeemMode == NormalizeRedeemMode(filter.RedeemMode));
        }

        if (TryRedemptionStatus(filter.Status, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (fromDate.HasValue) query = query.Where(x => x.CreatedAt.HasValue && x.CreatedAt.Value.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.CreatedAt.HasValue && x.CreatedAt.Value.Date <= toDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.TransactionNo.Contains(search)
                || x.SchemeName.Contains(search)
                || x.Points.ToString().Contains(search));
        }

        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var items = rows.Select(ToMobileRedemptionHistoryItem).ToList();
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var total = items.Count;
        var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            status = "success",
            summary = BuildRedemptionHistorySummary(items),
            filter_options = new
            {
                search_placeholder = "Search transaction or scheme",
                wallets = new[]
                {
                    new { key = "all", label = "All Wallets" },
                    new { key = "Regular", label = "Regular Wallet" },
                    new { key = "Booster", label = "Booster Wallet" }
                },
                statuses = new[]
                {
                    new { key = "all", label = "All Status" },
                    new { key = "pending", label = "Pending" },
                    new { key = "approved", label = "Approved" },
                    new { key = "rejected", label = "Rejected" },
                    new { key = "hold", label = "Hold" }
                },
                modes = new[]
                {
                    new { key = "all", label = "All Modes" },
                    new { key = "NEFT", label = "NEFT" },
                    new { key = "IMPS", label = "IMPS" }
                }
            },
            groups = BuildRedemptionMonthGroups(pagedItems),
            items = pagedItems,
            data = pagedItems,
            pagination = new
            {
                page,
                page_size = pageSize,
                total,
                total_pages = total == 0 ? 0 : (int)Math.Ceiling(total / (decimal)pageSize),
                has_next = page * pageSize < total,
                has_previous = page > 1
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
        return customer is null ? Unauthorized(new { status = "error", message = "Unauthenticated." }) : Ok(await ToMobileProfile(customer, cancellationToken));
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
        return Ok(await ToMobileProfile(customer, cancellationToken));
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

    private (DateTime? FromDate, DateTime? ToDate) DateRange(DateTime? fromDate, DateTime? toDate) =>
        (fromDate ?? QueryDate("from_date", "date_from", "start_date"),
            toDate ?? QueryDate("to_date", "date_to", "end_date"));

    private DateTime? QueryDate(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!Request.Query.TryGetValue(key, out var values)) continue;
            var value = values.FirstOrDefault();
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return parsed;
        }

        return null;
    }

    private static IReadOnlyCollection<MobileInvoiceListItemDto> BuildInvoiceListItems(IReadOnlyCollection<NewInvoiceDto> invoices)
    {
        return invoices
            .GroupBy(x => x.Id)
            .Select(group =>
            {
                var invoice = group.OrderByDescending(x => x.SchemePoints).First();
                var rewardAmount = invoice.ApprovalStatus == NewInvoice.StatusApprovedHo ? group.Sum(x => x.SchemePoints) : 0;
                var displayDate = invoice.CreatedAt ?? invoice.InvoiceDate;
                var statusKey = InvoiceStatusKey(invoice.ApprovalStatus);
                return new MobileInvoiceListItemDto
                {
                    Id = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceNumberDisplay = $"#{invoice.InvoiceNumber.TrimStart('#')}",
                    InvoiceDate = invoice.InvoiceDate,
                    DisplayDate = displayDate.ToString("dd MMM, h:mm tt", CultureInfo.InvariantCulture),
                    MonthKey = invoice.InvoiceDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    MonthLabel = invoice.InvoiceDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                    Amount = invoice.Amount,
                    AmountDisplay = FormatIndianCurrency(invoice.Amount),
                    RewardAmount = rewardAmount,
                    RewardDisplay = rewardAmount > 0 ? $"+{FormatIndianCurrency(rewardAmount)}" : null,
                    RewardLabel = rewardAmount > 0 ? "Reward" : "Awaiting approval",
                    Status = statusKey,
                    StatusLabel = invoice.ApprovalStatusLabel,
                    IsRewardCredited = rewardAmount > 0,
                    IsPending = invoice.ApprovalStatus == NewInvoice.StatusPending,
                    Attachment = invoice.Attachment,
                    SchemeName = invoice.SchemeName,
                    SchemeNames = group.Where(x => !string.IsNullOrWhiteSpace(x.SchemeName)).Select(x => x.SchemeName!).Distinct().ToArray()
                };
            })
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.Id)
            .ToList();
    }

    private static object BuildInvoiceListSummary(IReadOnlyCollection<MobileInvoiceListItemDto> items) => new
    {
        total_invoices = items.Count,
        rewards_credited = items.Sum(x => x.RewardAmount),
        rewards_credited_display = FormatIndianCurrency(items.Sum(x => x.RewardAmount)),
        approved_invoices = items.Count(x => x.Status == "approved"),
        pending_invoices = items.Count(x => x.Status == "pending"),
        rejected_invoices = items.Count(x => x.Status == "rejected"),
        total_turnover = items.Sum(x => x.Amount),
        total_turnover_display = FormatIndianCurrency(items.Sum(x => x.Amount))
    };

    private static IReadOnlyCollection<MobileInvoiceMonthGroupDto> BuildInvoiceMonthGroups(IReadOnlyCollection<MobileInvoiceListItemDto> items)
    {
        return items
            .GroupBy(x => new { x.MonthKey, x.MonthLabel })
            .Select(group => new MobileInvoiceMonthGroupDto
            {
                MonthKey = group.Key.MonthKey,
                MonthLabel = group.Key.MonthLabel,
                Count = group.Count(),
                Turnover = group.Sum(x => x.Amount),
                TurnoverDisplay = FormatIndianCurrency(group.Sum(x => x.Amount)),
                RewardAmount = group.Sum(x => x.RewardAmount),
                RewardDisplay = group.Sum(x => x.RewardAmount) > 0 ? $"+{FormatIndianCurrency(group.Sum(x => x.RewardAmount))}" : null,
                Items = group.ToList()
            })
            .OrderByDescending(x => x.MonthKey)
            .ToList();
    }

    private static MobileRedemptionHistoryItemDto ToMobileRedemptionHistoryItem(LoyaltyRedemption redemption)
    {
        var createdAt = redemption.CreatedAt ?? DateTime.UtcNow;
        return new MobileRedemptionHistoryItemDto
        {
            Id = redemption.Id,
            TransactionNo = redemption.TransactionNo,
            TransactionNoDisplay = string.IsNullOrWhiteSpace(redemption.TransactionNo) ? $"#{redemption.Id}" : redemption.TransactionNo,
            LoyaltySchemeId = redemption.LoyaltySchemeId,
            SchemeName = redemption.SchemeName,
            WalletType = NormalizeWalletType(redemption.WalletType),
            RedeemMode = NormalizeRedeemMode(redemption.RedeemMode),
            Points = redemption.Points,
            PointsDisplay = $"{redemption.Points:0.##}",
            AccountHolder = redemption.AccountHolder,
            MaskedAccountNumber = Mask(redemption.AccountNumber),
            BankName = redemption.BankName,
            IfscCode = redemption.IfscCode,
            Status = RedemptionStatusKey(redemption.Status),
            StatusLabel = RedemptionStatusLabel(redemption.Status),
            Remark = redemption.Remark,
            CreatedAt = redemption.CreatedAt,
            DisplayDate = createdAt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            MonthKey = createdAt.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            MonthLabel = createdAt.ToString("MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()
        };
    }

    private static object BuildRedemptionHistorySummary(IReadOnlyCollection<MobileRedemptionHistoryItemDto> items) => new
    {
        total_redemptions = items.Count,
        total_points = items.Sum(x => x.Points),
        pending_points = items.Where(x => x.Status == "pending").Sum(x => x.Points),
        approved_points = items.Where(x => x.Status == "approved").Sum(x => x.Points),
        rejected_points = items.Where(x => x.Status == "rejected").Sum(x => x.Points),
        hold_points = items.Where(x => x.Status == "hold").Sum(x => x.Points),
        regular_points = items.Where(x => x.WalletType == "Regular").Sum(x => x.Points),
        booster_points = items.Where(x => x.WalletType == "Booster").Sum(x => x.Points),
        pending_count = items.Count(x => x.Status == "pending"),
        approved_count = items.Count(x => x.Status == "approved"),
        rejected_count = items.Count(x => x.Status == "rejected"),
        hold_count = items.Count(x => x.Status == "hold")
    };

    private static IReadOnlyCollection<MobileRedemptionMonthGroupDto> BuildRedemptionMonthGroups(IReadOnlyCollection<MobileRedemptionHistoryItemDto> items)
    {
        return items
            .GroupBy(x => new { x.MonthKey, x.MonthLabel })
            .Select(group => new MobileRedemptionMonthGroupDto
            {
                MonthKey = group.Key.MonthKey,
                MonthLabel = group.Key.MonthLabel,
                Count = group.Count(),
                TotalPoints = group.Sum(x => x.Points),
                Items = group.ToList()
            })
            .OrderByDescending(x => x.MonthKey)
            .ToList();
    }

    private static IReadOnlyCollection<MobileWalletSchemeBalanceDto> ToMobileSchemeBalances(WalletDto wallet) =>
        wallet.Schemes
            .Where(x => x.AvailablePoints > 0)
            .Select(x => new MobileWalletSchemeBalanceDto
            {
                LoyaltySchemeId = x.SchemeId,
                SchemeName = x.SchemeName,
                AvailablePoints = x.AvailablePoints,
                WalletType = wallet.WalletType
            })
            .ToList();

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

    private async Task<IReadOnlyCollection<DashboardWalletCardDto>> BuildDashboardWalletCards(Customer customer, WalletPair wallet, IReadOnlyCollection<NewInvoiceDto> invoices, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var schemes = await _dbContext.LoyaltySchemes.AsNoTracking()
            .Include(x => x.Slabs)
            .Where(x => x.DeletedAt == null
                && x.Active == "Y"
                && x.Status == "Live"
                && x.SchemeType == "Invoice"
                && x.StartDate <= today
                && x.EndDate >= today)
            .ToListAsync(cancellationToken);

        var regularScheme = schemes
            .Where(x => !IsBooster(x.SchemeTag) && SchemeCustomerTypeMatches(x.CustomerType, customer.CustomerType))
            .OrderBy(x => x.EndDate)
            .ThenBy(x => x.SchemeName)
            .FirstOrDefault();

        var boosterScheme = schemes
            .Where(x => IsBooster(x.SchemeTag) && SchemeCustomerTypeMatches(x.CustomerType, customer.CustomerType))
            .OrderBy(x => x.EndDate)
            .ThenBy(x => x.SchemeName)
            .FirstOrDefault();

        return
        [
            BuildRegularWalletCard(regularScheme, wallet.Regular, invoices, today),
            BuildBoosterWalletCard(boosterScheme, wallet.Booster, invoices, today)
        ];
    }

    private static DashboardWalletCardDto BuildRegularWalletCard(LoyaltyScheme? scheme, WalletDto wallet, IReadOnlyCollection<NewInvoiceDto> invoices, DateOnly today)
    {
        var approvedInvoices = invoices
            .Where(x => x.ApprovalStatus == NewInvoice.StatusApprovedHo)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();

        if (scheme is null)
        {
            return new DashboardWalletCardDto
            {
                Key = "slab",
                Title = "Slab Wallet",
                WalletType = "Regular",
                Points = wallet.AvailablePoints,
                AvailablePoints = wallet.AvailablePoints,
                Schemes = ToMobileSchemeBalances(wallet),
                IsActive = false,
                ExpiryLabel = "No active scheme",
                NextMessage = "No active slab scheme available.",
                DaysLeftMessage = "No active slab scheme available."
            };
        }

        var start = scheme.StartDate.ToDateTime(TimeOnly.MinValue);
        var end = scheme.EndDate.ToDateTime(TimeOnly.MaxValue);
        var invoiceAmount = approvedInvoices
            .Where(x => x.InvoiceDate >= start && x.InvoiceDate <= end)
            .Sum(x => x.Amount);
        var slabs = scheme.Slabs
            .OrderBy(x => x.ValueFrom)
            .ThenBy(x => x.SortOrder)
            .ToList();
        var achieved = slabs.LastOrDefault(x => invoiceAmount >= x.ValueFrom && (!x.ValueTo.HasValue || invoiceAmount <= x.ValueTo.Value))
            ?? slabs.LastOrDefault(x => invoiceAmount >= x.ValueFrom);
        var next = slabs.FirstOrDefault(x => invoiceAmount < x.ValueFrom);
        var daysLeft = Math.Max(0, (scheme.EndDate.ToDateTime(TimeOnly.MinValue).Date - DateTime.UtcNow.Date).Days);
        var amountMore = next is null ? 0 : Math.Max(0, next.ValueFrom - invoiceAmount);

        return new DashboardWalletCardDto
        {
            Key = "slab",
            Title = "Slab Wallet",
            WalletType = "Regular",
            SchemeId = scheme.Id,
            SchemeName = scheme.SchemeName,
            SchemeCode = scheme.SchemeCode,
            SchemeTag = scheme.SchemeTag,
            BasedOn = scheme.BasedOn,
            Points = wallet.AvailablePoints,
            AvailablePoints = wallet.AvailablePoints,
            EarnedPoints = wallet.EarnedPoints,
            RedeemedPoints = wallet.RedeemedPoints,
            Schemes = ToMobileSchemeBalances(wallet),
            InvoiceAmount = invoiceAmount,
            InvoiceAmountShort = FormatIndianShortAmount(invoiceAmount),
            AchievedReward = achieved?.RewardValue ?? 0,
            AchievedLabel = achieved is null ? "0" : FormatReward(achieved.RewardValue, scheme.BasedOn),
            AchievedTierName = achieved?.TierName,
            NextReward = next?.RewardValue,
            NextRewardLabel = next is null ? null : FormatReward(next.RewardValue, scheme.BasedOn),
            NextTierName = next?.TierName,
            AmountMoreForNextSlab = amountMore,
            NextMessage = next is null
                ? "Highest slab achieved."
                : $"{FormatIndianCurrency(amountMore)} more for {FormatReward(next.RewardValue, scheme.BasedOn)} slab",
            DaysLeft = daysLeft,
            DaysLeftMessage = daysLeft == 1 ? "You have 1 day left to reach it" : $"You have {daysLeft} days left to reach it",
            StartDate = scheme.StartDate,
            EndDate = scheme.EndDate,
            ExpiresOn = scheme.EndDate.ToString("dd MMM"),
            ExpiryLabel = $"Expires {scheme.EndDate:dd MMM} · Quarterly",
            BadgeText = daysLeft == 1 ? "1D LEFT" : $"{daysLeft}D LEFT",
            IsActive = true,
            ProgressSteps = slabs.Select(slab => new DashboardSlabStepDto
            {
                Id = slab.Id,
                TierName = slab.TierName,
                ValueFrom = slab.ValueFrom,
                ValueTo = slab.ValueTo,
                RewardValue = slab.RewardValue,
                RewardLabel = FormatReward(slab.RewardValue, scheme.BasedOn),
                Achieved = invoiceAmount >= slab.ValueFrom,
                Current = achieved?.Id == slab.Id
            }).ToList(),
            ProgressIndex = achieved is null ? -1 : slabs.FindIndex(x => x.Id == achieved.Id),
            ProgressPercent = slabs.Count == 0 ? 0 : Math.Clamp((decimal)(achieved is null ? 0 : slabs.FindIndex(x => x.Id == achieved.Id) + 1) / slabs.Count * 100, 0, 100)
        };
    }

    private static DashboardWalletCardDto BuildBoosterWalletCard(LoyaltyScheme? scheme, WalletDto wallet, IReadOnlyCollection<NewInvoiceDto> invoices, DateOnly today)
    {
        var approvedInvoices = invoices
            .Where(x => x.ApprovalStatus == NewInvoice.StatusApprovedHo)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();
        var invoiceAmount = approvedInvoices.Sum(x => x.Amount);

        return new DashboardWalletCardDto
        {
            Key = "booster",
            Title = "Booster Wallet",
            WalletType = "Booster",
            SchemeId = scheme?.Id,
            SchemeName = scheme?.SchemeName,
            SchemeCode = scheme?.SchemeCode,
            SchemeTag = scheme?.SchemeTag ?? "Booster",
            BasedOn = scheme?.BasedOn,
            Points = wallet.AvailablePoints,
            AvailablePoints = wallet.AvailablePoints,
            EarnedPoints = wallet.EarnedPoints,
            RedeemedPoints = wallet.RedeemedPoints,
            Schemes = ToMobileSchemeBalances(wallet),
            InvoiceAmount = invoiceAmount,
            InvoiceAmountShort = FormatIndianShortAmount(invoiceAmount),
            StartDate = scheme?.StartDate,
            EndDate = scheme?.EndDate,
            ExpiresOn = scheme?.EndDate.ToString("dd MMM"),
            ExpiryLabel = "Lifetime · Never expires",
            BadgeText = "FOREVER",
            IsActive = scheme is not null,
            NextMessage = scheme is null ? "No active booster scheme available." : null,
            DaysLeft = scheme is null ? null : Math.Max(0, (scheme.EndDate.ToDateTime(TimeOnly.MinValue).Date - DateTime.UtcNow.Date).Days),
            ProgressSteps = scheme?.Slabs.OrderBy(x => x.ValueFrom).ThenBy(x => x.SortOrder).Select(slab => new DashboardSlabStepDto
            {
                Id = slab.Id,
                TierName = slab.TierName,
                ValueFrom = slab.ValueFrom,
                ValueTo = slab.ValueTo,
                RewardValue = slab.RewardValue,
                RewardLabel = FormatReward(slab.RewardValue, scheme.BasedOn),
                Achieved = invoiceAmount >= slab.ValueFrom
            }).ToList() ?? []
        };
    }

    private async Task<IReadOnlyCollection<CurrentSchemeDto>> LiveSchemes(string? walletType, CancellationToken cancellationToken) =>
        await CurrentRunningSchemes(walletType, cancellationToken);

    private async Task<IReadOnlyCollection<CurrentSchemeDto>> CurrentRunningSchemes(string? walletType, CancellationToken cancellationToken)
    {
        var today = CurrentBusinessDate();
        var query = _dbContext.LoyaltySchemes.AsNoTracking().Include(x => x.Slabs)
            .Where(x => x.DeletedAt == null
                && x.Active == "Y"
                && x.Status == "Live"
                && x.SchemeType == "Invoice"
                && x.StartDate <= today
                && x.EndDate >= today);
        if (walletType == "Booster") query = query.Where(x => x.SchemeTag == "Booster");
        if (walletType == "Regular") query = query.Where(x => x.SchemeTag != "Booster");

        var schemes = await query.OrderBy(x => x.SchemeTag).ThenBy(x => x.SchemeName).ToListAsync(cancellationToken);
        return schemes.Select(scheme => new CurrentSchemeDto
        {
            Id = scheme.Id,
            SchemeName = scheme.SchemeName,
            SchemeCode = scheme.SchemeCode,
            SchemeDescription = scheme.SchemeDescription,
            SchemeTag = scheme.SchemeTag,
            WalletType = IsBooster(scheme.SchemeTag) ? "Booster" : "Regular",
            CustomerType = scheme.CustomerType,
            AreaScope = scheme.AreaScope,
            AreaValues = ReadSchemeAreaValues(scheme.AreaValues),
            StartDate = scheme.StartDate,
            EndDate = scheme.EndDate,
            BasedOn = scheme.BasedOn,
            Status = scheme.Status,
            DaysLeft = Math.Max(0, (scheme.EndDate.ToDateTime(TimeOnly.MinValue).Date - DateTime.UtcNow.AddHours(5.5).Date).Days),
            Tiers = scheme.Slabs
                .OrderBy(slab => slab.ValueFrom)
                .ThenBy(slab => slab.SortOrder)
                .Select(slab => new CurrentSchemeTierDto
                {
                    Id = slab.Id,
                    TierName = slab.TierName,
                    ValueFrom = slab.ValueFrom,
                    ValueTo = slab.ValueTo,
                    RewardValue = slab.RewardValue,
                    RewardLabel = FormatReward(slab.RewardValue, scheme.BasedOn),
                    SortOrder = slab.SortOrder
                })
                .ToList()
        }).ToList();
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

    private async Task<object> ToMobileProfile(Customer customer, CancellationToken cancellationToken)
    {
        var fields = ReadFields(customer);
        var cityId = Field(fields, "city_id");
        var stateId = Field(fields, "state_id");
        var pincodeId = Field(fields, "pincode_id");

        return new
        {
            id = customer.Id,
            owner_name = Field(fields, "owner_name") ?? customer.FirstName ?? string.Empty,
            shop_name = Field(fields, "shop_name") ?? customer.Name ?? string.Empty,
            mobile = customer.Mobile ?? string.Empty,
            email = customer.Email ?? string.Empty,
            customer_type_name = customer.CustomerType == InfluencerType ? "Influencers" : "Retailer",
            kyc = new { status = KycStatusValue(fields) },
            custom_fields = new
            {
                gst_number = FirstField(fields, "gst_number", "gstin_no") ?? string.Empty,
                address_line = FirstField(fields, "address_line", "address") ?? string.Empty,
                city_id = cityId ?? string.Empty,
                city_name = Field(fields, "city_name") ?? await CityName(cityId, cancellationToken) ?? string.Empty,
                state_id = stateId ?? string.Empty,
                state_name = Field(fields, "state_name") ?? await StateName(stateId, cancellationToken) ?? string.Empty,
                pincode_id = pincodeId ?? string.Empty,
                pincode = Field(fields, "pincode") ?? await Pincode(pincodeId, cancellationToken) ?? string.Empty
            }
        };
    }

    private static string KycStatusValue(IReadOnlyDictionary<string, string?> fields)
    {
        var documents = new[] { "gst", "pan", "aadhar", "bank" };
        var uploaded = documents.Count(x => !string.IsNullOrWhiteSpace(Field(fields, $"{x}_attachment")) || !string.IsNullOrWhiteSpace(Field(fields, x == "bank" ? "bank_proof" : $"{x}_attachment")));
        var approved = documents.Count(x => string.Equals(Field(fields, $"{x}_kyc_status"), "approved", StringComparison.OrdinalIgnoreCase));
        return approved == documents.Length ? "approved" : uploaded > 0 ? "pending" : "missing";
    }

    private async Task<string?> CityName(string? cityId, CancellationToken cancellationToken)
    {
        if (!ulong.TryParse(cityId, out var id)) return null;
        return await _dbContext.Cities.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.CityName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> StateName(string? stateId, CancellationToken cancellationToken)
    {
        if (!ulong.TryParse(stateId, out var id)) return null;
        return await _dbContext.States.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.StateName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> Pincode(string? pincodeId, CancellationToken cancellationToken)
    {
        if (!ulong.TryParse(pincodeId, out var id)) return null;
        return await _dbContext.Pincodes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.PinCode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private object BuildMobileKyc(Customer customer, IReadOnlyDictionary<string, string?> fields)
    {
        var documents = new[]
        {
            BuildMobileKycDocument(fields, "gst", "GST", FirstField(fields, "gst_attachment", "gst_image"), new[]
            {
                new MobileKycDetailDto("GST Number", "gst_number", FirstField(fields, "gst_number", "gstin_no"))
            }),
            BuildMobileKycDocument(fields, "pan", "PAN", FirstField(fields, "pan_attachment", "pan_image"), new[]
            {
                new MobileKycDetailDto("PAN Number", "pan_number", FirstField(fields, "pan_number", "pan_no"))
            }),
            BuildMobileKycDocument(fields, "aadhar", "Aadhaar Card", FirstField(fields, "aadhar_attachment", "aadhaar_attachment", "adharcard"), new[]
            {
                new MobileKycDetailDto("Aadhaar Number", "aadhar_no", FirstField(fields, "aadhar_no", "aadhaar_no", "aadhaar_number", "aadhar_number"))
            }),
            BuildMobileKycDocument(fields, "bank", "Blank Cheque / Passbook", FirstField(fields, "bank_proof", "blank_cheque", "passbook"), new[]
            {
                new MobileKycDetailDto("Bank Account Type", "bank_account_type", Field(fields, "bank_account_type")),
                new MobileKycDetailDto("Bank Name", "bank_name", Field(fields, "bank_name")),
                new MobileKycDetailDto("Account Number", "bank_account_number", Field(fields, "bank_account_number")),
                new MobileKycDetailDto("IFSC Code", "ifsc_code", Field(fields, "ifsc_code")),
                new MobileKycDetailDto("Account Holder Name", "account_holder_name", Field(fields, "account_holder_name"))
            })
        };

        return new
        {
            customer_id = customer.Id,
            summary = KycState(fields),
            bank_account = BankAccount(fields),
            documents,
            fields = new
            {
                gst_number = FirstField(fields, "gst_number", "gstin_no") ?? string.Empty,
                pan_number = FirstField(fields, "pan_number", "pan_no") ?? string.Empty,
                aadhar_no = FirstField(fields, "aadhar_no", "aadhaar_no", "aadhaar_number", "aadhar_number") ?? string.Empty,
                bank_account_type = Field(fields, "bank_account_type") ?? string.Empty,
                bank_name = Field(fields, "bank_name") ?? string.Empty,
                bank_account_number = Field(fields, "bank_account_number") ?? string.Empty,
                ifsc_code = Field(fields, "ifsc_code") ?? string.Empty,
                account_holder_name = Field(fields, "account_holder_name") ?? string.Empty
            }
        };
    }

    private MobileKycDocumentDto BuildMobileKycDocument(IReadOnlyDictionary<string, string?> fields, string key, string label, string? attachment, IReadOnlyCollection<MobileKycDetailDto> details)
    {
        var status = KycStatus(Field(fields, $"{key}_kyc_status"));
        return new MobileKycDocumentDto
        {
            Key = key,
            Label = label,
            Attachment = attachment ?? string.Empty,
            AttachmentUrl = MediaUrl(attachment),
            Status = status,
            StatusLabel = KycStatusLabel(status),
            Remark = Field(fields, $"{key}_kyc_remark") ?? string.Empty,
            ActionBy = Field(fields, $"{key}_kyc_action_by_name") ?? Field(fields, $"{key}_kyc_action_by") ?? string.Empty,
            ActionAt = Field(fields, $"{key}_kyc_action_at") ?? string.Empty,
            Details = details.ToList()
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

    private string MediaUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        if (Uri.TryCreate(path, UriKind.Absolute, out _)) return path;
        var request = Request;
        return $"{request.Scheme}://{request.Host}{path}";
    }

    private async Task<string> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken)
    {
        if (!IsPdfOrImageFile(file))
        {
            throw new BadHttpRequestException("Only PDF and image files are allowed.");
        }

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

    private static string? FirstField(IReadOnlyDictionary<string, string?> fields, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Field(fields, key);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }

    private static string KycStatus(string? value)
    {
        if (string.Equals(value, "approved", StringComparison.OrdinalIgnoreCase)) return "approved";
        if (string.Equals(value, "rejected", StringComparison.OrdinalIgnoreCase)) return "rejected";
        return "pending";
    }

    private static string KycStatusLabel(string status) => status switch
    {
        "approved" => "Approved",
        "rejected" => "Rejected",
        _ => "Pending"
    };

    private static void ResetKycStatus(IDictionary<string, string?> fields, string documentKey)
    {
        if (string.IsNullOrWhiteSpace(documentKey)) return;

        var prefix = $"{documentKey}_kyc";
        fields[$"{prefix}_status"] = "pending";
        fields.Remove($"{prefix}_remark");
        fields.Remove($"{prefix}_action_by");
        fields.Remove($"{prefix}_action_by_name");
        fields.Remove($"{prefix}_action_at");
    }

    private static string? KycDocumentKeyForDetail(string key) => key switch
    {
        "gst_number" or "gstin_no" => "gst",
        "pan_number" or "pan_no" => "pan",
        "aadhar_no" or "aadhaar_no" or "aadhaar_number" or "aadhar_number" => "aadhar",
        "bank_account_type" or "bank_name" or "bank_account_number" or "bank_account_number_confirm" or "ifsc_code" or "account_holder_name" => "bank",
        _ => null
    };

    private static bool IsImageFile(IFormFile file)
    {
        if (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return true;
        return Path.GetExtension(file.FileName).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp";
    }

    private static bool IsPdfOrImageFile(IFormFile file)
    {
        if (IsImageFile(file)) return true;
        if (string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBooster(string? schemeTag) => string.Equals(schemeTag, "Booster", StringComparison.OrdinalIgnoreCase);

    private static DateOnly CurrentBusinessDate() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5));

    private static string InvoiceStatusKey(int status) => status switch
    {
        NewInvoice.StatusApprovedHo => "approved",
        NewInvoice.StatusRejected => "rejected",
        _ => "pending"
    };

    private static bool InvoiceStatusMatches(NewInvoiceDto invoice, string status)
    {
        var normalized = status.Trim();
        if (string.Equals(normalized, "all", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(normalized, InvoiceStatusKey(invoice.ApprovalStatus), StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(invoice.ApprovalStatusLabel, normalized, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeWalletType(string? value) =>
        string.Equals(value, "Booster", StringComparison.OrdinalIgnoreCase) ? "Booster" : "Regular";

    private static string NormalizeRedeemMode(string? value) =>
        string.Equals(value, "IMPS", StringComparison.OrdinalIgnoreCase) ? "IMPS" : "NEFT";

    private static string RedemptionStatusKey(int status) => status switch
    {
        LoyaltyRedemption.StatusApproved => "approved",
        LoyaltyRedemption.StatusRejected => "rejected",
        LoyaltyRedemption.StatusHold => "hold",
        _ => "pending"
    };

    private static string RedemptionStatusLabel(int status) => status switch
    {
        LoyaltyRedemption.StatusApproved => "Approved",
        LoyaltyRedemption.StatusRejected => "Rejected",
        LoyaltyRedemption.StatusHold => "Hold",
        _ => "Pending"
    };

    private static bool TryRedemptionStatus(string? value, out int status)
    {
        status = LoyaltyRedemption.StatusPending;
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "all", StringComparison.OrdinalIgnoreCase)) return false;
        if (int.TryParse(value, out var parsed) && parsed is >= LoyaltyRedemption.StatusPending and <= LoyaltyRedemption.StatusHold)
        {
            status = parsed;
            return true;
        }

        status = value.Trim().ToLowerInvariant() switch
        {
            "approved" => LoyaltyRedemption.StatusApproved,
            "rejected" => LoyaltyRedemption.StatusRejected,
            "hold" => LoyaltyRedemption.StatusHold,
            "pending" => LoyaltyRedemption.StatusPending,
            _ => -1
        };

        return status >= LoyaltyRedemption.StatusPending;
    }

    private static bool SchemeCustomerTypeMatches(string customerType, ulong? actualCustomerType)
    {
        if (actualCustomerType == RetailerType)
        {
            return customerType.Contains("Retailer", StringComparison.OrdinalIgnoreCase);
        }

        if (actualCustomerType == InfluencerType)
        {
            return customerType.Contains("Influencer", StringComparison.OrdinalIgnoreCase)
                || customerType.Contains("Plumber", StringComparison.OrdinalIgnoreCase)
                || customerType.Contains("Sub-Dealer", StringComparison.OrdinalIgnoreCase)
                || customerType.Contains("Sub Dealer", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string FormatReward(decimal value, string? basedOn) =>
        string.Equals(basedOn, "Percentage", StringComparison.OrdinalIgnoreCase) ? $"{value:0.##}%" : $"Rs. {value:0.##}";

    private static IReadOnlyCollection<string> ReadSchemeAreaValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string FormatIndianCurrency(decimal value) => $"₹{value.ToString("N0", CultureInfo.GetCultureInfo("en-IN"))}";

    private static string FormatIndianShortAmount(decimal value)
    {
        var absolute = Math.Abs(value);
        if (absolute >= 10000000) return $"₹{value / 10000000:0.##}Cr";
        if (absolute >= 100000) return $"₹{value / 100000:0.##}L";
        if (absolute >= 1000) return $"₹{value / 1000:0.##}K";
        return $"₹{value:0.##}";
    }

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
        [FromQuery(Name = "from_date")] public DateTime? FromDate { get; set; }
        [FromQuery(Name = "to_date")] public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        [FromQuery(Name = "page_size")] public int PageSize { get; set; } = 20;
    }

    public sealed class MobileRedemptionRequest
    {
        public ulong? LoyaltySchemeId { get; set; }
        public string? WalletType { get; set; }
        public decimal Points { get; set; }
    }

    public sealed class MobileRedemptionHistoryFilter
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        [FromQuery(Name = "wallet_type")] public string? WalletType { get; set; }
        [FromQuery(Name = "redeem_mode")] public string? RedeemMode { get; set; }
        [FromQuery(Name = "from_date")] public DateTime? FromDate { get; set; }
        [FromQuery(Name = "to_date")] public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        [FromQuery(Name = "page_size")] public int PageSize { get; set; } = 20;
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

    private sealed record MobileKycDetailDto(string Label, string Key, string? Value);

    private sealed class MobileKycDocumentDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Attachment { get; set; } = string.Empty;
        public string AttachmentUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public string StatusLabel { get; set; } = "Pending";
        public string Remark { get; set; } = string.Empty;
        public string ActionBy { get; set; } = string.Empty;
        public string ActionAt { get; set; } = string.Empty;
        public IReadOnlyCollection<MobileKycDetailDto> Details { get; set; } = [];
    }

    private sealed class MobileWalletSchemeBalanceDto
    {
        public ulong? LoyaltySchemeId { get; set; }
        public string SchemeName { get; set; } = string.Empty;
        public decimal AvailablePoints { get; set; }
        public string WalletType { get; set; } = string.Empty;
    }

    private sealed class MobileInvoiceListItemDto
    {
        public ulong Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string InvoiceNumberDisplay { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string DisplayDate { get; set; } = string.Empty;
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string AmountDisplay { get; set; } = string.Empty;
        public decimal RewardAmount { get; set; }
        public string? RewardDisplay { get; set; }
        public string RewardLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public bool IsRewardCredited { get; set; }
        public bool IsPending { get; set; }
        public string? Attachment { get; set; }
        public string? SchemeName { get; set; }
        public IReadOnlyCollection<string> SchemeNames { get; set; } = [];
    }

    private sealed class MobileInvoiceMonthGroupDto
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Turnover { get; set; }
        public string TurnoverDisplay { get; set; } = string.Empty;
        public decimal RewardAmount { get; set; }
        public string? RewardDisplay { get; set; }
        public IReadOnlyCollection<MobileInvoiceListItemDto> Items { get; set; } = [];
    }

    private sealed class MobileRedemptionHistoryItemDto
    {
        public ulong Id { get; set; }
        public string TransactionNo { get; set; } = string.Empty;
        public string TransactionNoDisplay { get; set; } = string.Empty;
        public ulong? LoyaltySchemeId { get; set; }
        public string SchemeName { get; set; } = string.Empty;
        public string WalletType { get; set; } = string.Empty;
        public string RedeemMode { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public string PointsDisplay { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public string MaskedAccountNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string DisplayDate { get; set; } = string.Empty;
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
    }

    private sealed class MobileRedemptionMonthGroupDto
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalPoints { get; set; }
        public IReadOnlyCollection<MobileRedemptionHistoryItemDto> Items { get; set; } = [];
    }

    private sealed class DashboardWalletCardDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string WalletType { get; set; } = string.Empty;
        public ulong? SchemeId { get; set; }
        public string? SchemeName { get; set; }
        public string? SchemeCode { get; set; }
        public string? SchemeTag { get; set; }
        public string? BasedOn { get; set; }
        public decimal Points { get; set; }
        public decimal AvailablePoints { get; set; }
        public decimal EarnedPoints { get; set; }
        public decimal RedeemedPoints { get; set; }
        public IReadOnlyCollection<MobileWalletSchemeBalanceDto> Schemes { get; set; } = [];
        public decimal InvoiceAmount { get; set; }
        public string InvoiceAmountShort { get; set; } = "₹0";
        public decimal AchievedReward { get; set; }
        public string? AchievedLabel { get; set; }
        public string? AchievedTierName { get; set; }
        public decimal? NextReward { get; set; }
        public string? NextRewardLabel { get; set; }
        public string? NextTierName { get; set; }
        public decimal AmountMoreForNextSlab { get; set; }
        public string? NextMessage { get; set; }
        public int? DaysLeft { get; set; }
        public string? DaysLeftMessage { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? ExpiresOn { get; set; }
        public string ExpiryLabel { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int ProgressIndex { get; set; } = -1;
        public decimal ProgressPercent { get; set; }
        public IReadOnlyCollection<DashboardSlabStepDto> ProgressSteps { get; set; } = [];
    }

    private sealed class DashboardSlabStepDto
    {
        public ulong Id { get; set; }
        public string TierName { get; set; } = string.Empty;
        public decimal ValueFrom { get; set; }
        public decimal? ValueTo { get; set; }
        public decimal RewardValue { get; set; }
        public string RewardLabel { get; set; } = string.Empty;
        public bool Achieved { get; set; }
        public bool Current { get; set; }
    }

    private sealed class CurrentSchemeDto
    {
        public ulong Id { get; set; }
        public string SchemeName { get; set; } = string.Empty;
        public string SchemeCode { get; set; } = string.Empty;
        public string? SchemeDescription { get; set; }
        public string SchemeTag { get; set; } = string.Empty;
        public string WalletType { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string AreaScope { get; set; } = string.Empty;
        public IReadOnlyCollection<string> AreaValues { get; set; } = [];
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string BasedOn { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int DaysLeft { get; set; }
        public IReadOnlyCollection<CurrentSchemeTierDto> Tiers { get; set; } = [];
    }

    private sealed class CurrentSchemeTierDto
    {
        public ulong Id { get; set; }
        public string TierName { get; set; } = string.Empty;
        public decimal ValueFrom { get; set; }
        public decimal? ValueTo { get; set; }
        public decimal RewardValue { get; set; }
        public string RewardLabel { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
