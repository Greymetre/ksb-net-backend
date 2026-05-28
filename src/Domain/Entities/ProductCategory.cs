namespace Domain.Entities;

public sealed class ProductCategory : BaseEntity
{
    public string Active { get; set; } = "Y";
    public int Ranking { get; set; } = 1;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryImage { get; set; } = string.Empty;
    public string? SapCode { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}

