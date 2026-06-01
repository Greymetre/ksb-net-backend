namespace Domain.Entities;

public sealed class TourProgramme : BaseEntity
{
    public DateTime? Date { get; set; }
    public ulong? UserId { get; set; }
    public string Town { get; set; } = string.Empty;
    public long? District { get; set; }
    public string Objectives { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Status { get; set; }
    public ulong? CreatedBy { get; set; }
}
