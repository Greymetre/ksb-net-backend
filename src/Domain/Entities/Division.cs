namespace Domain.Entities;

public sealed class Division : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string DivisionName { get; set; } = string.Empty;
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
