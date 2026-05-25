namespace Domain.Entities;

public sealed class Designation : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string DesignationName { get; set; } = string.Empty;
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
