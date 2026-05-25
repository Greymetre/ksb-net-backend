namespace Domain.Entities;

public sealed class Pincode : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string PinCode { get; set; } = string.Empty;
    public ulong? CityId { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
