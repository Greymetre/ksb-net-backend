namespace Domain.Entities;

public sealed class OrderDetail : BaseEntity
{
    public string Active { get; set; } = "Y";
    public ulong? OrderId { get; set; }
    public ulong? ProductId { get; set; }
    public ulong? ProductDetailId { get; set; }
    public long Quantity { get; set; }
    public long ShippedQty { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal Gst { get; set; }
    public decimal GstAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public ulong? StatusId { get; set; }
    public string? SchemeName { get; set; }
    public decimal SchemeDiscount { get; set; }
    public decimal SchemeAmount { get; set; }
    public decimal ClusterDiscount { get; set; }
    public decimal ClusterAmount { get; set; }
    public decimal DealDiscount { get; set; }
    public decimal DealAmount { get; set; }
    public decimal DistributorDiscount { get; set; }
    public decimal DistributorAmount { get; set; }
    public decimal FrieghtDiscount { get; set; }
    public decimal FrieghtAmount { get; set; }
    public decimal AgriStandardDis { get; set; }
    public decimal AgriStandardDisAmounts { get; set; }
    public int? EbdDis { get; set; }
    public int? SpecialDis { get; set; }
    public decimal? SpecialAmounts { get; set; }
    public decimal? EbdAmount { get; set; }
    public ulong? SubcategoryId { get; set; }
    public ulong? CategoryId { get; set; }
}
