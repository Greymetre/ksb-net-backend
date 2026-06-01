namespace Domain.Entities;

public sealed class Holiday : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string? Name { get; set; }
    public string? HolidayDate { get; set; }
    public ulong? Branch { get; set; }
    public string HolidayFor { get; set; } = "branch";
    public ulong? DivisionId { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
}
