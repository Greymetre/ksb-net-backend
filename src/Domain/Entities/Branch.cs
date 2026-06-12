namespace Domain.Entities;

public sealed class Branch : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string BranchName { get; set; } = string.Empty;
    public string? BranchCode { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
