namespace Application.DTOs.Redemptions;

public sealed class RedemptionDto
{
    public ulong Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public ulong CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerTypeName { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? CityName { get; set; }
    public string? DistributorName { get; set; }
    public ulong? LoyaltySchemeId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public string WalletType { get; set; } = string.Empty;
    public string RedeemMode { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string IfscCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class RedemptionFilterDto
{
    public string? Search { get; set; }
    public int? Status { get; set; }
    public string? RedeemMode { get; set; }
}

public sealed class RedemptionCreateRequestDto
{
    public ulong CustomerId { get; set; }
    public ulong? LoyaltySchemeId { get; set; }
    public string? WalletType { get; set; }
    public string? RedeemMode { get; set; }
    public decimal Points { get; set; }
    public bool BankConfirmed { get; set; }
}

public sealed class RedemptionSummaryDto
{
    public int TotalRequests { get; set; }
    public int Approved { get; set; }
    public int Pending { get; set; }
    public int RejectedOrHold { get; set; }
}

public sealed class RedemptionListResultDto
{
    public IReadOnlyCollection<RedemptionDto> Redemptions { get; set; } = [];
    public RedemptionSummaryDto Summary { get; set; } = new();
}

public sealed class RedemptionCustomerOptionDto
{
    public ulong Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string CustomerTypeName { get; set; } = string.Empty;
    public string? CityName { get; set; }
    public string? DistributorName { get; set; }
    public bool KycApproved { get; set; }
    public string KycState { get; set; } = "pending";
    public string KycMessage { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string MaskedAccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string IfscCode { get; set; } = string.Empty;
    public decimal RegularPoints { get; set; }
    public decimal BoosterPoints { get; set; }
    public IReadOnlyCollection<RedemptionSchemeOptionDto> RegularSchemes { get; set; } = [];
    public IReadOnlyCollection<RedemptionSchemeOptionDto> BoosterSchemes { get; set; } = [];
}

public sealed class RedemptionSchemeOptionDto
{
    public ulong? SchemeId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public string WalletType { get; set; } = string.Empty;
    public decimal EarnedPoints { get; set; }
    public decimal RedeemedPoints { get; set; }
    public decimal AvailablePoints { get; set; }
}
