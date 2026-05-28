namespace Application.DTOs.Products;

public sealed class ProductSegmentDto
{
    public ulong Id { get; set; }
    public string Active { get; set; } = "Y";
    public string Name { get; set; } = string.Empty;
    public string? SapCode { get; set; }
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class ProductFamilyDto
{
    public ulong Id { get; set; }
    public string Active { get; set; } = "Y";
    public string Name { get; set; } = string.Empty;
    public ulong? SegmentId { get; set; }
    public string? SegmentName { get; set; }
    public string? SapCode { get; set; }
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class ProductDto
{
    public ulong Id { get; set; }
    public string Active { get; set; } = "Y";
    public ulong? SegmentId { get; set; }
    public string? SegmentName { get; set; }
    public ulong? FamilyId { get; set; }
    public string? FamilyName { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? Mrp { get; set; }
    public string? Attachment { get; set; }
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class ProductSegmentRequestDto
{
    public string? Active { get; set; }
    public string? Name { get; set; }
    public string? SapCode { get; set; }
}

public sealed class ProductFamilyRequestDto
{
    public string? Active { get; set; }
    public string? Name { get; set; }
    public ulong? SegmentId { get; set; }
    public string? SapCode { get; set; }
}

public sealed class ProductRequestDto
{
    public string? Active { get; set; }
    public ulong? SegmentId { get; set; }
    public ulong? FamilyId { get; set; }
    public string? PartNo { get; set; }
    public string? ProductName { get; set; }
    public decimal? Mrp { get; set; }
    public string? Attachment { get; set; }
}
