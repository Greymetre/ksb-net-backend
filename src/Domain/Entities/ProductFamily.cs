namespace Domain.Entities;

public sealed class ProductFamily : BaseEntity
{
    public string Active { get; set; } = "Y";
    public int Ranking { get; set; } = 1;
    public string SubcategoryName { get; set; } = string.Empty;
    public string SubcategoryImage { get; set; } = string.Empty;
    public string? SapCode { get; set; }
    public ulong? CategoryId { get; set; }
    public string? ServiceCategoryId { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}

