namespace Domain.Entities;

public sealed class NewInvoiceApprovalLog : BaseEntity
{
    public DateTime? LogDate { get; set; }
    public ulong? NewInvoiceId { get; set; }
    public ulong? CreatedBy { get; set; }
    public string? StatusType { get; set; }
    public int? FromStatus { get; set; }
    public int? ToStatus { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? Remark { get; set; }
}
