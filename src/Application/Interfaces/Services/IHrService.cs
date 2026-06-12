using Application.DTOs.Hr;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IHrService
{
    Task<LaravelApiResponse> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetUserDistrictsAsync(ulong userId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetUserCitiesAsync(ulong userId, ulong districtId, CancellationToken cancellationToken);

    Task<LaravelApiResponse> GetHolidaysAsync(HolidayListFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetHolidayAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateHolidayAsync(HolidayRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateHolidayAsync(ulong id, HolidayRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteHolidayAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task<HrFileDto> ExportHolidaysAsync(HolidayListFilterDto filter, CancellationToken cancellationToken);

    Task<LaravelApiResponse> GetLeavesAsync(LeaveListFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateLeaveAsync(LeaveRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteLeaveAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> ApproveLeaveAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> RejectLeaveAsync(ulong id, LeaveStatusRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateCompOffAsync(CompOffRequestDto request, CancellationToken cancellationToken);
    Task<HrFileDto> ExportLeavesAsync(LeaveListFilterDto filter, CancellationToken cancellationToken);

    Task<LaravelApiResponse> GetToursAsync(TourListFilterDto filter, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetTourAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateTourAsync(TourRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateTourAsync(ulong id, TourRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteTourAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> ChangeTourStatusAsync(TourStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<HrFileDto> ExportToursAsync(TourListFilterDto filter, ulong? actorUserId, CancellationToken cancellationToken);
    Task<HrFileDto> GetTourTemplateAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> UploadToursAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken);

    Task<LaravelApiResponse> GetAttendancesAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetAttendancePlanAsync(AttendancePlanLookupDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> PunchInAsync(AttendancePunchInRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> PunchOutAsync(AttendancePunchOutRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> RemovePunchOutAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> ApproveAttendanceAsync(ulong[] ids, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> RejectAttendanceAsync(AttendanceRejectRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<HrFileDto> ExportAttendancesAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetAttendanceSummaryAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken);
    Task<HrFileDto> ExportAttendanceSummaryAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken);
}
