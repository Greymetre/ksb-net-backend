namespace Domain.Entities;

public sealed class City : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string CityName { get; set; } = string.Empty;
    public ulong? DistrictId { get; set; }
    public ulong? StateId { get; set; }
    public string? Grade { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
