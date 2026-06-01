namespace Domain.Entities;

public sealed class TourLog : BaseEntity
{
    public ulong? TourProgrammeId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ulong? PerformedBy { get; set; }
    public string? Remark { get; set; }
}
