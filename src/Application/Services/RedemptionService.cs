using Application.Common;
using Application.DTOs.Customers;
using Application.DTOs.MasterData;
using Application.DTOs.NewInvoices;
using Application.DTOs.Redemptions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Services;

public sealed class RedemptionService : IRedemptionService
{
    private const decimal MinimumRedemptionPoints = 500;
    private readonly IRedemptionRepository _repository;
    private readonly ICustomerRepository _customerRepository;
    private readonly INewInvoiceRepository _newInvoiceRepository;

    public RedemptionService(IRedemptionRepository repository, ICustomerRepository customerRepository, INewInvoiceRepository newInvoiceRepository)
    {
        _repository = repository;
        _customerRepository = customerRepository;
        _newInvoiceRepository = newInvoiceRepository;
    }

    public async Task<LaravelApiResponse> GetRedemptionsAsync(RedemptionFilterDto filter, CancellationToken cancellationToken)
    {
        var redemptions = await _repository.GetRedemptionsAsync(filter, cancellationToken);
        return LaravelApiResponse.Success("redemptions", new RedemptionListResultDto
        {
            Redemptions = redemptions,
            Summary = new RedemptionSummaryDto
            {
                TotalRequests = redemptions.Count,
                Approved = redemptions.Count(x => x.Status == LoyaltyRedemption.StatusApproved),
                Pending = redemptions.Count(x => x.Status == LoyaltyRedemption.StatusPending),
                RejectedOrHold = redemptions.Count(x => x.Status is LoyaltyRedemption.StatusRejected or LoyaltyRedemption.StatusHold)
            }
        });
    }

    public async Task<MasterDataFileDto> ExportRedemptionsAsync(RedemptionFilterDto filter, CancellationToken cancellationToken)
    {
        var redemptions = await _repository.GetRedemptionsAsync(filter, cancellationToken);
        return CreateWorkbook("redemptions.xlsx",
            ["date", "transaction_id", "customer_id", "customer", "type", "city", "mobile", "mode", "distributor", "wallet", "scheme", "points", "status", "created_by"],
            redemptions.Select(x => new object?[]
            {
                x.CreatedAt,
                x.TransactionNo,
                x.CustomerCode,
                x.CustomerName,
                x.CustomerTypeName,
                x.CityName,
                x.MobileNumber,
                x.RedeemMode,
                x.DistributorName,
                x.WalletType,
                x.SchemeName,
                x.Points,
                x.StatusLabel,
                x.CreatedByName
            }));
    }

    public async Task<LaravelApiResponse> GetCustomerOptionsAsync(string? search, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var customers = await _customerRepository.GetCustomersAsync(new CustomerListFilterDto
        {
            CustomerType = 2,
            Active = "Y",
            Search = search
        }, cancellationToken);

        var invoices = await _newInvoiceRepository.GetInvoicesAsync(new NewInvoiceFilterDto(), actorUserId, cancellationToken);
        var options = new List<RedemptionCustomerOptionDto>();
        foreach (var customer in customers.Take(50))
        {
            options.Add(await BuildCustomerOptionAsync(customer, invoices.Where(x => x.SecondaryCustomerId == customer.Id).ToList(), cancellationToken));
        }

        return LaravelApiResponse.Success("customers", options);
    }

    public async Task<LaravelApiResponse> CreateRedemptionAsync(RedemptionCreateRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) throw Http(LaravelStatusCodes.Unauthorized, "Unauthenticated.");
        if (request.CustomerId == 0) throw Http(LaravelStatusCodes.NoContentLikeValidation, "Customer is required.");
        if (!request.BankConfirmed) throw Http(LaravelStatusCodes.NoContentLikeValidation, "Bank details confirmation is required.");
        if (request.Points < MinimumRedemptionPoints) throw Http(LaravelStatusCodes.NoContentLikeValidation, $"Minimum redemption is {MinimumRedemptionPoints:0} points.");

        var walletType = NormalizeWalletType(request.WalletType);
        var redeemMode = NormalizeRedeemMode(request.RedeemMode);
        var customer = await _customerRepository.GetCustomerAsync(request.CustomerId, cancellationToken) ?? throw Http(LaravelStatusCodes.NotFound, "Customer not found.");
        var invoices = await _newInvoiceRepository.GetInvoicesAsync(new NewInvoiceFilterDto(), actorUserId, cancellationToken);
        var option = await BuildCustomerOptionAsync(customer, invoices.Where(x => x.SecondaryCustomerId == customer.Id).ToList(), cancellationToken);

