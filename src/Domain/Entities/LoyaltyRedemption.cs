namespace Domain.Entities;

public sealed class LoyaltyRedemption : BaseEntity
{
    public const int StatusPending = 0;
    public const int StatusApproved = 1;
    public const int StatusRejected = 2;
    public const int StatusHold = 3;

    public string TransactionNo { get; set; } = string.Empty;
    public ulong CustomerId { get; set; }
    public ulong? LoyaltySchemeId { get; set; }
    public string WalletType { get; set; } = "Regular";
    public string SchemeName { get; set; } = string.Empty;
    public string RedeemMode { get; set; } = "NEFT";
    public decimal Points { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string IfscCode { get; set; } = string.Empty;
    public bool BankConfirmed { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public ulong? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
}
