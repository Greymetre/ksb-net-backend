using Api.Filters;
using Application.DTOs.Hr;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class HrController : ControllerBase
{
    private readonly IHrService _hrService;

    public HrController(IHrService hrService)
    {
        _hrService = hrService;
    }

    [HttpGet("hr/options")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken) =>
        Ok(await _hrService.GetOptionsAsync(cancellationToken));

    [HttpGet("tours/ajax-user-districts")]
    public async Task<IActionResult> UserDistricts([FromQuery(Name = "user_id")] ulong userId, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetUserDistrictsAsync(userId, cancellationToken));

    [HttpGet("tours/ajax-user-cities-by-district")]
    public async Task<IActionResult> UserCities([FromQuery(Name = "user_id")] ulong userId, [FromQuery(Name = "district_id")] ulong districtId, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetUserCitiesAsync(userId, districtId, cancellationToken));

    [RequirePermission("holiday_access")]
    [HttpGet("holidays")]
    [HttpGet("holiday")]
    public async Task<IActionResult> Holidays([FromQuery] string? search, [FromQuery(Name = "holiday_for")] string? holidayFor, [FromQuery(Name = "branch_id")] ulong? branchId, [FromQuery(Name = "division_id")] ulong? divisionId, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetHolidaysAsync(new HolidayListFilterDto { Search = search, HolidayFor = holidayFor, BranchId = branchId, DivisionId = divisionId }, cancellationToken));

    [RequirePermission("holiday_access")]
    [HttpGet("holidays/{id}")]
    [HttpGet("holiday/{id}")]
    public async Task<IActionResult> Holiday(ulong id, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetHolidayAsync(id, cancellationToken));

    [RequirePermission("holiday_access")]
    [HttpPost("holidays")]
    [HttpPost("holiday")]
    public async Task<IActionResult> CreateHoliday([FromBody] HolidayRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _hrService.CreateHolidayAsync(request, CurrentUserId(), cancellationToken));

    [RequirePermission("holiday_access")]
    [HttpPut("holidays/{id}")]
    [HttpPatch("holidays/{id}")]
    [HttpPut("holiday/{id}")]
    [HttpPatch("holiday/{id}")]
    public async Task<IActionResult> UpdateHoliday(ulong id, [FromBody] HolidayRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _hrService.UpdateHolidayAsync(id, request, CurrentUserId(), cancellationToken));

    [RequirePermission("holiday_access")]
    [HttpDelete("holidays/{id}")]
    [HttpDelete("holiday/{id}")]
    public async Task<IActionResult> DeleteHoliday(ulong id, CancellationToken cancellationToken) =>
        Ok(await _hrService.DeleteHolidayAsync(id, CurrentUserId(), cancellationToken));

    [RequirePermission("holiday_access")]
    [HttpGet("holidays/export")]
    public async Task<IActionResult> ExportHolidays([FromQuery] string? search, [FromQuery(Name = "holiday_for")] string? holidayFor, [FromQuery(Name = "branch_id")] ulong? branchId, [FromQuery(Name = "division_id")] ulong? divisionId, CancellationToken cancellationToken)
    {
        var file = await _hrService.ExportHolidaysAsync(new HolidayListFilterDto { Search = search, HolidayFor = holidayFor, BranchId = branchId, DivisionId = divisionId }, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("leave_access")]
    [HttpGet("leaves")]
    public async Task<IActionResult> Leaves([FromQuery(Name = "executive_id")] ulong? executiveId, [FromQuery(Name = "start_date")] DateTime? startDate, [FromQuery(Name = "end_date")] DateTime? endDate, [FromQuery] string? status, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetLeavesAsync(new LeaveListFilterDto { ExecutiveId = executiveId, StartDate = startDate, EndDate = endDate, Status = status, Search = search }, cancellationToken));

    [RequirePermission("leave_access")]
    [HttpPost("leaves")]
    public async Task<IActionResult> CreateLeave([FromBody] LeaveRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _hrService.CreateLeaveAsync(request, CurrentUserId(), cancellationToken));

    [RequirePermission("leave_access")]
    [HttpDelete("leaves/{id}")]
    public async Task<IActionResult> DeleteLeave(ulong id, CancellationToken cancellationToken) =>
        Ok(await _hrService.DeleteLeaveAsync(id, cancellationToken));

    [RequirePermission("leave_access")]
    [HttpPost("leaves-approved")]
    [HttpPost("leaves/{id}/approve")]
    public async Task<IActionResult> ApproveLeave([FromRoute] ulong? id, [FromBody] IdRequest? request, CancellationToken cancellationToken) =>
        Ok(await _hrService.ApproveLeaveAsync(id ?? request?.Id ?? 0, cancellationToken));

    [RequirePermission("leave_access")]
    [HttpPost("leaverejected")]
    [HttpPost("leaves/{id}/reject")]
    public async Task<IActionResult> RejectLeave([FromRoute] ulong? id, [FromBody] LeaveStatusBody request, CancellationToken cancellationToken) =>
        Ok(await _hrService.RejectLeaveAsync(id ?? request.LeaveId ?? 0, new LeaveStatusRequestDto { RemarkStatus = request.RemarkStatus }, cancellationToken));

    [RequirePermission("leave_access")]
    [HttpPost("combo-off-leave")]
    public async Task<IActionResult> CompOff([FromBody] CompOffRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _hrService.CreateCompOffAsync(request, cancellationToken));

    [RequirePermission("leave_access")]
    [HttpGet("leaves-export")]
    [HttpGet("leaves/export")]
    public async Task<IActionResult> ExportLeaves([FromQuery(Name = "executive_id")] ulong? executiveId, [FromQuery(Name = "start_date")] DateTime? startDate, [FromQuery(Name = "end_date")] DateTime? endDate, [FromQuery] string? status, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var file = await _hrService.ExportLeavesAsync(new LeaveListFilterDto { ExecutiveId = executiveId, StartDate = startDate, EndDate = endDate, Status = status, Search = search }, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("tours")]
    [HttpGet("tours")]
    public async Task<IActionResult> Tours([FromQuery(Name = "executive_id")] ulong? executiveId, [FromQuery(Name = "division_id")] ulong? divisionId, [FromQuery(Name = "designation_id")] ulong? designationId, [FromQuery(Name = "start_date")] DateTime? startDate, [FromQuery(Name = "end_date")] DateTime? endDate, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetToursAsync(new TourListFilterDto { ExecutiveId = executiveId, DivisionId = divisionId, DesignationId = designationId, StartDate = startDate, EndDate = endDate, Search = search }, cancellationToken));

    [RequirePermission("tours")]
    [HttpGet("tours/{id}")]
    public async Task<IActionResult> Tour(ulong id, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetTourAsync(id, cancellationToken));

    [RequirePermission("tours")]
    [HttpPost("tours")]
    public async Task<IActionResult> CreateTour([FromBody] TourRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _hrService.CreateTourAsync(request, CurrentUserId(), cancellationToken));

    [RequirePermission("tours")]
    [HttpPut("tours/{id}")]
    [HttpPatch("tours/{id}")]
    public async Task<IActionResult> UpdateTour(ulong id, [FromBody] TourRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _hrService.UpdateTourAsync(id, request, CurrentUserId(), cancellationToken));

    [RequirePermission("tours")]
    [HttpDelete("tours/{id}")]
    public async Task<IActionResult> DeleteTour(ulong id, CancellationToken cancellationToken) =>
        Ok(await _hrService.DeleteTourAsync(id, cancellationToken));

    [RequirePermission("tours")]
    [HttpPost("tours-changeStatus")]
    [HttpPost("tours/change-status")]
    public async Task<IActionResult> ChangeTourStatus([FromBody] TourStatusRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _hrService.ChangeTourStatusAsync(request, CurrentUserId(), cancellationToken));

    [RequirePermission("tours")]
    [HttpGet("tours-download")]
    [HttpGet("tours/export")]
    public async Task<IActionResult> ExportTours([FromQuery(Name = "executive_id")] ulong? executiveId, [FromQuery(Name = "division_id")] ulong? divisionId, [FromQuery(Name = "designation_id")] ulong? designationId, [FromQuery(Name = "start_date")] DateTime? startDate, [FromQuery(Name = "end_date")] DateTime? endDate, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var file = await _hrService.ExportToursAsync(new TourListFilterDto { ExecutiveId = executiveId, DivisionId = divisionId, DesignationId = designationId, StartDate = startDate, EndDate = endDate, Search = search }, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("tours")]
    [HttpGet("tours-template")]
    [HttpGet("tours/template")]
    public async Task<IActionResult> TourTemplate(CancellationToken cancellationToken)
    {
        var file = await _hrService.GetTourTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("tours")]
    [HttpPost("tours-upload")]
    [HttpPost("tours/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadTours(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        return Ok(await _hrService.UploadToursAsync(stream, CurrentUserId(), cancellationToken));
    }

    [RequirePermission("attendance_report")]
    [HttpGet("attendances")]
    [HttpGet("attendancesInfo")]
    public async Task<IActionResult> Attendances([FromQuery(Name = "executive_id")] ulong? executiveId, [FromQuery(Name = "designation_id")] ulong? designationId, [FromQuery(Name = "start_date")] DateTime? startDate, [FromQuery(Name = "end_date")] DateTime? endDate, [FromQuery] string? status, [FromQuery] string? type, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetAttendancesAsync(new AttendanceListFilterDto { ExecutiveId = executiveId, DesignationId = designationId, StartDate = startDate, EndDate = endDate, Status = status, Type = type, Search = search }, cancellationToken));

    [RequirePermission("attendance_report")]
    [HttpGet("attendance-plan")]
    public async Task<IActionResult> AttendancePlan([FromQuery(Name = "user_id")] ulong? userId, [FromQuery] DateTime? date, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetAttendancePlanAsync(new AttendancePlanLookupDto { UserId = userId, Date = date }, cancellationToken));

    [RequirePermission("attendance_report")]
    [HttpPost("get-tour-and-beat-plan")]
    public async Task<IActionResult> AttendancePlanPost([FromBody] AttendancePlanLookupDto request, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetAttendancePlanAsync(request, cancellationToken));

    [RequirePermission("attendance_report")]
    [HttpPost("attendances")]
    [HttpPost("submitAttendances")]
    [HttpPost("attendances/punch-in")]
    public async Task<IActionResult> PunchIn([FromBody] AttendancePunchInRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _hrService.PunchInAsync(request, CurrentUserId(), cancellationToken));

    [RequirePermission("attendance_report")]
    [HttpPost("punchoutnow")]
    [HttpPost("attendances/punch-out")]
    public async Task<IActionResult> PunchOut([FromBody] AttendancePunchOutRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _hrService.PunchOutAsync(request, cancellationToken));

    [RequirePermission("attendance_report")]
    [HttpPost("removePunchout")]
    public async Task<IActionResult> RemovePunchOut([FromBody] IdRequest request, CancellationToken cancellationToken) =>
        Ok(await _hrService.RemovePunchOutAsync(request.Id, cancellationToken));

    [RequirePermission("attendance_report")]
    [HttpPost("approveAttendance")]
    [HttpPost("attendances/approve")]
    public async Task<IActionResult> ApproveAttendance([FromBody] IdsRequest request, CancellationToken cancellationToken) =>
        Ok(await _hrService.ApproveAttendanceAsync(request.Ids(), CurrentUserId(), cancellationToken));

    [RequirePermission("attendance_report")]
    [HttpPost("rejectAttendance")]
    [HttpPost("attendances/reject")]
    public async Task<IActionResult> RejectAttendance([FromBody] AttendanceRejectRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _hrService.RejectAttendanceAsync(request, CurrentUserId(), cancellationToken));

    [RequirePermission("attendance_report")]
    [HttpGet("attendance-download")]
    [HttpGet("attendances/export")]
    public async Task<IActionResult> ExportAttendances([FromQuery(Name = "executive_id")] ulong? executiveId, [FromQuery(Name = "designation_id")] ulong? designationId, [FromQuery(Name = "start_date")] DateTime? startDate, [FromQuery(Name = "end_date")] DateTime? endDate, [FromQuery] string? status, [FromQuery] string? type, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var file = await _hrService.ExportAttendancesAsync(new AttendanceListFilterDto { ExecutiveId = executiveId, DesignationId = designationId, StartDate = startDate, EndDate = endDate, Status = status, Type = type, Search = search }, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("attendance_summary_report")]
    [HttpGet("attendance-summary")]
    public async Task<IActionResult> AttendanceSummary([FromQuery(Name = "executive_id")] ulong? executiveId, [FromQuery(Name = "designation_id")] ulong? designationId, [FromQuery(Name = "start_date")] DateTime? startDate, [FromQuery(Name = "end_date")] DateTime? endDate, CancellationToken cancellationToken) =>
        Ok(await _hrService.GetAttendanceSummaryAsync(new AttendanceListFilterDto { ExecutiveId = executiveId, DesignationId = designationId, StartDate = startDate, EndDate = endDate }, cancellationToken));

    [RequirePermission("attendance_summary_report")]
    [HttpGet("attendancesummary-download")]
    [HttpGet("attendance-summary/export")]
    public async Task<IActionResult> ExportAttendanceSummary([FromQuery(Name = "executive_id")] ulong? executiveId, [FromQuery(Name = "designation_id")] ulong? designationId, [FromQuery(Name = "start_date")] DateTime? startDate, [FromQuery(Name = "end_date")] DateTime? endDate, CancellationToken cancellationToken)
    {
        var file = await _hrService.ExportAttendanceSummaryAsync(new AttendanceListFilterDto { ExecutiveId = executiveId, DesignationId = designationId, StartDate = startDate, EndDate = endDate }, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }

    public sealed class IdRequest
    {
        public ulong Id { get; init; }
    }

    public sealed class IdsRequest
    {
        public ulong[]? Id { get; init; }
        public string? IdsCsv { get; init; }
        public ulong[] Ids() => Id?.Length > 0 ? Id : (IdsCsv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(ulong.Parse).ToArray();
    }

    public sealed class LeaveStatusBody
    {
        public ulong? LeaveId { get; init; }
        public string? RemarkStatus { get; init; }
    }
}
