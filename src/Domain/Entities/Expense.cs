namespace Domain.Entities;

public sealed class Expense : BaseEntity
{
    public ulong? ExpensesType { get; set; }
    public ulong? UserId { get; set; }
    public string? Date { get; set; }
    public decimal? ClaimAmount { get; set; }
    public decimal? ApproveAmount { get; set; }
    public string? StartKm { get; set; }
    public string? StopKm { get; set; }
    public string? TotalKm { get; set; }
    public string? Note { get; set; }
    public int CheckerStatus { get; set; }
    public int AccountantStatus { get; set; }
    public ulong? ApproveRejectBy { get; set; }
    public string? Reason { get; set; }
    public ulong CreatedBy { get; set; }
}
