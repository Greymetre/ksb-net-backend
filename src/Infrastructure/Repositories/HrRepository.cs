using Application.DTOs.Hr;
using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class HrRepository : IHrRepository
{
    private const string DistributorRoleName = "Distributor";
    private const int MaxRows = 50000;
    private readonly AppDbContext _db;

    public HrRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<HrLookupDto>> GetUsersAsync(CancellationToken cancellationToken) =>
        await InternalUsersQuery(_db.Users.AsNoTracking())
            .Where(x => x.Active == "Y" && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Take(MaxRows)
            .Select(x => new HrLookupDto { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HrLookupDto>> GetBranchesAsync(CancellationToken cancellationToken) =>
        await _db.Branches.AsNoTracking().OrderBy(x => x.BranchName)
            .Select(x => new HrLookupDto { Id = x.Id, Name = x.BranchName })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HrLookupDto>> GetDivisionsAsync(CancellationToken cancellationToken) =>
        await _db.Divisions.AsNoTracking().OrderBy(x => x.DivisionName)
            .Select(x => new HrLookupDto { Id = x.Id, Name = x.DivisionName })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HrLookupDto>> GetDesignationsAsync(CancellationToken cancellationToken) =>
        await _db.Designations.AsNoTracking().OrderBy(x => x.DesignationName)
            .Select(x => new HrLookupDto { Id = x.Id, Name = x.DesignationName })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HrLookupDto>> GetDistrictsByUserAsync(ulong userId, CancellationToken cancellationToken) =>
        await (from assign in _db.UserCityAssigns.AsNoTracking()
               join city in _db.Cities.AsNoTracking() on assign.CityId equals city.Id
               join district in _db.Districts.AsNoTracking() on city.DistrictId equals district.Id
               where assign.UserId == userId
               orderby district.DistrictName
               select new HrLookupDto { Id = district.Id, Name = district.DistrictName })
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HrLookupDto>> GetCitiesByUserAndDistrictAsync(ulong userId, ulong districtId, CancellationToken cancellationToken) =>
        await (from assign in _db.UserCityAssigns.AsNoTracking()
               join city in _db.Cities.AsNoTracking() on assign.CityId equals city.Id
               where assign.UserId == userId && city.DistrictId == districtId
               orderby city.CityName
               select new HrLookupDto { Id = city.Id, Name = city.CityName })
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(HolidayListFilterDto filter, CancellationToken cancellationToken)
    {
        var query = from h in _db.Holidays.AsNoTracking()
                    join branch in _db.Branches.AsNoTracking() on h.Branch equals branch.Id into branches
                    from branch in branches.DefaultIfEmpty()
                    join creator in _db.Users.AsNoTracking() on h.CreatedBy equals creator.Id into creators
                    from creator in creators.DefaultIfEmpty()
                    select new { h, branch, creator };

        if (filter.BranchId.HasValue) query = query.Where(x => x.h.Branch == filter.BranchId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => (x.h.Name ?? "").Contains(search) || (x.h.HolidayDate ?? "").Contains(search) || (x.branch.BranchName ?? "").Contains(search));
        }

        return await query.OrderByDescending(x => x.h.Id)
            .Select(x => new HolidayDto
            {
                Id = x.h.Id,
                Active = x.h.Active,
                Branch = x.h.Branch,
                BranchName = x.branch.BranchName,
                Name = x.h.Name,
                HolidayDate = x.h.HolidayDate,
                Names = SplitCsv(x.h.Name),
                HolidayDates = SplitCsv(x.h.HolidayDate),
                CreatedBy = x.h.CreatedBy,
                CreatedByName = x.creator.Name,
                CreatedAt = x.h.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public Task<Holiday?> GetHolidayEntityAsync(ulong id, CancellationToken cancellationToken) =>
        _db.Holidays.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<HolidayDto?> GetHolidayAsync(ulong id, CancellationToken cancellationToken) =>
        (await GetHolidaysAsync(new HolidayListFilterDto(), cancellationToken)).FirstOrDefault(x => x.Id == id);

    public async Task AddHolidayAsync(Holiday holiday, CancellationToken cancellationToken)
    {
        _db.Holidays.Add(holiday);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteHolidayAsync(Holiday holiday, ulong? actorUserId, CancellationToken cancellationToken)
    {
        holiday.DeletedAt = DateTime.UtcNow;
        holiday.UpdatedBy = actorUserId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveDto>> GetLeavesAsync(LeaveListFilterDto filter, CancellationToken cancellationToken)
    {
        var query = from leave in _db.Leaves.AsNoTracking()
                    join user in _db.Users.AsNoTracking() on leave.UserId equals user.Id into users
                    from user in users.DefaultIfEmpty()
                    join creator in _db.Users.AsNoTracking() on leave.CreatedBy equals creator.Id into creators
                    from creator in creators.DefaultIfEmpty()
                    select new { leave, user, creator };

        if (filter.ExecutiveId.HasValue) query = query.Where(x => x.leave.UserId == filter.ExecutiveId);
        if (filter.StartDate.HasValue) query = query.Where(x => x.leave.ToDate >= filter.StartDate.Value.Date);
        if (filter.EndDate.HasValue) query = query.Where(x => x.leave.FromDate <= filter.EndDate.Value.Date);
        if (int.TryParse(filter.Status, out var status)) query = query.Where(x => x.leave.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => (x.user.Name ?? "").Contains(search) || (x.leave.Type ?? "").Contains(search) || (x.leave.Reason ?? "").Contains(search));
        }

        return await query.OrderByDescending(x => x.leave.Id)
            .Select(x => new LeaveDto
            {
                Id = x.leave.Id,
                Active = x.leave.Active,
                UserId = x.leave.UserId,
                UserName = x.user.Name,
                EmployeeCode = x.user.EmployeeCodes,
                FromDate = x.leave.FromDate,
                ToDate = x.leave.ToDate,
                Type = x.leave.Type,
                BalType = x.leave.BalType,
                Reason = x.leave.Reason,
                Status = x.leave.Status,
                StatusLabel = x.leave.Status == 1 ? "Approved" : x.leave.Status == 2 ? "Rejected" : "Pending",
                RemarkStatus = x.leave.RemarkStatus,
                CreatedBy = x.leave.CreatedBy,
                CreatedByName = x.creator.Name,
                CreatedAt = x.leave.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public Task<Leave?> GetLeaveEntityAsync(ulong id, CancellationToken cancellationToken) =>
        _db.Leaves.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<LeaveDto?> GetLeaveAsync(ulong id, CancellationToken cancellationToken) =>
        (await GetLeavesAsync(new LeaveListFilterDto(), cancellationToken)).FirstOrDefault(x => x.Id == id);

    public async Task AddLeaveAsync(Leave leave, CancellationToken cancellationToken)
    {
        _db.Leaves.Add(leave);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLeaveAsync(Leave leave, CancellationToken cancellationToken)
    {
        _db.Leaves.Remove(leave);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TourDto>> GetToursAsync(TourListFilterDto filter, CancellationToken cancellationToken)
    {
        var query = from tour in _db.TourProgrammes.AsNoTracking()
                    join user in _db.Users.AsNoTracking() on tour.UserId equals user.Id into users
                    from user in users.DefaultIfEmpty()
                    select new { tour, user };

        if (filter.ExecutiveId.HasValue) query = query.Where(x => x.tour.UserId == filter.ExecutiveId);
        if (filter.DivisionId.HasValue) query = query.Where(x => x.user.DivisionId == filter.DivisionId);
        if (filter.DesignationId.HasValue) query = query.Where(x => x.user.DesignationId == filter.DesignationId);
        if (filter.StartDate.HasValue) query = query.Where(x => x.tour.Date >= filter.StartDate.Value.Date);
        if (filter.EndDate.HasValue) query = query.Where(x => x.tour.Date <= filter.EndDate.Value.Date);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => (x.user.Name ?? "").Contains(search) || x.tour.Objectives.Contains(search) || x.tour.Type.Contains(search));
        }

        var rows = await query.OrderByDescending(x => x.tour.CreatedAt ?? DateTime.MinValue)
            .Select(x => new { x.tour, UserName = x.user.Name, EmployeeCode = x.user.EmployeeCodes })
            .ToListAsync(cancellationToken);

        var cityIds = rows.Select(x => ParseUlong(x.tour.Town)).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var districtIds = rows.Select(x => ToUlong(x.tour.District)).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var cities = await _db.Cities.AsNoTracking()
            .Where(x => cityIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.CityName, cancellationToken);
        var districts = await _db.Districts.AsNoTracking()
            .Where(x => districtIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DistrictName, cancellationToken);

        return rows.Select(x =>
        {
            var cityId = ParseUlong(x.tour.Town);
            var districtId = ToUlong(x.tour.District);
            return new TourDto
            {
                Id = x.tour.Id,
                Date = x.tour.Date,
                UserId = x.tour.UserId,
                UserName = x.UserName,
                EmployeeCode = x.EmployeeCode,
                Town = x.tour.Town,
                TownName = cityId.HasValue && cities.TryGetValue(cityId.Value, out var cityName) ? cityName : x.tour.Town,
                District = x.tour.District?.ToString(),
                DistrictName = districtId.HasValue && districts.TryGetValue(districtId.Value, out var districtName) ? districtName : x.tour.District?.ToString(),
                Objectives = x.tour.Objectives,
                Type = x.tour.Type,
                Status = x.tour.Status.ToString(),
                StatusLabel = x.tour.Status == 1 ? "Approved" : x.tour.Status == 2 ? "Rejected" : "Pending",
                CreatedAt = x.tour.CreatedAt
            };
        }).ToList();
    }

    public Task<TourProgramme?> GetTourEntityAsync(ulong id, CancellationToken cancellationToken) =>
        _db.TourProgrammes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<TourDto?> GetTourAsync(ulong id, CancellationToken cancellationToken) =>
        (await GetToursAsync(new TourListFilterDto(), cancellationToken)).FirstOrDefault(x => x.Id == id);

    public async Task AddTourAsync(TourProgramme tour, CancellationToken cancellationToken)
    {
        _db.TourProgrammes.Add(tour);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTourAsync(TourProgramme tour, CancellationToken cancellationToken)
    {
        _db.TourProgrammes.Remove(tour);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTourLogAsync(TourLog log, CancellationToken cancellationToken)
    {
        _db.TourLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertTourDetailAsync(ulong tourId, ulong cityId, CancellationToken cancellationToken)
    {
        var detail = await _db.TourDetails.FirstOrDefaultAsync(x => x.TourId == tourId && x.CityId == cityId, cancellationToken);
        if (detail is null)
        {
            _db.TourDetails.Add(new TourDetail { TourId = tourId, CityId = cityId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }
        else
        {
            detail.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceDto>> GetAttendancesAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken)
    {
        var query = from attendance in _db.Attendances.AsNoTracking()
                    join user in _db.Users.AsNoTracking() on attendance.UserId equals user.Id into users
                    from user in users.DefaultIfEmpty()
                    join branch in _db.Branches.AsNoTracking() on user.PrimaryBranchId equals branch.Id into branches
                    from branch in branches.DefaultIfEmpty()
                    select new { attendance, user, branch };

        if (filter.ExecutiveId.HasValue) query = query.Where(x => x.attendance.UserId == filter.ExecutiveId);
        if (filter.DesignationId.HasValue) query = query.Where(x => x.user.DesignationId == filter.DesignationId);
        if (filter.StartDate.HasValue) query = query.Where(x => x.attendance.PunchinDate >= filter.StartDate.Value.Date);
        if (filter.EndDate.HasValue) query = query.Where(x => x.attendance.PunchinDate <= filter.EndDate.Value.Date);
        if (int.TryParse(filter.Status, out var status)) query = query.Where(x => x.attendance.AttendanceStatus == status);
        if (!string.IsNullOrWhiteSpace(filter.Type)) query = query.Where(x => x.attendance.WorkingType == filter.Type);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => (x.user.Name ?? "").Contains(search) || x.attendance.WorkingType.Contains(search) || x.attendance.PunchinSummary.Contains(search));
        }

        return await query.OrderByDescending(x => x.attendance.PunchinDate).ThenByDescending(x => x.attendance.Id)
            .Select(x => new AttendanceDto
            {
                Id = x.attendance.Id,
                UserId = x.attendance.UserId,
                UserName = x.user.Name,
                EmployeeCode = x.user.EmployeeCodes,
                BranchName = x.branch.BranchName,
                PunchinDate = x.attendance.PunchinDate,
                PunchinTime = x.attendance.PunchinTime.ToString(@"hh\:mm"),
                PunchoutDate = x.attendance.PunchoutDate,
                PunchoutTime = x.attendance.PunchoutTime == null ? null : x.attendance.PunchoutTime.Value.ToString(@"hh\:mm"),
                WorkedTime = x.attendance.WorkedTime,
                WorkingType = x.attendance.WorkingType,
                AttendanceStatus = x.attendance.AttendanceStatus,
                AttendanceStatusLabel = x.attendance.AttendanceStatus == 1 ? "Approved" : x.attendance.AttendanceStatus == 2 ? "Rejected" : "Pending",
                AttendanceLabel = x.attendance.PunchoutTime == null ? "Misspunch" : "Present",
                RemarkStatus = x.attendance.RemarkStatus,
                PunchinSummary = x.attendance.PunchinSummary,
                PunchoutSummary = x.attendance.PunchoutSummary,
                PunchinAddress = x.attendance.PunchinAddress,
                PunchoutAddress = x.attendance.PunchoutAddress,
                PunchinFrom = x.attendance.PunchinFrom,
                CreatedAt = x.attendance.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AttendancePlanResponseDto> GetAttendancePlanAsync(ulong userId, DateTime date, CancellationToken cancellationToken)
    {
        var tour = await _db.TourProgrammes.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date == date.Date)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        AttendanceTourPlanDto? tourData = null;
        if (tour is not null)
        {
            var cityId = ParseUlong(tour.Town);
            var cityName = cityId.HasValue
                ? await _db.Cities.AsNoTracking()
                    .Where(x => x.Id == cityId.Value)
                    .Select(x => x.CityName)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            tourData = new AttendanceTourPlanDto
            {
                Id = tour.Id,
                Name = string.IsNullOrWhiteSpace(tour.Objectives) ? "Tour Plan" : tour.Objectives,
                Objectives = string.IsNullOrWhiteSpace(tour.Objectives) ? "-" : tour.Objectives,
                CityName = string.IsNullOrWhiteSpace(cityName) ? "-" : cityName,
                CityId = tour.Town
            };
        }

        var beatRows = await (from schedule in _db.BeatSchedules.AsNoTracking()
                              join beat in _db.Beats.AsNoTracking() on schedule.BeatId equals beat.Id
                              where schedule.UserId == userId && schedule.BeatDate == date.Date
                              orderby beat.BeatName
                              select new { beat.BeatName, beat.Description, beat.CityId })
            .ToListAsync(cancellationToken);

        AttendanceBeatPlanDto? beatData = null;
        if (beatRows.Count > 0)
        {
            var mainBeat = beatRows[0];
            var beatCityId = ParseUlong(mainBeat.CityId);
            var beatCityName = beatCityId.HasValue
                ? await _db.Cities.AsNoTracking()
                    .Where(x => x.Id == beatCityId.Value)
                    .Select(x => x.CityName)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            beatData = new AttendanceBeatPlanDto
            {
                BeatName = string.Join(", ", beatRows.Select(x => x.BeatName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()),
                AreaTown = string.IsNullOrWhiteSpace(beatCityName) ? "-" : beatCityName,
                CityId = mainBeat.CityId,
                Description = string.IsNullOrWhiteSpace(mainBeat.Description) ? "-" : mainBeat.Description
            };
        }

        return new AttendancePlanResponseDto
        {
            Tour = new AttendancePlanSectionDto { Exists = tourData is not null, Data = tourData },
            Beat = new AttendanceBeatSectionDto { Exists = beatData is not null, Data = beatData }
        };
    }

    public Task<Attendance?> GetAttendanceEntityAsync(ulong id, CancellationToken cancellationToken) =>
        _db.Attendances.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Attendance?> GetAttendanceByUserDateAsync(ulong userId, DateTime date, CancellationToken cancellationToken) =>
        _db.Attendances.FirstOrDefaultAsync(x => x.UserId == userId && x.PunchinDate == date.Date, cancellationToken);

    public async Task AddAttendanceAsync(Attendance attendance, CancellationToken cancellationToken)
    {
        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Attendance>> GetAttendanceEntitiesInRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken) =>
        await _db.Attendances.AsNoTracking()
            .Where(x => x.PunchinDate >= startDate.Date && x.PunchinDate <= endDate.Date)
            .ToListAsync(cancellationToken);

    public Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken) =>
        _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> GetReportUsersAsync(ulong? executiveId, ulong? designationId, CancellationToken cancellationToken)
    {
        var query = _db.Users.AsNoTracking().Where(x => x.Active == "Y" && !x.IsDeleted && x.ShowAttandanceReport == "1");
        if (executiveId.HasValue) query = query.Where(x => x.Id == executiveId);
        if (designationId.HasValue) query = query.Where(x => x.DesignationId == designationId);
        return await query.OrderBy(x => x.Name).Take(MaxRows).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Holiday>> GetActiveHolidaysAsync(CancellationToken cancellationToken) =>
        await _db.Holidays.AsNoTracking().Where(x => x.Active == "Y").ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Leave>> GetLeavesForUserDateRangeAsync(ulong userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken) =>
        await _db.Leaves.AsNoTracking()
            .Where(x => x.UserId == userId && x.ToDate >= startDate.Date && x.FromDate <= endDate.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CompOffLeave>> GetAvailableCompOffsAsync(ulong userId, CancellationToken cancellationToken) =>
        await _db.CompOffLeaves
            .Where(x => x.UserId == (long)userId && !x.IsUsed && x.ExpiryDate >= DateTime.Today && x.Balance > 0)
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync(cancellationToken);

    public async Task AddCompOffAsync(CompOffLeave compOff, CancellationToken cancellationToken)
    {
        _db.CompOffLeaves.Add(compOff);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);

    private static string[] SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static ulong? ParseUlong(string? value) =>
        ulong.TryParse(value, out var id) && id > 0 ? id : null;

    private static ulong? ToUlong(long? value) =>
        value.HasValue && value.Value > 0 ? (ulong)value.Value : null;

    private IQueryable<User> InternalUsersQuery(IQueryable<User> query) =>
        query.Where(user =>
            !user.CustomerId.HasValue
            && !_db.ModelHasRoles
                .Join(_db.Roles, modelRole => modelRole.RoleId, role => role.Id, (modelRole, role) => new { modelRole, role })
                .Any(x => x.modelRole.ModelId == user.Id && x.modelRole.ModelType == LaravelModelTypes.User && x.role.Name == DistributorRoleName));
}
