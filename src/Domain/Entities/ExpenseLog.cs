namespace Domain.Entities;

public sealed class ExpenseLog : BaseEntity
{
    public DateOnly? LogDate { get; set; }
    public ulong? ExpenseId { get; set; }
    public ulong? CreatedBy { get; set; }
    public string? StatusType { get; set; }
}
