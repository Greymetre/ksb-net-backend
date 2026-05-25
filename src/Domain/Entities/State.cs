namespace Domain.Entities;

public sealed class State : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string StateName { get; set; } = string.Empty;
    public ulong? CountryId { get; set; }
    public string? GstCode { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
