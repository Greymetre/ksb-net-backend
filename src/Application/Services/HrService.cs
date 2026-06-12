using Application.Common;
using Application.DTOs.Hr;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;
using System.Globalization;

namespace Application.Services;

public sealed class HrService : IHrService
{
    private static readonly string[] LeaveTypes = ["First Half Leave", "Second Half Leave", "Full Day Leave"];
    private static readonly string[] BalanceTypes = ["Casual Leave"];
    private readonly IHrRepository _repository;

    public HrService(IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken)
    {
        return LaravelApiResponse.Success("options", new
        {
            users = await _repository.GetUsersAsync(actorUserId, cancellationToken),
            branches = await _repository.GetBranchesAsync(cancellationToken),
            divisions = await _repository.GetDivisionsAsync(cancellationToken),
            designations = await _repository.GetDesignationsAsync(cancellationToken),
            leave_types = LeaveTypes,
            balance_types = BalanceTypes,
            working_types = WorkingTypes()
        });
    }

    public async Task<LaravelApiResponse> GetUserDistrictsAsync(ulong userId, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("districts", await _repository.GetDistrictsByUserAsync(userId, cancellationToken));

    public async Task<LaravelApiResponse> GetUserCitiesAsync(ulong userId, ulong districtId, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("cities", await _repository.GetCitiesByUserAndDistrictAsync(userId, districtId, cancellationToken));

    public async Task<LaravelApiResponse> GetHolidaysAsync(HolidayListFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("holidays", await _repository.GetHolidaysAsync(filter, cancellationToken));

    public async Task<LaravelApiResponse> GetHolidayAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("holiday", await _repository.GetHolidayAsync(id, cancellationToken) ?? throw NotFound("Holiday not found"));

    public async Task<LaravelApiResponse> CreateHolidayAsync(HolidayRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var names = request.Names ?? request.Name ?? [];
        var dates = request.HolidayDates ?? request.HolidayDate ?? [];
        var holidayFor = NormalizeHolidayFor(request.HolidayFor);
        if (holidayFor == "branch" && request.Branch is null or 0) throw BadRequest("Branch is required.");
        if (holidayFor == "division" && request.DivisionId is null or 0) throw BadRequest("Division is required.");
        if (dates.Length == 0 || names.Length == 0) throw BadRequest("Holiday name and date are required.");

        var now = DateTime.UtcNow;
        var holiday = new Holiday
        {
            Active = request.Active,
            HolidayFor = holidayFor,
            Branch = holidayFor == "branch" ? request.Branch : null,
            DivisionId = holidayFor == "division" ? request.DivisionId : null,
            Name = string.Join(",", names.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())),
            HolidayDate = string.Join(",", dates.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => DateOnly.Parse(x, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"))),
            CreatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddHolidayAsync(holiday, cancellationToken);
        return LaravelApiResponse.Success("holiday", await _repository.GetHolidayAsync(holiday.Id, cancellationToken), "Holiday added successfully");
    }

    public async Task<LaravelApiResponse> UpdateHolidayAsync(ulong id, HolidayRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var holiday = await _repository.GetHolidayEntityAsync(id, cancellationToken) ?? throw NotFound("Holiday not found");
        var names = request.Names ?? request.Name ?? [];
        var dates = request.HolidayDates ?? request.HolidayDate ?? [];
        var holidayFor = NormalizeHolidayFor(request.HolidayFor);
        if (holidayFor == "branch" && request.Branch is null or 0) throw BadRequest("Branch is required.");
        if (holidayFor == "division" && request.DivisionId is null or 0) throw BadRequest("Division is required.");
        if (dates.Length == 0 || names.Length == 0) throw BadRequest("Holiday name and date are required.");

        holiday.Active = request.Active;
        holiday.HolidayFor = holidayFor;
        holiday.Branch = holidayFor == "branch" ? request.Branch : null;
        holiday.DivisionId = holidayFor == "division" ? request.DivisionId : null;
        holiday.Name = string.Join(",", names.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        holiday.HolidayDate = string.Join(",", dates.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => DateOnly.Parse(x, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd")));
        holiday.UpdatedBy = actorUserId;
        holiday.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("holiday", await _repository.GetHolidayAsync(id, cancellationToken), "Holiday updated successfully");
    }

    public async Task<LaravelApiResponse> DeleteHolidayAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var holiday = await _repository.GetHolidayEntityAsync(id, cancellationToken) ?? throw NotFound("Holiday not found");
        await _repository.DeleteHolidayAsync(holiday, actorUserId, cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Holiday deleted successfully!");
    }

    public async Task<HrFileDto> ExportHolidaysAsync(HolidayListFilterDto filter, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetHolidaysAsync(filter, cancellationToken);
        return Workbook("holidays.xlsx", ["id", "holiday_for", "branch", "division", "holiday_names", "holiday_dates", "active", "created_by", "created_at"],
            rows.Select(x => new object?[] { x.Id, x.HolidayFor, x.BranchName, x.DivisionName, x.Name, x.HolidayDate, x.Active, x.CreatedBy, x.CreatedAt }));
    }

    public async Task<LaravelApiResponse> GetLeavesAsync(LeaveListFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("leaves", await _repository.GetLeavesAsync(filter, cancellationToken));

    public async Task<LaravelApiResponse> CreateLeaveAsync(LeaveRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var userId = RequireId(request.UserId, "User is required.");
        var from = request.FromDate?.Date ?? throw BadRequest("From date is required.");
        var to = request.ToDate?.Date ?? throw BadRequest("To date is required.");
        if (from > to) throw BadRequest("From date must be before or equal to to date.");
        var type = RequireIn(request.Type, LeaveTypes, "Leave type is required.");
        var balanceType = RequireIn(request.BalType, BalanceTypes, "Balance type is required.");
        var user = await _repository.GetUserAsync(userId, cancellationToken) ?? throw NotFound("User not found");
        var days = (decimal)(to - from).TotalDays + 1m;
        var amount = type is "First Half Leave" or "Second Half Leave" ? days * 0.5m : days;

        await EnsureLeaveBalanceAsync(user, balanceType, amount, cancellationToken);

        var now = DateTime.UtcNow;
        var leave = new Leave
        {
            UserId = userId,
            FromDate = from,
            ToDate = to,
            Type = type,
            BalType = balanceType,
            Reason = request.Reason?.Trim() ?? string.Empty,
            CreatedBy = actorUserId,
            Status = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddLeaveAsync(leave, cancellationToken);

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var attendance = await _repository.GetAttendanceByUserDateAsync(userId, date, cancellationToken);
            if (attendance is null)
            {
                await _repository.AddAttendanceAsync(new Attendance
                {
                    UserId = userId,
                    Active = "Y",
                    PunchinDate = date,
                    PunchinTime = new TimeSpan(10, 0, 0),
                    PunchinSummary = leave.Reason,
                    WorkingType = type,
                    PunchinFrom = "App",
                    CreatedAt = now,
                    UpdatedAt = now
                }, cancellationToken);
            }
            else
            {
                attendance.PunchinTime = new TimeSpan(10, 0, 0);
                attendance.PunchinSummary = leave.Reason;
                attendance.WorkingType = type;
                attendance.PunchinFrom = "App";
                attendance.UpdatedAt = now;
            }
        }

        await DeductLeaveBalanceAsync(user, leave, balanceType, amount, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("leave", await _repository.GetLeaveAsync(leave.Id, cancellationToken), "Leave added successfully.");
    }

    public async Task<LaravelApiResponse> DeleteLeaveAsync(ulong id, CancellationToken cancellationToken)
    {
        var leave = await _repository.GetLeaveEntityAsync(id, cancellationToken) ?? throw NotFound("Leave not found");
        var user = leave.UserId.HasValue ? await _repository.GetUserAsync(leave.UserId.Value, cancellationToken) : null;
        var amount = LeaveAmount(leave.FromDate, leave.ToDate, leave.Type);

        for (var date = leave.FromDate.Date; date <= leave.ToDate.Date; date = date.AddDays(1))
        {
            var attendance = leave.UserId.HasValue ? await _repository.GetAttendanceByUserDateAsync(leave.UserId.Value, date, cancellationToken) : null;
            if (attendance is not null) attendance.DeletedAt = DateTime.UtcNow;
        }

        RefundLeaveBalance(user, leave.BalType, amount);
        await _repository.DeleteLeaveAsync(leave, cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Leave deleted successfully and balance refunded!");
    }

    public async Task<LaravelApiResponse> ApproveLeaveAsync(ulong id, CancellationToken cancellationToken)
    {
        var leave = await _repository.GetLeaveEntityAsync(id, cancellationToken) ?? throw NotFound("Leave not found");
        leave.Status = 1;
        leave.RemarkStatus = null;
        leave.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Leave Approved Successfully");
    }

    public async Task<LaravelApiResponse> RejectLeaveAsync(ulong id, LeaveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var leave = await _repository.GetLeaveEntityAsync(id, cancellationToken) ?? throw NotFound("Leave not found");
        leave.Status = 2;
        leave.RemarkStatus = request.RemarkStatus;
        leave.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Leave Rejected Successfully");
    }

    public async Task<LaravelApiResponse> CreateCompOffAsync(CompOffRequestDto request, CancellationToken cancellationToken)
    {
        var userId = RequireId(request.UserId, "User is required.");
        var date = request.ComboOffDate?.Date ?? throw BadRequest("Comp-off date is required.");
        if (date.DayOfWeek != DayOfWeek.Sunday) throw BadRequest("Combo off leave apply only on sunday.");
        await _repository.AddCompOffAsync(new CompOffLeave
        {
            UserId = (long)userId,
            CompOffDate = date,
            ExpiryDate = date.AddDays(60),
            IsUsed = false,
            Balance = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "A comp-off date added for this user.");
    }

    public async Task<HrFileDto> ExportLeavesAsync(LeaveListFilterDto filter, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetLeavesAsync(filter, cancellationToken);
        return Workbook("leaves.xlsx", ["id", "employee_code", "user_name", "from_date", "to_date", "type", "balance_type", "reason", "status", "created_by", "created_at"],
            rows.Select(x => new object?[] { x.Id, x.EmployeeCode, x.UserName, x.FromDate, x.ToDate, x.Type, x.BalType, x.Reason, x.StatusLabel, x.CreatedBy, x.CreatedAt }));
    }

    public async Task<LaravelApiResponse> GetToursAsync(TourListFilterDto filter, ulong? actorUserId, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("tours", await _repository.GetToursAsync(filter, await _repository.GetVisibleUserIdsAsync(actorUserId, cancellationToken), cancellationToken));

    public async Task<LaravelApiResponse> GetTourAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("tour", await _repository.GetTourAsync(id, cancellationToken) ?? throw NotFound("Tour not found"));

    public async Task<LaravelApiResponse> CreateTourAsync(TourRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        await EnsureVisibleTourUserAsync(request.UserId, actorUserId, cancellationToken);
        var tour = new TourProgramme();
        ApplyTour(tour, request, actorUserId, true);
        await _repository.AddTourAsync(tour, cancellationToken);
        await TryTourDetailAsync(tour, cancellationToken);
        await SafeTourLogAsync(tour.Id, "created", "pending", "Tour created", actorUserId, cancellationToken);
        return LaravelApiResponse.Success("tour", await _repository.GetTourAsync(tour.Id, cancellationToken), "Tour programme created successfully.");
    }

    public async Task<LaravelApiResponse> UpdateTourAsync(ulong id, TourRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        await EnsureVisibleTourUserAsync(request.UserId, actorUserId, cancellationToken);
        var tour = await _repository.GetTourEntityAsync(id, cancellationToken) ?? throw NotFound("Tour not found");
        ApplyTour(tour, request, actorUserId, false);
        await _repository.SaveChangesAsync(cancellationToken);
        await TryTourDetailAsync(tour, cancellationToken);
        await SafeTourLogAsync(tour.Id, "updated", "pending", "Tour updated", actorUserId, cancellationToken);
        return LaravelApiResponse.Success("tour", await _repository.GetTourAsync(tour.Id, cancellationToken), "Tour updated successfully");
    }

    public async Task<LaravelApiResponse> DeleteTourAsync(ulong id, CancellationToken cancellationToken)
    {
        var tour = await _repository.GetTourEntityAsync(id, cancellationToken) ?? throw NotFound("Tour not found");
        await _repository.DeleteTourAsync(tour, cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "TourProgramme deleted successfully!");
    }

    public async Task<LaravelApiResponse> ChangeTourStatusAsync(TourStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var ids = request.Id?.Length > 0 ? request.Id : throw BadRequest("Tour id is required.");
        var status = int.TryParse(request.Status, out var parsedStatus) ? parsedStatus : 0;
        foreach (var id in ids)
        {
            var tour = await _repository.GetTourEntityAsync(id, cancellationToken);
            if (tour is null) continue;
            var oldStatus = tour.Status;
            tour.Status = status;
            tour.UpdatedAt = DateTime.UtcNow;
            await SafeTourLogAsync(id, StatusAction(status), status.ToString(), $"Status changed from {TourStatusLabel(oldStatus)} to {TourStatusLabel(status)}", actorUserId, cancellationToken);
        }
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Tour status changed successfully");
    }

    public async Task<HrFileDto> ExportToursAsync(TourListFilterDto filter, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetToursAsync(filter, await _repository.GetVisibleUserIdsAsync(actorUserId, cancellationToken), cancellationToken);
        return Workbook("tours.xlsx", ["id", "date", "employee_code", "user_name", "district", "city", "objectives", "type", "status", "created_at"],
            rows.Select(x => new object?[] { x.Id, x.Date, x.EmployeeCode, x.UserName, x.DistrictName, x.TownName, x.Objectives, x.Type, x.StatusLabel, x.CreatedAt }));
    }

    public Task<HrFileDto> GetTourTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Workbook("tours-template.xlsx", ["date", "user_id", "district", "city", "objectives"], []));

    public async Task<LaravelApiResponse> UploadToursAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheets.First();
        var header = sheet.FirstRowUsed() ?? throw BadRequest("Import file is empty.");
        var headings = header.CellsUsed().ToDictionary(x => Normalize(x.GetString()), x => x.Address.ColumnNumber);
        var imported = 0;
        foreach (var row in sheet.RowsUsed().Where(x => x.RowNumber() > header.RowNumber()))
        {
            var request = new TourRequestDto
            {
                Date = DateTime.TryParse(Cell(row, headings, "date"), out var date) ? date : null,
                UserId = ulong.TryParse(Cell(row, headings, "user_id"), out var userId) ? userId : null,
                District = Cell(row, headings, "district"),
                Town = Cell(row, headings, "city"),
                Objectives = Cell(row, headings, "objectives")
            };
            await CreateTourAsync(request, actorUserId, cancellationToken);
            imported++;
        }
        return LaravelApiResponse.Success("import", new { imported_rows = imported }, "Tour import completed");
    }

    public async Task<LaravelApiResponse> GetAttendancesAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("attendances", await _repository.GetAttendancesAsync(filter, cancellationToken));

    public async Task<LaravelApiResponse> GetAttendancePlanAsync(AttendancePlanLookupDto request, CancellationToken cancellationToken)
    {
        var userId = RequireId(request.UserId, "User is required.");
        var date = request.Date?.Date ?? throw BadRequest("Date is required.");
        return LaravelApiResponse.Success("plan", await _repository.GetAttendancePlanAsync(userId, date, cancellationToken));
    }

    public async Task<LaravelApiResponse> PunchInAsync(AttendancePunchInRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var userId = RequireId(request.UserId, "User is required.");
        var date = request.PunchinDate?.Date ?? DateTime.Today;
        var time = ParseTime(request.PunchinTime) ?? DateTime.Now.TimeOfDay;
        var tourId = RequireId(request.TourId, "Tour plan is required for punch in.");
        var tour = await _repository.GetTourEntityAsync(tourId, cancellationToken) ?? throw BadRequest("Tour plan is required for punch in.");
        if (tour.UserId != userId || tour.Date?.Date != date)
        {
            throw BadRequest("Selected tour plan does not match punch in user and date.");
        }

        var attendance = await _repository.GetAttendanceByUserDateAsync(userId, date, cancellationToken);
        var now = DateTime.UtcNow;
        var isNewAttendance = attendance is null;

        attendance ??= new Attendance { UserId = userId, PunchinDate = date, CreatedAt = now };

        attendance.Active = "Y";
        attendance.PunchinTime = time;
        attendance.PunchinAddress ??= string.Empty;
        attendance.PunchinImage ??= string.Empty;
        attendance.PunchoutAddress ??= string.Empty;
        attendance.PunchoutImage ??= string.Empty;
        attendance.PunchoutSummary ??= string.Empty;
        attendance.WorkedTime ??= string.Empty;
        attendance.AttendanceStatus ??= 0;
        attendance.PunchinSummary = request.PunchinSummary?.Trim() ?? string.Empty;
        attendance.WorkingType = request.WorkingType?.Trim() ?? string.Empty;
        attendance.PunchinFrom = "Web";
        attendance.Flag = "true";
        attendance.TourId = tourId.ToString(CultureInfo.InvariantCulture);
        attendance.City = request.City ?? tour.Town;
        attendance.UpdatedAt = now;
        tour.Type = attendance.WorkingType;
        tour.UpdatedAt = now;

        await EarnCompOffIfNeededAsync(userId, date, cancellationToken);
        if (isNewAttendance)
        {
            await _repository.AddAttendanceAsync(attendance, cancellationToken);
        }
        else
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return LaravelApiResponse.Success("attendance", (await _repository.GetAttendancesAsync(new AttendanceListFilterDto { ExecutiveId = userId, StartDate = date, EndDate = date }, cancellationToken)).FirstOrDefault(), "PunchIn Successfully");
    }

    public async Task<LaravelApiResponse> PunchOutAsync(AttendancePunchOutRequestDto request, CancellationToken cancellationToken)
    {
        var id = RequireId(request.Id, "Attendance id is required.");
        var attendance = await _repository.GetAttendanceEntityAsync(id, cancellationToken) ?? throw NotFound("Attendance not found");
        attendance.PunchoutDate = DateTime.Today;
        attendance.PunchoutTime = DateTime.Now.TimeOfDay;
        attendance.PunchoutSummary = request.PunchoutSummary?.Trim() ?? string.Empty;
        attendance.WorkedTime = FormatWorkedTime(attendance.PunchinDate, attendance.PunchinTime, attendance.PunchoutDate.Value, attendance.PunchoutTime.Value);
        attendance.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("punchout", attendance, "Punch Out successfully");
    }

    public async Task<LaravelApiResponse> RemovePunchOutAsync(ulong id, CancellationToken cancellationToken)
    {
        var attendance = await _repository.GetAttendanceEntityAsync(id, cancellationToken) ?? throw NotFound("Attendance not found");
        attendance.PunchoutDate = null;
        attendance.PunchoutTime = null;
        attendance.PunchoutAddress = string.Empty;
        attendance.PunchoutImage = string.Empty;
        attendance.PunchoutSummary = string.Empty;
        attendance.WorkedTime = string.Empty;
        attendance.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Punchout Remeoved Successfully");
    }

    public async Task<LaravelApiResponse> ApproveAttendanceAsync(ulong[] ids, ulong? actorUserId, CancellationToken cancellationToken)
    {
        foreach (var id in ids)
        {
            var attendance = await _repository.GetAttendanceEntityAsync(id, cancellationToken);
            if (attendance is null) continue;
            attendance.AttendanceStatus = 1;
            attendance.ApproveRejectBy = actorUserId?.ToString(CultureInfo.InvariantCulture);
            attendance.RemarkStatus = null;
            attendance.UpdatedAt = DateTime.UtcNow;
        }
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Attendance Approved Successfully");
    }

    public async Task<LaravelApiResponse> RejectAttendanceAsync(AttendanceRejectRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var ids = request.Id?.Length > 0 ? request.Id : throw BadRequest("Attendance id is required.");
        foreach (var id in ids)
        {
            var attendance = await _repository.GetAttendanceEntityAsync(id, cancellationToken);
            if (attendance is null) continue;
            attendance.AttendanceStatus = 2;
            attendance.ApproveRejectBy = actorUserId?.ToString(CultureInfo.InvariantCulture);
            attendance.RemarkStatus = request.RemarkStatus;
            attendance.UpdatedAt = DateTime.UtcNow;
        }
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Attendance Rejected Successfully");
    }

    public async Task<HrFileDto> ExportAttendancesAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetAttendancesAsync(filter, cancellationToken);
        return Workbook("attendancereports.xlsx", ["id", "employee_code", "user_name", "punchin_date", "punchin_time", "punchout_time", "worked_time", "status", "objective", "attendance_status", "remark_status", "punchin_address", "punchout_address", "punchin_from"],
            rows.Select(x => new object?[] { x.Id, x.EmployeeCode, x.UserName, x.PunchinDate, x.PunchinTime, x.PunchoutTime ?? "misspunch", x.WorkedTime, x.AttendanceLabel, x.WorkingType, x.AttendanceStatusLabel, x.RemarkStatus, x.PunchinAddress, x.PunchoutAddress, x.PunchinFrom }));
    }

    public async Task<LaravelApiResponse> GetAttendanceSummaryAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("summary", await BuildSummaryAsync(filter, cancellationToken));

    public async Task<HrFileDto> ExportAttendanceSummaryAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken)
    {
        var rows = await BuildSummaryAsync(filter, cancellationToken);
        var start = filter.StartDate?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = filter.EndDate?.Date ?? start.AddMonths(1).AddDays(-1);
        var dayHeadings = EachDate(start, end).Select(x => x.ToString("d-MMM-yyyy", CultureInfo.InvariantCulture)).ToArray();
        var headings = new[] { "user_id", "employee_code", "user_name", "reporting_managers" }
            .Concat(dayHeadings)
            .Concat(["week_off", "absent", "half_day", "holiday", "present", "total_days"])
            .ToArray();
        return Workbook("attendance-summary.xlsx", headings, rows.Select(row =>
            new object?[] { row.UserId, row.EmployeeCode, row.UserName, row.ReportingManagers }
                .Concat(dayHeadings.Select(day => row.Days.TryGetValue(day, out var value) ? value : "-"))
                .Concat([row.WeekOff, row.Absent, row.HalfDay, row.Holiday, row.Present, row.TotalDays])
                .ToArray()));
    }

    private async Task<IReadOnlyList<AttendanceSummaryDto>> BuildSummaryAsync(AttendanceListFilterDto filter, CancellationToken cancellationToken)
    {
        var start = filter.StartDate?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = filter.EndDate?.Date ?? start.AddMonths(1).AddDays(-1);
        var users = await _repository.GetReportUsersAsync(filter.ExecutiveId, filter.DesignationId, cancellationToken);
        var attendances = (await _repository.GetAttendanceEntitiesInRangeAsync(start, end, cancellationToken))
            .GroupBy(x => (x.UserId, Date: x.PunchinDate.Date))
            .ToDictionary(x => x.Key, x => x.First());
        var holidays = await _repository.GetActiveHolidaysAsync(cancellationToken);
        var holidayByBranch = holidays
            .Where(x => x.HolidayFor == "branch" && x.Branch.HasValue)
            .GroupBy(x => x.Branch!.Value)
            .ToDictionary(x => x.Key, x => x.SelectMany(h => SplitCsv(h.HolidayDate)).ToHashSet(StringComparer.OrdinalIgnoreCase));
        var holidayByDivision = holidays
            .Where(x => x.HolidayFor == "division" && x.DivisionId.HasValue)
            .GroupBy(x => x.DivisionId!.Value)
            .ToDictionary(x => x.Key, x => x.SelectMany(h => SplitCsv(h.HolidayDate)).ToHashSet(StringComparer.OrdinalIgnoreCase));

        return users.Select(user =>
        {
            var days = new Dictionary<string, string>();
            var weekOff = 0;
            var absent = 0;
            var halfDay = 0;
            var holiday = 0;
            var present = 0;
            foreach (var date in EachDate(start, end))
            {
                var key = date.ToString("d-MMM-yyyy", CultureInfo.InvariantCulture);
                var label = SummaryLabel(user, date, attendances.TryGetValue((user.Id, date), out var att) ? att : null, holidayByBranch, holidayByDivision);
                days[key] = label;
                if (label == "W/o") weekOff++;
                else if (label == "A" || label == "MIS") absent++;
                else if (label.StartsWith("1/2", StringComparison.OrdinalIgnoreCase)) halfDay++;
                else if (label == "H") holiday++;
                else if (label == "P" || label == "PW") present++;
            }

            return new AttendanceSummaryDto
            {
                UserId = user.Id,
                EmployeeCode = user.EmployeeCodes,
                UserName = user.Name,
                ReportingManagers = user.ReportingId?.ToString() ?? "-",
                Days = days,
                WeekOff = weekOff,
                Absent = absent,
                HalfDay = halfDay,
                Holiday = holiday,
                Present = present,
                TotalDays = days.Count
            };
        }).ToList();
    }

    private static string SummaryLabel(User user, DateTime date, Attendance? attendance, IReadOnlyDictionary<ulong, HashSet<string>> holidayByBranch, IReadOnlyDictionary<ulong, HashSet<string>> holidayByDivision)
    {
        var holidayDate = date.ToString("yyyy-MM-dd");
        var branchId = user.PrimaryBranchId ?? ParseFirstUlong(user.BranchId);
        if (branchId.HasValue && holidayByBranch.TryGetValue(branchId.Value, out var holidayDates) && holidayDates.Contains(holidayDate)) return "H";
        if (user.DivisionId.HasValue && holidayByDivision.TryGetValue(user.DivisionId.Value, out var divisionHolidayDates) && divisionHolidayDates.Contains(holidayDate)) return "H";
        if (attendance is null)
        {
            if (user.DateOfJoining.HasValue && user.DateOfJoining.Value.Date > date.Date) return "-";
            return date.DayOfWeek == DayOfWeek.Sunday ? "W/o" : "MIS";
        }
        if (attendance.AttendanceStatus == 1)
        {
            if (attendance.WorkingType is "Leave" or "Full Day Leave") return "AL";
            if (attendance.WorkingType is "First Half Leave" or "Second Half Leave") return "1/2P+1/2AL";
            return date.DayOfWeek == DayOfWeek.Sunday ? "PW" : "P";
        }
        if (attendance.AttendanceStatus == 2) return attendance.WorkingType is "First Half Leave" or "Second Half Leave" ? "1/2P+1/2LOPN" : "A";
        return attendance.WorkingType is "First Half Leave" or "Second Half Leave" ? "1/2P+1/2LOPN" : "PN";
    }

    private async Task EnsureLeaveBalanceAsync(User user, string balanceType, decimal amount, CancellationToken cancellationToken)
    {
        if (balanceType == "Comp-off Balance")
        {
            var compOffs = await _repository.GetAvailableCompOffsAsync(user.Id, cancellationToken);
            if (compOffs.Sum(x => x.Balance) < amount) throw BadRequest($"Insufficient {balanceType} balance.");
            return;
        }

        var balance = balanceType switch
        {
            "Earned Leave" => user.EarnedLeaveBalance ?? 0,
            "Sick Leave" => user.SickLeaveBalance ?? 0,
            "Compoff Balance" => user.CompbOff ?? 0,
            _ => user.CasualLeaveBalance ?? 0
        };
        if (balance < amount && balanceType != "Casual Leave") throw BadRequest($"Insufficient {balanceType} balance.");
    }

    private async Task DeductLeaveBalanceAsync(User user, Leave leave, string balanceType, decimal amount, CancellationToken cancellationToken)
    {
        if (balanceType == "Comp-off Balance")
        {
            var remaining = amount;
            var compOffs = await _repository.GetAvailableCompOffsAsync(user.Id, cancellationToken);
            foreach (var comp in compOffs)
            {
                if (remaining <= 0) break;
                var use = Math.Min(remaining, comp.Balance);
                comp.Balance -= use;
                remaining -= use;
                comp.LeaveId = string.Join(",", SplitCsv(comp.LeaveId).Append(leave.Id.ToString()).Distinct());
                comp.IsUsed = comp.Balance <= 0;
            }
            return;
        }

        switch (balanceType)
        {
            case "Earned Leave": user.EarnedLeaveBalance = Math.Max(0, (user.EarnedLeaveBalance ?? 0) - amount); break;
            case "Sick Leave": user.SickLeaveBalance = Math.Max(0, (user.SickLeaveBalance ?? 0) - amount); break;
            case "Compoff Balance": user.CompbOff = Math.Max(0, (user.CompbOff ?? 0) - amount); break;
            default: user.CasualLeaveBalance = Math.Max(0, (user.CasualLeaveBalance ?? 0) - amount); break;
        }
    }

    private static void RefundLeaveBalance(User? user, string? balanceType, decimal amount)
    {
        if (user is null) return;
        switch (balanceType)
        {
            case "Earned Leave": user.EarnedLeaveBalance = (user.EarnedLeaveBalance ?? 0) + amount; break;
            case "Sick Leave": user.SickLeaveBalance = (user.SickLeaveBalance ?? 0) + amount; break;
            case "Compoff Balance": user.CompbOff = (user.CompbOff ?? 0) + amount; break;
            case "Casual Leave": user.CasualLeaveBalance = (user.CasualLeaveBalance ?? 0) + amount; break;
        }
    }

    private async Task EarnCompOffIfNeededAsync(ulong userId, DateTime date, CancellationToken cancellationToken)
    {
        if (date.DayOfWeek != DayOfWeek.Sunday) return;
        await _repository.AddCompOffAsync(new CompOffLeave
        {
            UserId = (long)userId,
            CompOffDate = date,
            ExpiryDate = date.AddDays(60),
            IsUsed = false,
            Balance = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private void ApplyTour(TourProgramme tour, TourRequestDto request, ulong? actorUserId, bool creating)
    {
        var userId = RequireId(request.UserId, "User is required.");
        var date = request.Date?.Date ?? throw BadRequest("Date is required.");
        if (!ulong.TryParse(request.Town, out var cityId) || cityId == 0) throw BadRequest("City is required.");
        tour.Date = date;
        tour.UserId = userId;
        tour.Town = cityId.ToString(CultureInfo.InvariantCulture);
        tour.District = long.TryParse(request.District, out var districtId) && districtId > 0 ? districtId : null;
        tour.Objectives = request.Objectives?.Trim() ?? string.Empty;
        tour.UpdatedAt = DateTime.UtcNow;
        if (creating)
        {
            tour.Status = 0;
            tour.CreatedBy = actorUserId;
            tour.CreatedAt = DateTime.UtcNow;
        }
    }

    private async Task EnsureVisibleTourUserAsync(ulong? userId, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var selectedUserId = RequireId(userId, "User is required.");
        var visibleUserIds = await _repository.GetVisibleUserIdsAsync(actorUserId, cancellationToken);
        if (!visibleUserIds.Contains(selectedUserId))
        {
            throw BadRequest("Selected user is not in your reporting users.");
        }
    }

    private async Task TryTourDetailAsync(TourProgramme tour, CancellationToken cancellationToken)
    {
        if (ulong.TryParse(tour.Town, out var cityId) && cityId > 0)
        {
            await _repository.UpsertTourDetailAsync(tour.Id, cityId, cancellationToken);
        }
    }

    private async Task SafeTourLogAsync(ulong tourId, string action, string status, string remark, ulong? actorUserId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private static HrFileDto Workbook(string fileName, string[] headings, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet1");
        for (var i = 0; i < headings.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = Title(headings[i]);
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }
        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++) sheet.Cell(rowNumber, i + 1).Value = XLCellValue.FromObject(row[i]);
            rowNumber++;
        }
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new HrFileDto { FileName = fileName, Content = stream.ToArray() };
    }

    private static string[] WorkingTypes() =>
        ["Local Market Visit", "Office Work", "Office Meeting", "Scouting for market", "Plumber Meet", "Retailer Meet", "Service Center Visit", "Tour", "Holiday"];

    private static IEnumerable<DateTime> EachDate(DateTime start, DateTime end)
    {
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1)) yield return date;
    }

    private static decimal LeaveAmount(DateTime from, DateTime to, string type) =>
        ((decimal)(to.Date - from.Date).TotalDays + 1m) * (type is "First Half Leave" or "Second Half Leave" ? 0.5m : 1m);

    private static string FormatWorkedTime(DateTime inDate, TimeSpan inTime, DateTime outDate, TimeSpan outTime)
    {
        var span = outDate.Date.Add(outTime) - inDate.Date.Add(inTime);
        if (span < TimeSpan.Zero) span = TimeSpan.Zero;
        return span.ToString(@"hh\:mm\:ss");
    }

    private static TimeSpan? ParseTime(string? value) =>
        TimeSpan.TryParse(value, out var time) ? time : null;

    private static ulong RequireId(ulong? value, string message) =>
        value is null or 0 ? throw BadRequest(message) : value.Value;

    private static string RequireIn(string? value, string[] allowed, string message)
    {
        if (string.IsNullOrWhiteSpace(value) || !allowed.Contains(value)) throw BadRequest(message);
        return value;
    }

    private static string NormalizeHolidayFor(string? value) =>
        string.Equals(value, "division", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "zone", StringComparison.OrdinalIgnoreCase)
            ? "division"
            : "branch";

    private static LaravelHttpException BadRequest(string message) => new(LaravelStatusCodes.BadRequest, message);
    private static LaravelHttpException NotFound(string message) => new(LaravelStatusCodes.NotFound, message);
    private static string[] SplitCsv(string? value) => string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static ulong? ParseFirstUlong(string? value) => SplitCsv(value).Select(x => ulong.TryParse(x, out var id) ? id : (ulong?)null).FirstOrDefault(x => x.HasValue);
    private static string Title(string value) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace("_", " "));
    private static string TourStatusLabel(int status) => status == 1 ? "Approved" : status == 2 ? "Rejected" : "Pending";
    private static string StatusAction(int status) => status == 1 ? "approved" : status == 2 ? "rejected" : "pending";
    private static string Normalize(string value) => value.Trim().ToLowerInvariant().Replace(" ", "_");
    private static string? Cell(IXLRow row, IReadOnlyDictionary<string, int> headings, string heading) =>
        headings.TryGetValue(Normalize(heading), out var column) ? row.Cell(column).GetFormattedString().Trim() : null;
}
