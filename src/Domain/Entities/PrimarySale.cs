namespace Domain.Entities;

public sealed class PrimarySale : BaseEntity
{
    public DateTime? InvoiceDate { get; set; }
    public ulong? BranchId { get; set; }
    public string? EmpCode { get; set; }
    public decimal NetAmount { get; set; }
    public decimal Quantity { get; set; }
}