        if (!option.KycApproved) throw Http(LaravelStatusCodes.NoContentLikeValidation, option.KycMessage);
        if (string.IsNullOrWhiteSpace(option.AccountHolder) || string.IsNullOrWhiteSpace(option.AccountNumber) || string.IsNullOrWhiteSpace(option.BankName) || string.IsNullOrWhiteSpace(option.IfscCode))
        {
            throw Http(LaravelStatusCodes.NoContentLikeValidation, "Bank details are required before redemption.");
        }

        var scheme = (walletType == "Booster" ? option.BoosterSchemes : option.RegularSchemes)
            .FirstOrDefault(x => x.SchemeId == request.LoyaltySchemeId);
        if (scheme is null) throw Http(LaravelStatusCodes.NoContentLikeValidation, "Selected scheme is not available in this wallet.");
        if (request.Points > scheme.AvailablePoints) throw Http(LaravelStatusCodes.NoContentLikeValidation, "Redeem points cannot be greater than available points.");

        var now = DateTime.UtcNow;
        var redemption = new LoyaltyRedemption
        {
            TransactionNo = await GenerateTransactionNoAsync(cancellationToken),
            CustomerId = customer.Id,
            LoyaltySchemeId = scheme.SchemeId,
            WalletType = walletType,
            SchemeName = scheme.SchemeName,
            RedeemMode = redeemMode,
            Points = request.Points,
            AccountHolder = option.AccountHolder,
            AccountNumber = option.AccountNumber,
            BankName = option.BankName,
            IfscCode = option.IfscCode,
            BankConfirmed = true,
            Status = LoyaltyRedemption.StatusPending,
            CreatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.CreateAsync(redemption, cancellationToken);
        return LaravelApiResponse.Success("redemption", redemption, "Redemption request submitted successfully");
    }

    private async Task<RedemptionCustomerOptionDto> BuildCustomerOptionAsync(CustomerDto customer, IReadOnlyCollection<NewInvoiceDto> invoices, CancellationToken cancellationToken)
    {
        var redemptions = await _repository.GetCustomerRedemptionsAsync(customer.Id, cancellationToken);
        var schemes = invoices
            .Where(x => x.SchemeId.HasValue && x.SchemePoints > 0)
            .GroupBy(x => new { x.SchemeId, x.SchemeName, WalletType = IsBooster(x.SchemeTag) ? "Booster" : "Regular" })
            .Select(group =>
            {
                var redeemed = redemptions
                    .Where(x => x.LoyaltySchemeId == group.Key.SchemeId && x.WalletType == group.Key.WalletType)
                    .Sum(x => x.Points);
                var earned = group.Sum(x => x.SchemePoints);
                return new RedemptionSchemeOptionDto
                {
                    SchemeId = group.Key.SchemeId,
                    SchemeName = group.Key.SchemeName ?? "Scheme",
                    WalletType = group.Key.WalletType,
                    EarnedPoints = earned,
                    RedeemedPoints = redeemed,
                    AvailablePoints = Math.Max(0, earned - redeemed)
                };
            })
            .Where(x => x.AvailablePoints > 0)
            .OrderBy(x => x.SchemeName)
            .ToList();

        var kyc = KycApproved(customer);
        return new RedemptionCustomerOptionDto
        {
            Id = customer.Id,
            CustomerCode = CustomerCode(customer),
            Name = Field(customer, "owner_name") ?? customer.Name,
            ShopName = Field(customer, "shop_name") ?? customer.Name,
            MobileNumber = Field(customer, "mobile_numbers")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? customer.Mobile ?? string.Empty,
            CustomerTypeName = customer.CustomerTypeName,
            CityName = customer.CityName,
            DistributorName = Field(customer, "distributor_name_name") ?? Field(customer, "distributor_name"),
            KycApproved = kyc.Approved,
            KycState = kyc.State,
            KycMessage = kyc.Message,
            AccountHolder = Field(customer, "account_holder_name") ?? string.Empty,
            AccountNumber = Field(customer, "bank_account_number") ?? string.Empty,
            MaskedAccountNumber = MaskAccount(Field(customer, "bank_account_number") ?? string.Empty),
            BankName = Field(customer, "bank_name") ?? string.Empty,
            IfscCode = Field(customer, "ifsc_code") ?? string.Empty,
            RegularPoints = schemes.Where(x => x.WalletType == "Regular").Sum(x => x.AvailablePoints),
            BoosterPoints = schemes.Where(x => x.WalletType == "Booster").Sum(x => x.AvailablePoints),
            RegularSchemes = schemes.Where(x => x.WalletType == "Regular").ToList(),
            BoosterSchemes = schemes.Where(x => x.WalletType == "Booster").ToList()
        };
    }

