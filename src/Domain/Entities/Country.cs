namespace Domain.Entities;

public sealed class Country : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string CountryName { get; set; } = string.Empty;
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
