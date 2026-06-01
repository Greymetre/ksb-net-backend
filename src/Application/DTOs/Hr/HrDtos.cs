namespace Application.DTOs.Hr;

public sealed class HrFileDto
{
    public string FileName { get; init; } = "export.xlsx";
    public string ContentType { get; init; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public byte[] Content { get; init; } = [];
}

public sealed class HrLookupDto
{
    public ulong Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class HolidayListFilterDto
{
    public string? Search { get; init; }
    public string? HolidayFor { get; init; }
    public ulong? BranchId { get; init; }
    public ulong? DivisionId { get; init; }
}

public sealed class HolidayDto
{
    public ulong Id { get; init; }
    public string Active { get; init; } = "Y";
    public ulong? Branch { get; init; }
    public string? BranchName { get; init; }
    public string HolidayFor { get; init; } = "branch";
    public ulong? DivisionId { get; init; }
    public string? DivisionName { get; init; }
    public string? Name { get; init; }
    public string? HolidayDate { get; init; }
    public string[] Names { get; init; } = [];
    public string[] HolidayDates { get; init; } = [];
    public ulong? CreatedBy { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public sealed class HolidayRequestDto
{
    public string Active { get; init; } = "Y";
    public ulong? Branch { get; init; }
    public string? HolidayFor { get; init; }
    public ulong? DivisionId { get; init; }
    public string[]? Name { get; init; }
    public string[]? HolidayDate { get; init; }
    public string[]? Names { get; init; }
    public string[]? HolidayDates { get; init; }
}

public sealed class LeaveListFilterDto
{
    public ulong? ExecutiveId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
}

public sealed class LeaveDto
{
    public ulong Id { get; init; }
    public string Active { get; init; } = "Y";
    public ulong? UserId { get; init; }
    public string? UserName { get; init; }
    public string? EmployeeCode { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? BalType { get; init; }
    public string Reason { get; init; } = string.Empty;
    public int? Status { get; init; }
    public string StatusLabel { get; init; } = "Pending";
    public string? RemarkStatus { get; init; }
    public ulong? CreatedBy { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public sealed class LeaveRequestDto
{
    public ulong? UserId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Type { get; init; }
    public string? BalType { get; init; }
    public string? Reason { get; init; }
}

public sealed class LeaveStatusRequestDto
{
    public string? RemarkStatus { get; init; }
}

public sealed class CompOffRequestDto
{
    public ulong? UserId { get; init; }
    public DateTime? ComboOffDate { get; init; }
}

public sealed class TourListFilterDto
{
    public ulong? ExecutiveId { get; init; }
    public ulong? DivisionId { get; init; }
    public ulong? DesignationId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Search { get; init; }
}

public sealed class TourDto
{
    public ulong Id { get; init; }
    public DateTime? Date { get; init; }
    public ulong? UserId { get; init; }
    public string? UserName { get; init; }
    public string? EmployeeCode { get; init; }
    public string Town { get; init; } = string.Empty;
    public string? TownName { get; init; }
    public string? District { get; init; }
    public string? DistrictName { get; init; }
    public string Objectives { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = "0";
    public string StatusLabel { get; init; } = "Pending";
    public DateTime? CreatedAt { get; init; }
}

public sealed class TourRequestDto
{
    public DateTime? Date { get; init; }
    public ulong? UserId { get; init; }
    public string? Town { get; init; }
    public string? District { get; init; }
    public string? Objectives { get; init; }
}

public sealed class TourStatusRequestDto
{
    public ulong[]? Id { get; init; }
    public string? Status { get; init; }
}

public sealed class AttendanceListFilterDto
{
    public ulong? ExecutiveId { get; init; }
    public ulong? DesignationId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Status { get; init; }
    public string? Type { get; init; }
    public string? Search { get; init; }
}

public sealed class AttendanceDto
{
    public ulong Id { get; init; }
    public ulong? UserId { get; init; }
    public string? UserName { get; init; }
    public string? EmployeeCode { get; init; }
    public string? BranchName { get; init; }
    public DateTime PunchinDate { get; init; }
    public string PunchinTime { get; init; } = string.Empty;
    public DateTime? PunchoutDate { get; init; }
    public string? PunchoutTime { get; init; }
    public string WorkedTime { get; init; } = string.Empty;
    public string WorkingType { get; init; } = string.Empty;
    public int? AttendanceStatus { get; init; }
    public string AttendanceStatusLabel { get; init; } = "Pending";
    public string AttendanceLabel { get; init; } = string.Empty;
    public string? RemarkStatus { get; init; }
    public string PunchinSummary { get; init; } = string.Empty;
    public string PunchoutSummary { get; init; } = string.Empty;
    public string PunchinAddress { get; init; } = string.Empty;
    public string PunchoutAddress { get; init; } = string.Empty;
    public string? PunchinFrom { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public sealed class AttendancePunchInRequestDto
{
    public ulong? UserId { get; init; }
    public DateTime? PunchinDate { get; init; }
    public string? PunchinTime { get; init; }
    public string? PunchinSummary { get; init; }
    public string? WorkingType { get; init; }
    public ulong? TourId { get; init; }
    public string? City { get; init; }
}

public sealed class AttendancePlanLookupDto
{
    public ulong? UserId { get; init; }
    public DateTime? Date { get; init; }
}

public sealed class AttendancePlanResponseDto
{
    public AttendancePlanSectionDto Tour { get; init; } = new();
    public AttendanceBeatSectionDto Beat { get; init; } = new();
}

public sealed class AttendancePlanSectionDto
{
    public bool Exists { get; init; }
    public AttendanceTourPlanDto? Data { get; init; }
}

public sealed class AttendanceBeatSectionDto
{
    public bool Exists { get; init; }
    public AttendanceBeatPlanDto? Data { get; init; }
}

public sealed class AttendanceTourPlanDto
{
    public ulong Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Objectives { get; init; } = string.Empty;
    public string CityName { get; init; } = string.Empty;
    public string? CityId { get; init; }
}

public sealed class AttendanceBeatPlanDto
{
    public string BeatName { get; init; } = string.Empty;
    public string AreaTown { get; init; } = string.Empty;
    public string? CityId { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class AttendancePunchOutRequestDto
{
    public ulong? Id { get; init; }
    public string? PunchoutSummary { get; init; }
}

public sealed class AttendanceRejectRequestDto
{
    public ulong[]? Id { get; init; }
    public string? RemarkStatus { get; init; }
}

public sealed class AttendanceSummaryDto
{
    public ulong UserId { get; init; }
    public string? EmployeeCode { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? ReportingManagers { get; init; }
    public IReadOnlyDictionary<string, string> Days { get; init; } = new Dictionary<string, string>();
    public int WeekOff { get; init; }
    public int Absent { get; init; }
    public int HalfDay { get; init; }
    public int Holiday { get; init; }
    public int Present { get; init; }
    public int TotalDays { get; init; }
}
