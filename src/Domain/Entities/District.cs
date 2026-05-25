namespace Domain.Entities;

public sealed class District : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string DistrictName { get; set; } = string.Empty;
    public ulong? StateId { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
