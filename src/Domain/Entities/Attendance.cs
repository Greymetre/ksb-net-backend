namespace Domain.Entities;

public sealed class Attendance : BaseEntity
{
    public string Active { get; set; } = "Y";
    public ulong? UserId { get; set; }
    public DateTime PunchinDate { get; set; }
    public TimeSpan PunchinTime { get; set; }
    public string? PunchinLongitude { get; set; }
    public string? PunchinLatitude { get; set; }
    public string PunchinAddress { get; set; } = string.Empty;
    public string PunchinImage { get; set; } = string.Empty;
    public DateTime? PunchoutDate { get; set; }
    public TimeSpan? PunchoutTime { get; set; }
    public string? PunchoutLatitude { get; set; }
    public string? PunchoutLongitude { get; set; }
    public string PunchoutAddress { get; set; } = string.Empty;
    public string PunchoutImage { get; set; } = string.Empty;
    public string PunchinSummary { get; set; } = string.Empty;
    public string PunchoutSummary { get; set; } = string.Empty;
    public string WorkedTime { get; set; } = string.Empty;
    public string WorkingType { get; set; } = "fields";
    public int? AttendanceStatus { get; set; }
    public string? RemarkStatus { get; set; }
    public string? ApproveRejectBy { get; set; }
    public string? PunchinFrom { get; set; }
    public string? Flag { get; set; }
    public string? TourId { get; set; }
    public string? City { get; set; }
}
