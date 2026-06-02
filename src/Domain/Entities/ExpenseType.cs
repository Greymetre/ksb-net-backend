namespace Domain.Entities;

public sealed class ExpenseType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public int IsActive { get; set; } = 1;
    public ulong AllowanceTypeId { get; set; }
    public ulong? PayrollId { get; set; }
}
