namespace Domain.Entities;

public sealed class ProductDetail : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string DetailTitle { get; set; } = string.Empty;
    public string DetailDescription { get; set; } = string.Empty;
    public ulong? ProductId { get; set; }
    public string DetailImage { get; set; } = string.Empty;
    public decimal? Mrp { get; set; }
    public decimal? Price { get; set; }
    public decimal? SellingPrice { get; set; }
}

