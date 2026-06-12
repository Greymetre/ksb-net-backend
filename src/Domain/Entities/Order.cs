namespace Domain.Entities;

public sealed class Order : BaseEntity
{
    public string Active { get; set; } = "Y";
    public ulong? BuyerId { get; set; }
    public ulong? SellerId { get; set; }
    public ulong? ExecutiveId { get; set; }
    public long ShippedQty { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime? OrderDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? EstimatedDate { get; set; }
    public decimal TotalGst { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ExtraDiscount { get; set; }
    public decimal ExtraDiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string OrderTaking { get; set; } = string.Empty;
    public ulong? StatusId { get; set; }
    public ulong? AddressId { get; set; }
    public string? SucDel { get; set; }
    public string? GstAmount { get; set; }
    public decimal? SchmeAmount { get; set; }
    public decimal? SchmeVal { get; set; }
    public decimal? EbdAmount { get; set; }
    public decimal? EbdDiscount { get; set; }
    public int? SpecialDiscount { get; set; }
    public decimal? SpecialAmount { get; set; }
    public int? ClusterDiscount { get; set; }
    public decimal? ClusterAmount { get; set; }
    public int? DealDiscount { get; set; }
    public decimal? DealAmount { get; set; }
    public int? DistributorDiscount { get; set; }
    public decimal? DistributorAmount { get; set; }
    public int? FrieghtDiscount { get; set; }
    public decimal? FrieghtAmount { get; set; }
    public ulong? ProductCatId { get; set; }
    public decimal DodDiscount { get; set; }
    public decimal CashDiscount { get; set; }
    public decimal CashAmount { get; set; }
    public decimal AgriStandardDiscount { get; set; }
    public decimal AgriStandardDiscountAmount { get; set; }
    public decimal Advance { get; set; }
    public decimal Gst5Amt { get; set; }
    public decimal Gst12Amt { get; set; }
    public decimal Gst18Amt { get; set; }
    public decimal Gst28Amt { get; set; }
    public string? OrderRemark { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
    public ulong? BeatScheduleId { get; set; }
    public string? OrderType { get; set; }
    public decimal SubTotal { get; set; }
    public long TotalQty { get; set; }
}
