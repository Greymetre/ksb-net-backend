namespace Domain.Entities;

public sealed class NewInvoice : BaseEntity
{
    public const int StatusPending = 0;
    public const int StatusApprovedSs = 1;
    public const int StatusApprovedSales = 2;
    public const int StatusApprovedHo = 3;
    public const int StatusRejected = 4;

    public ulong SecondaryCustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Points { get; set; }
    public string? Attachment { get; set; }
    public int ApprovalStatus { get; set; }
    public string? ApprovalRemark { get; set; }
    public ulong? ApprovedSsBy { get; set; }
    public DateTime? ApprovedSsAt { get; set; }
    public ulong? ApprovedSalesBy { get; set; }
    public DateTime? ApprovedSalesAt { get; set; }
    public ulong? ApprovedHoBy { get; set; }
    public DateTime? ApprovedHoAt { get; set; }
    public ulong? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public ulong CreatedBy { get; set; }
}
