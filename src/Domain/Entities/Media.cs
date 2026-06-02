namespace Domain.Entities;

public sealed class Media : BaseEntity
{
    public string ModelType { get; set; } = string.Empty;
    public ulong ModelId { get; set; }
    public string? Uuid { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public string Disk { get; set; } = "public";
    public string? ConversionsDisk { get; set; }
    public ulong Size { get; set; }
    public string Manipulations { get; set; } = "[]";
    public string CustomProperties { get; set; } = "[]";
    public string GeneratedConversions { get; set; } = "[]";
    public string ResponsiveImages { get; set; } = "[]";
    public uint? OrderColumn { get; set; }
}
