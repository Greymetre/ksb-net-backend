namespace Domain.Entities;

public sealed class Order : BaseEntity
{
    public DateTime? OrderDate { get; set; }
    public ulong? CreatedBy { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalQty { get; set; }
}

