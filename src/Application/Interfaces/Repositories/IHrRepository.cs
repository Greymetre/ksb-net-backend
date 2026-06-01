using Application.DTOs.Hr;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IHrRepository
{
    Task<IReadOnlyList<HrLookupDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<HrLookupDto>> GetBranchesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<HrLookupDto>> GetDivisionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<HrLookupDto>> GetDesignationsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<HrLookupDto>> GetDistrictsByUserAsync(ulong userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HrLookupDto>> GetCitiesByUserAndDistrictAsync(ulong userId, ulong districtId, CancellationToken cancellationToken);

    Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(HolidayListFilterDto filter, CancellationToken cancellationToken);
    Task<Holiday?> GetHolidayEntityAsync(ulong id, CancellationToken cancellationToken);
    Task<HolidayDto?> GetHolidayAsync(ulong id, CancellationToken cancellationToken);
    Task AddHolidayAsync(Holiday holiday, CancellationToken cancellationToken);
    Task DeleteHolidayAsync(Holiday holiday, ulong? actorUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaveDto>> GetLeavesAsync(LeaveListFilterDto filter, CancellationToken cancellationToken);
    Task<Leave?> GetLeaveEntityAsync(ulong id, CancellationToken cancellationToken);
    Task<LeaveDto?> GetLeaveAsync(ulong id, CancellationToken cancellationToken);
    Task AddLeaveAsync(Leave leave, CancellationToken cancellationToken);
    Task DeleteLeaveAsync(Leave leave, CancellationToken cancellationToken);

    Task<IReadOnlyList<TourDto>> GetToursAsync(TourListFilterDto filter, CancellationToken cancellationToken);
    Task<TourProgramme?> GetTourEntityAsync(ulong id, CancellationToken cancellationToken);
    Task<TourDto?> GetTourAsync(ulong id, CancellationToken cancellationToken);
    Task AddTourAsync(TourProgramme tour, CancellationToken cancellationToken);
    Task DeleteTourAsync(TourProgramme tour, CancellationToken cancellationToken);
    Task AddTourLogAsync(TourLog log, CancellationToken cancellationToken);
    Task UpsertTourDetailAsync(ulong tourId, ulong cityId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceDto>> GetAttendancesAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken);
    Task<AttendancePlanResponseDto> GetAttendancePlanAsync(ulong userId, DateTime date, CancellationToken cancellationToken);
    Task<Attendance?> GetAttendanceEntityAsync(ulong id, CancellationToken cancellationToken);
    Task<Attendance?> GetAttendanceByUserDateAsync(ulong userId, DateTime date, CancellationToken cancellationToken);
    Task AddAttendanceAsync(Attendance attendance, CancellationToken cancellationToken);
    Task<IReadOnlyList<Attendance>> GetAttendanceEntitiesInRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

    Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetReportUsersAsync(ulong? executiveId, ulong? designationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Holiday>> GetActiveHolidaysAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Leave>> GetLeavesForUserDateRangeAsync(ulong userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompOffLeave>> GetAvailableCompOffsAsync(ulong userId, CancellationToken cancellationToken);
    Task AddCompOffAsync(CompOffLeave compOff, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
