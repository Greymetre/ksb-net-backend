namespace Domain.Entities;

public sealed class Leave : BaseEntity
{
    public string Active { get; set; } = "Y";
    public ulong? UserId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Type { get; set; } = "leave";
    public string? BalType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ulong? CreatedBy { get; set; }
    public int? Status { get; set; }
    public string? RemarkStatus { get; set; }
}
