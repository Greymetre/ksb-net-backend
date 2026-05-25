namespace Domain.Entities;

public sealed class Department : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string Name { get; set; } = string.Empty;
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