    private static (bool Approved, string State, string Message) KycApproved(CustomerDto customer)
    {
        var documents = new[]
        {
            new KycDocument("gst", "GST", "gst_attachment"),
            new KycDocument("pan", "PAN", "pan_attachment"),
            new KycDocument("aadhar", "Aadhaar", "aadhar_attachment"),
            new KycDocument("bank", "Blank Cheque / Passbook", "bank_proof")
        };

        var missing = documents.Where(document => string.IsNullOrWhiteSpace(Field(customer, document.AttachmentKey))).Select(document => document.Label).ToList();
        var rejected = documents.Where(document => string.Equals(Field(customer, $"{document.Key}_kyc_status"), "rejected", StringComparison.OrdinalIgnoreCase)).Select(document => document.Label).ToList();
        var pending = documents
            .Where(document => !missing.Contains(document.Label) && !rejected.Contains(document.Label))
            .Where(document => !string.Equals(Field(customer, $"{document.Key}_kyc_status"), "approved", StringComparison.OrdinalIgnoreCase))
            .Select(document => document.Label)
            .ToList();

        if (missing.Count == 0 && rejected.Count == 0 && pending.Count == 0) return (true, "approved", "KYC approved. You can continue with redemption.");

        var messages = new List<string>();
        if (missing.Count > 0) messages.Add($"Please upload {JoinLabels(missing)} KYC document{Plural(missing)} before creating redemption.");
        if (rejected.Count > 0) messages.Add($"{JoinLabels(rejected)} KYC document{Plural(rejected)} rejected. Please upload corrected document{Plural(rejected)} again.");
        if (pending.Count > 0) messages.Add($"{JoinLabels(pending)} KYC document{Plural(pending)} uploaded and pending for approval.");

        var state = missing.Count > 0 ? "missing" : rejected.Count > 0 ? "rejected" : "pending";
        return (false, state, string.Join(" ", messages));
    }

    private async Task<string> GenerateTransactionNoAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var value = $"TXN-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(1000, 9999)}";
            if (!await _repository.TransactionNoExistsAsync(value, cancellationToken)) return value;
        }

        return $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private static string NormalizeWalletType(string? value)
    {
        if (string.Equals(value, "Booster", StringComparison.OrdinalIgnoreCase)) return "Booster";
        return "Regular";
    }

    private static string NormalizeRedeemMode(string? value)
    {
        if (string.Equals(value, "IMPS", StringComparison.OrdinalIgnoreCase)) return "IMPS";
        return "NEFT";
    }

    private static bool IsBooster(string? schemeTag) =>
        string.Equals(schemeTag, "Booster", StringComparison.OrdinalIgnoreCase);

    private static string? Field(CustomerDto customer, string key) =>
        customer.CustomFields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string MaskAccount(string value) =>
        value.Length <= 4 ? value : $"{new string('*', Math.Max(0, value.Length - 4))}{value[^4..]}";

    private static string CustomerCode(CustomerDto customer) =>
        !string.IsNullOrWhiteSpace(customer.CustomerCode)
            ? customer.CustomerCode
            : $"{CustomerPrefix(customer.CustomerType)}-{customer.Id.ToString().PadLeft(4, '0')}";

    private static string CustomerPrefix(ulong? customerType) => customerType switch
    {
        1 => "DIS",
        2 => "RET",
        _ => "CUS"
    };

    private static string TitleCase(string value) =>
        System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace("_", " ").ToLowerInvariant());

    private static string JoinLabels(IReadOnlyList<string> values) =>
        values.Count switch
        {
            0 => string.Empty,
            1 => values[0],
            2 => $"{values[0]} and {values[1]}",
            _ => $"{string.Join(", ", values.Take(values.Count - 1))}, and {values[^1]}"
        };

    private static string Plural(IReadOnlyCollection<string> values) => values.Count == 1 ? string.Empty : "s";

    private static LaravelHttpException Http(int statusCode, object message) => new(statusCode, message);

    private sealed record KycDocument(string Key, string Label, string AttachmentKey);

    private static MasterDataFileDto CreateWorkbook(string fileName, string[] headings, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");
        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 9;

        for (var column = 0; column < headings.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = TitleCase(headings[column].Replace("_", " "));
            worksheet.Cell(1, column + 1).Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < row.Length; column++)
            {
                worksheet.Cell(rowNumber, column + 1).Value = XLCellValue.FromObject(row[column]);
            }

            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new MasterDataFileDto { FileName = fileName, Content = stream.ToArray() };
    }
}
