namespace Domain.Entities;

public sealed class Product : BaseEntity
{
    public string Active { get; set; } = "Y";
    public int Ranking { get; set; } = 1;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ulong? SubcategoryId { get; set; }
    public ulong? CategoryId { get; set; }
    public string ProductImage { get; set; } = string.Empty;
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
    public string Specification { get; set; } = string.Empty;
    public string PartNo { get; set; } = string.Empty;
    public string ProductNo { get; set; } = string.Empty;
    public string ModelNo { get; set; } = string.Empty;
    public string? SapCode { get; set; }
    public long HsnSac { get; set; }
}

