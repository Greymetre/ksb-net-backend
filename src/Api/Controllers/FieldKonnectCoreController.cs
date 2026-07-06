using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class FieldKonnectCoreController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public FieldKonnectCoreController(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    [AcceptVerbs("GET", "POST")]
    [Route("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DashboardQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var userId = CurrentUserId();
            var (fromDate, toDate) = ResolveDashboardDates(query);
            var today = IndiaNow().Date;

            var punchin = await _dbContext.Attendances
                .Where(x => x.UserId == userId && x.PunchinDate.Date == today)
                .Select(x => new { x.Id, x.PunchinTime, x.PunchoutTime, x.WorkingType, x.Flag })
                .FirstOrDefaultAsync(cancellationToken);

            var toExclusive = toDate.AddDays(1);
            var orders = await _dbContext.Orders
                .Where(x => x.CreatedBy == userId && x.OrderDate >= fromDate && x.OrderDate < toExclusive)
                .Select(x => new { x.Id, x.BuyerId, x.BeatScheduleId, x.GrandTotal })
                .ToListAsync(cancellationToken);

            var achievement = orders.Sum(x => x.GrandTotal);
            var target = await QueryScalarDecimal(
                "SELECT COALESCE(SUM(amount), 0) FROM sales_targets WHERE userid = @user_id AND YEAR(startdate) = @year AND MONTH(startdate) = @month",
                cancellationToken,
                ("@user_id", userId),
                ("@year", fromDate.Year),
                ("@month", fromDate.Month));

            var salesRows = await QueryRows(
                "SELECT id, grand_total FROM sales WHERE created_by = @user_id AND DATE(invoice_date) >= @from_date AND DATE(invoice_date) <= @to_date",
                cancellationToken,
                ("@user_id", userId),
                ("@from_date", fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("@to_date", toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

            var salesIds = salesRows.Select(x => Convert.ToUInt64(x["id"])).ToArray();
            var salesAmount = salesRows.Sum(x => ToDecimal(x["grand_total"]));
            var collectionAmount = salesIds.Length == 0
                ? 0m
                : await QueryScalarDecimal($"SELECT COALESCE(SUM(amount), 0) FROM payment_details WHERE sales_id IN ({string.Join(',', salesIds)})", cancellationToken);

            var beatSchedules = await _dbContext.BeatSchedules
                .Where(x => x.UserId == userId && x.BeatDate >= fromDate && x.BeatDate <= toDate)
                .Select(x => new { x.Id, x.BeatId })
                .ToListAsync(cancellationToken);

            var totalBeatCounter = 0L;
            var totalVisitedCounter = 0L;
            foreach (var schedule in beatSchedules)
            {
                if (schedule.BeatId.HasValue)
                {
                    totalBeatCounter += await QueryScalarLong("SELECT COUNT(*) FROM beat_customers WHERE beat_id = @beat_id", cancellationToken, ("@beat_id", schedule.BeatId.Value));
                }

                totalVisitedCounter += await QueryScalarLong(
                    "SELECT COUNT(DISTINCT CONCAT(COALESCE(customer_id, 0), '#', checkin_date)) FROM check_in WHERE beatscheduleid = @schedule_id AND deleted_at IS NULL",
                    cancellationToken,
                    ("@schedule_id", schedule.Id));
            }

            var assignCounter = await _dbContext.Customers.CountAsync(x => x.ExecutiveId == userId, cancellationToken);
            var newAddedCounter = await _dbContext.Customers.CountAsync(x => x.CreatedBy == userId && x.CreatedAt >= fromDate && x.CreatedAt < toExclusive, cancellationToken);
            var activeCounter = orders.Select(x => x.BuyerId).Where(x => x.HasValue).Distinct().Count();

            var workings = await _dbContext.Attendances
                .Where(x => x.UserId == userId && x.WorkingType == "fields" && x.PunchinDate >= fromDate && x.PunchinDate <= toDate)
                .Select(x => x.WorkedTime)
                .ToListAsync(cancellationToken);
            var workingDays = workings.Sum(WorkingDayValue);
            var avgSales = achievement > 0 && workingDays > 0 ? achievement / workingDays : 0m;

            var productiveOrders = orders
                .Where(x => x.BuyerId.HasValue)
                .Select(x => $"{x.BuyerId}:{x.BeatScheduleId}")
                .Distinct()
                .Count();

            var data = new Dictionary<string, object?>
            {
                ["punchin_id"] = punchin?.Id.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ["punchin"] = punchin is not null,
                ["punchin_flag"] = !string.IsNullOrWhiteSpace(punchin?.Flag),
                ["punchout"] = punchin?.PunchoutTime is not null,
                ["working_type"] = punchin?.WorkingType ?? string.Empty,
                ["buyer"] = "Retailer",
                ["seller"] = "Distributor",
                ["totalcounter"] = totalBeatCounter,
                ["visitcounter"] = totalVisitedCounter,
                ["adherence"] = totalBeatCounter >= 1 ? Percent(totalVisitedCounter, totalBeatCounter) : string.Empty,
                ["productive_counter"] = activeCounter.ToString(CultureInfo.InvariantCulture),
                ["productivity"] = totalVisitedCounter >= 1 ? Percent(productiveOrders, totalVisitedCounter) : string.Empty,
                ["target_amount"] = AmountConversion(target),
                ["achievement_amount"] = AmountConversion(achievement),
                ["achievement_percent"] = target >= 1 ? (achievement * 100) / target : 0,
                ["target"] = target,
                ["achievement"] = achievement,
                ["orders_count"] = orders.Count,
                ["outstanding_amount"] = (salesAmount - collectionAmount).ToString(CultureInfo.InvariantCulture),
                ["new_added_counter"] = newAddedCounter,
                ["active_counter_percent"] = activeCounter >= 1 && assignCounter >= 1 ? Percent(activeCounter, assignCounter) : string.Empty,
                ["uniquesku_qty"] = 0,
                ["sales_amount"] = AmountConversion(salesAmount),
                ["average_sales"] = AmountConversion(avgSales),
                ["collection_amount"] = AmountConversion(collectionAmount)
            };

            return Ok(new { status = "success", message = "Data retrieved successfully.", data });
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getPunchin")]
    public async Task<IActionResult> GetPunchin(CancellationToken cancellationToken)
    {
        try
        {
            var userId = CurrentUserId();
            var today = IndiaNow().Date;
            var attendances = await _dbContext.Attendances
                .Where(x => x.UserId == userId && x.PunchinDate.Date == today)
                .OrderByDescending(x => x.PunchinDate)
                .ToListAsync(cancellationToken);

            var rows = attendances
                .Select(x => new
                {
                    punchin_id = x.Id,
                    punchin_date = DateOnly.FromDateTime(x.PunchinDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    punchin_time = FormatTime(x.PunchinTime),
                    punchin_longitude = x.PunchinLongitude ?? string.Empty,
                    punchin_latitude = x.PunchinLatitude ?? string.Empty,
                    punchin_address = x.PunchinAddress,
                    punchin_image = x.PunchinImage,
                    punchout_date = x.PunchoutDate.HasValue ? DateOnly.FromDateTime(x.PunchoutDate.Value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty,
                    punchout_time = x.PunchoutTime.HasValue ? FormatTime(x.PunchoutTime.Value) : string.Empty,
                    punchout_latitude = x.PunchoutLatitude ?? string.Empty,
                    punchout_longitude = x.PunchoutLongitude ?? string.Empty,
                    punchout_address = x.PunchoutAddress,
                    punchout_image = x.PunchoutImage,
                    punchin_flag = !string.IsNullOrWhiteSpace(x.Flag),
                    working_type = x.WorkingType ?? string.Empty
                })
                .ToList();

            if (rows.Count == 0)
            {
                return Ok(new { status = "error", message = "No Record Found.", data = rows });
            }

            return Ok(new { status = "success", message = "Data retrieved successfully.", data = rows });
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getTodaySchedul")]
    public async Task<IActionResult> GetTodaySchedul(CancellationToken cancellationToken)
    {
        try
        {
            var userId = CurrentUserId();
            var today = IndiaNow().Date;
            var rows = await QueryRows(@"SELECT bs.id, bs.active, bs.beat_id, bs.beat_date, bs.user_id, bs.created_at, bs.updated_at, bs.tourid,
b.id AS b_id, b.active AS b_active, b.beat_name, b.description, b.region_id, b.country_id, b.state_id, b.district_id, b.city_id, b.created_by AS b_created_by, b.created_at AS b_created_at, b.updated_at AS b_updated_at
FROM beat_schedules bs
LEFT JOIN beats b ON b.id = bs.beat_id
WHERE bs.user_id = @user_id AND bs.beat_date = @today
ORDER BY bs.id ASC", cancellationToken, ("@user_id", userId), ("@today", today));

            var data = rows.Select(row => new
            {
                id = Obj(row, "id"),
                active = Str(row, "active"),
                beat_id = Obj(row, "beat_id"),
                beat_date = DateSql(row, "beat_date"),
                user_id = Obj(row, "user_id"),
                created_at = Obj(row, "created_at"),
                updated_at = Obj(row, "updated_at"),
                tourid = Obj(row, "tourid"),
                beats = Obj(row, "b_id") is null ? null : new
                {
                    id = Obj(row, "b_id"),
                    active = Str(row, "b_active"),
                    beat_name = Str(row, "beat_name"),
                    description = Str(row, "description"),
                    region_id = Obj(row, "region_id"),
                    country_id = Obj(row, "country_id"),
                    state_id = Str(row, "state_id"),
                    district_id = Str(row, "district_id"),
                    city_id = Str(row, "city_id"),
                    created_by = Obj(row, "b_created_by"),
                    created_at = Obj(row, "b_created_at"),
                    updated_at = Obj(row, "b_updated_at")
                }
            }).ToList();

            return Ok(new { status = "success", message = "Data retrieved successfully.", data });
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = ExceptionMessage(exception) });
        }
    }

    [HttpPost("userPunchin")]
    public async Task<IActionResult> UserPunchin(CancellationToken cancellationToken)
    {
        try
        {
            var request = await ReadPunchInForm(cancellationToken);
            if (string.IsNullOrWhiteSpace(request.PunchinLatitude) ||
                string.IsNullOrWhiteSpace(request.PunchinLongitude) ||
                string.IsNullOrWhiteSpace(request.Type) ||
                string.IsNullOrWhiteSpace(request.City) ||
                string.IsNullOrWhiteSpace(request.PunchinSummary))
            {
                return BadRequest(new
                {
                    status = "error",
                    message = ValidationErrors(
                        ("punchin_latitude", request.PunchinLatitude),
                        ("punchin_longitude", request.PunchinLongitude),
                        ("type", request.Type),
                        ("city", request.City),
                        ("punchin_summary", request.PunchinSummary))
                });
            }

            var userId = CurrentUserId();
            var user = await _dbContext.Users.IgnoreQueryFilters().FirstAsync(x => x.Id == userId, cancellationToken);
            var now = IndiaNow();
            var today = now.Date;
            var imagePath = request.Image is { Length: > 0 } ? await SaveAttendanceFile(request.Image, "punchin", cancellationToken) : string.Empty;

            await AddCompOffIfNeeded(user, today, cancellationToken);

            var attendance = await _dbContext.Attendances.FirstOrDefaultAsync(x => x.UserId == userId && x.PunchinDate == today, cancellationToken);
            if (attendance is null)
            {
                attendance = new Attendance { UserId = userId, PunchinDate = today, CreatedAt = now };
                await _dbContext.Attendances.AddAsync(attendance, cancellationToken);
            }

            attendance.Active = "Y";
            attendance.Flag = "true";
            attendance.PunchinTime = now.TimeOfDay;
            attendance.TourId = string.IsNullOrWhiteSpace(request.TourId) ? null : request.TourId;
            attendance.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
            attendance.PunchinLongitude = request.PunchinLatitude ?? string.Empty;
            attendance.PunchinLatitude = request.PunchinLongitude ?? string.Empty;
            attendance.PunchinAddress = FirstNonEmpty(request.PunchinAddress, request.Address) ?? string.Empty;
            attendance.PunchinImage = imagePath;
            attendance.PunchinSummary = request.PunchinSummary ?? string.Empty;
            attendance.WorkingType = request.Type.Trim();
            attendance.AttendanceStatus ??= 0;
            attendance.PunchinFrom = "App";
            attendance.BeatId = request.Beats;
            attendance.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await AddBeatSchedules(userId, request.Beats, request.TourId, today, cancellationToken);
            await UpdateTourOnPunchIn(request, today, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var punchin = new
            {
                active = attendance.Active,
                user_id = attendance.UserId,
                punchin_date = DateOnly.FromDateTime(attendance.PunchinDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                punchin_time = FormatTime(attendance.PunchinTime),
                punchin_longitude = attendance.PunchinLongitude,
                punchin_latitude = attendance.PunchinLatitude,
                punchin_address = attendance.PunchinAddress,
                punchin_image = attendance.PunchinImage
            };

            return Ok(new { status = "success", message = "Punch In successfully", punchin_id = attendance.Id, punchin });
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = ExceptionMessage(exception) });
        }
    }

    [HttpPost("userPunchout")]
    public async Task<IActionResult> UserPunchout(CancellationToken cancellationToken)
    {
        try
        {
            var request = await ReadPunchOutForm(cancellationToken);
            if (request.PunchinId is null or 0 || string.IsNullOrWhiteSpace(request.PunchoutLatitude) || string.IsNullOrWhiteSpace(request.PunchoutLongitude))
            {
                return BadRequest(new { status = "error", message = ValidationErrors(("punchin_id", request.PunchinId?.ToString()), ("punchout_longitude", request.PunchoutLongitude), ("punchout_latitude", request.PunchoutLatitude)) });
            }

            var userId = CurrentUserId();
            var attendance = await _dbContext.Attendances.FirstOrDefaultAsync(x => x.Id == request.PunchinId && x.UserId == userId, cancellationToken);
            if (attendance is null)
            {
                return BadRequest(new { status = "error", message = new Dictionary<string, string[]> { ["punchin_id"] = ["The selected punchin id is invalid."] } });
            }

            var now = IndiaNow();
            var imagePath = request.Image is { Length: > 0 } ? await SaveAttendanceFile(request.Image, $"punchout_{request.PunchinId}", cancellationToken) : string.Empty;
            var punchoutTime = attendance.WorkingType == "Second Half Leave" ? new TimeSpan(14, 0, 0) : now.TimeOfDay;

            attendance.PunchoutDate = now.Date;
            attendance.PunchoutTime = punchoutTime;
            attendance.PunchoutLatitude = request.PunchoutLatitude;
            attendance.PunchoutLongitude = request.PunchoutLongitude;
            attendance.PunchoutAddress = FirstNonEmpty(request.PunchoutAddress, request.Address) ?? string.Empty;
            attendance.PunchoutImage = imagePath;
            attendance.PunchoutSummary = request.PunchoutSummary ?? string.Empty;
            attendance.WorkedTime = WorkedTime(now, attendance.PunchinDate, attendance.PunchinTime);
            attendance.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { status = "success", message = "Punch Out successfully", punchout = ToAttendanceObject(attendance) });
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getAllUserPunchInOut")]
    [Route("getAllAttendance")]
    public async Task<IActionResult> GetAllUserPunchInOut(CancellationToken cancellationToken)
    {
        try
        {
            var authUserId = CurrentUserId();
            var body = await RequestBody(cancellationToken);
            var pageSize = Math.Clamp((int)(ULongValue("pageSize", body) ?? ULongValue("per_page", body) ?? 100), 1, 500);
            var page = Math.Max(1, (int)(ULongValue("page", body) ?? 1));
            var offset = (page - 1) * pageSize;
            var searchName = RequestValue("search_name", body);
            var userIds = !string.IsNullOrWhiteSpace(searchName) && ulong.TryParse(searchName, out var selectedUserId)
                ? new List<ulong> { selectedUserId }
                : await VisibleUserIds(authUserId, cancellationToken);

            userIds = await FilterUserIds(userIds, body, cancellationToken);
            var users = await UserOptions(userIds, cancellationToken);
            var branches = await BranchOptions(await VisibleUserIds(authUserId, cancellationToken), cancellationToken);
            var where = new List<string> { userIds.Count == 0 ? "1 = 0" : $"a.user_id IN ({string.Join(',', userIds.Distinct())})", "a.deleted_at IS NULL" };
            var parameters = new List<(string, object?)>();

            var startDate = ParseDate(RequestValue("start_date", body));
            var endDate = ParseDate(RequestValue("end_date", body));
            if (startDate.HasValue)
            {
                if (!endDate.HasValue)
                {
                    return BadRequest(new { status = "error", message = new Dictionary<string, string[]> { ["end_date"] = ["The end date field is required when start date is present."] } });
                }

                where.Add("a.punchin_date BETWEEN @start_date AND @end_date");
                parameters.Add(("@start_date", startDate.Value));
                parameters.Add(("@end_date", endDate.Value));
            }

            var type = RequestValue("type", body);
            if (type == "leave")
            {
                where.Add("a.working_type IN ('Full Day Leave', 'First Half Leave', 'Second Half Leave')");
            }
            else if (type == "normal")
            {
                where.Add("(a.working_type NOT IN ('Full Day Leave', 'First Half Leave', 'Second Half Leave') OR a.working_type IS NULL)");
            }

            var status = RequestValue("status", body);
            if (!string.IsNullOrWhiteSpace(status))
            {
                where.Add("a.attendance_status = @status");
                parameters.Add(("@status", status));
            }

            var whereSql = string.Join(" AND ", where);
            var total = await QueryScalarLong($"SELECT COUNT(*) FROM attendances a WHERE {whereSql}", cancellationToken, parameters.ToArray());
            var rows = await QueryRows($@"SELECT a.id, a.user_id, a.punchin_date, a.punchin_time, a.punchout_time, a.working_type, a.attendance_status, u.name
FROM attendances a
LEFT JOIN users u ON u.id = a.user_id
WHERE {whereSql}
ORDER BY a.punchin_date DESC, a.id DESC
LIMIT {pageSize} OFFSET {offset}", cancellationToken, parameters.ToArray());

            var data = new List<object>();
            foreach (var row in rows)
            {
                var userId = ToUInt64(Obj(row, "user_id"));
                var hierarchyLevel = await HierarchyLevel(userId, authUserId, cancellationToken);
                data.Add(new
                {
                    attendance_id = Obj(row, "id"),
                    name = string.IsNullOrWhiteSpace(Str(row, "name")) ? "N/A" : Str(row, "name"),
                    date = DateDisplay(row, "punchin_date"),
                    punch_in = TimeDisplay(row, "punchin_time"),
                    punch_out = TimeDisplay(row, "punchout_time"),
                    working_type = Str(row, "working_type"),
                    status = ToInt(row, "attendance_status") == 1 ? "Approve" : ToInt(row, "attendance_status") == 2 ? "Rejected" : "Pending",
                    self = userId == authUserId,
                    hierarchy_level = hierarchyLevel,
                    hierarchy_label = hierarchyLevel == 0 ? "Self" : hierarchyLevel == -1 ? "Not in Hierarchy" : $"Level {hierarchyLevel}"
                });
            }

            var allStatus = new[] { new { id = "0", name = "Pending" }, new { id = "1", name = "Approved" }, new { id = "2", name = "Rejected" } };
            var response = new
            {
                status = "success",
                message = data.Count > 0 ? "Data retrieved successfully." : "No Record Found.",
                users,
                branches,
                page_count = total == 0 ? 1 : (long)Math.Ceiling(total / (double)pageSize),
                all_status = allStatus,
                data
            };

            return Ok(response);
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = ExceptionMessage(exception) });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("showAttendance")]
    public async Task<IActionResult> ShowAttendance(CancellationToken cancellationToken)
    {
        try
        {
            var body = await RequestBody(cancellationToken);
            var attendanceId = ULongValue("attendance_id", body);
            if (!attendanceId.HasValue)
            {
                return BadRequest(new { status = "error", message = new Dictionary<string, string[]> { ["attendance_id"] = ["The attendance id field is required."] } });
            }

            var attendance = (await QueryRows(@"SELECT a.*, u.id AS user_id_value, u.name AS user_name, u.email AS user_email, u.employee_codes AS user_employee_code
FROM attendances a
LEFT JOIN users u ON u.id = a.user_id
WHERE a.id = @attendance_id AND a.deleted_at IS NULL
LIMIT 1", cancellationToken, ("@attendance_id", attendanceId.Value))).FirstOrDefault();
            if (attendance is null)
            {
                return BadRequest(new { status = "error", message = "No Record Found." });
            }

            var data = new Dictionary<string, object?>(attendance, StringComparer.OrdinalIgnoreCase)
            {
                ["users"] = Obj(attendance, "user_id_value") is null ? null : new
                {
                    id = Obj(attendance, "user_id_value"),
                    name = Str(attendance, "user_name"),
                    email = Str(attendance, "user_email"),
                    employee_code = Str(attendance, "user_employee_code")
                }
            };
            data.Remove("user_id_value");
            data.Remove("user_name");
            data.Remove("user_email");
            data.Remove("user_employee_code");

            var tourDetails = await FormattedTourDetails(Str(attendance, "tourid"), cancellationToken);
            var cityNamesString = await CityNamesString(Str(attendance, "city"), cancellationToken);
            return Ok(new { status = "success", message = "Data retrieved successfully.", data, tour_details = tourDetails, city_names_string = cityNamesString });
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = ExceptionMessage(exception) });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("updateLiveLocation")]
    public async Task<IActionResult> UpdateLiveLocation(CancellationToken cancellationToken)
    {
        try
        {
            var locations = await ReadLiveLocations(cancellationToken);
            if (locations.Count == 0)
            {
                return BadRequest(new { status = "error", message = new Dictionary<string, string[]> { ["locations"] = ["The locations field is required."] } });
            }

            var userId = CurrentUserId();
            var lastLocation = (await QueryRows(@"SELECT latitude, longitude, time, created_at
FROM user_live_locations
WHERE userid = @user_id AND deleted_at IS NULL
ORDER BY time DESC, id DESC
LIMIT 1", cancellationToken, ("@user_id", userId))).FirstOrDefault();
            var inserted = 0;
            foreach (var location in locations)
            {
                if (!ShouldStoreLiveLocation(lastLocation, location.Latitude, location.Longitude, location.Time)) continue;
                var now = IndiaNow();
                await Execute(@"INSERT INTO user_live_locations (active, userid, latitude, longitude, time, created_at, updated_at)
VALUES ('Y', @user_id, @latitude, @longitude, @time, @now, @now)", cancellationToken,
                    ("@user_id", userId),
                    ("@latitude", location.Latitude),
                    ("@longitude", location.Longitude),
                    ("@time", location.Time),
                    ("@now", now));
                inserted++;
                lastLocation = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["latitude"] = location.Latitude,
                    ["longitude"] = location.Longitude,
                    ["time"] = location.Time,
                    ["created_at"] = now
                };
            }

            if (inserted == 0)
            {
                return Ok(new { status = "success", message = "Live location skipped. Last location is same or less than 4 minutes old." });
            }

            return Ok(new { status = "success", message = "Data inserted successfully." });
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = ExceptionMessage(exception) });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getUserSataus")]
    public async Task<IActionResult> GetUserStatus(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var active = await _dbContext.Users.IgnoreQueryFilters().Where(x => x.Id == userId).Select(x => x.Active).FirstOrDefaultAsync(cancellationToken);
        return Ok(new { status = "success", user_status = active });
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getLeaveBalance")]
    public async Task<IActionResult> GetLeaveBalance(CancellationToken cancellationToken)
    {
        try
        {
            var userId = CurrentUserId();
            var user = await _dbContext.Users.IgnoreQueryFilters().FirstAsync(x => x.Id == userId, cancellationToken);
            var since = IndiaNow().Date.AddDays(-60);
            var compOff = await _dbContext.CompOffLeaves
                .Where(x => x.UserId == (long)userId && x.CompOffDate >= since && !x.IsUsed)
                .SumAsync(x => x.Balance, cancellationToken);

            var data = new
            {
                leaveBalance = user.LeaveBalance,
                comb_off = compOff > 0 ? (object)compOff : "0"
            };

            return Ok(new { status = "success", message = "Data retrieved successfully.", data });
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = exception.Message });
        }
    }

    private ulong CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : throw new InvalidOperationException("Unauthenticated.");
    }

    private async Task AddCompOffIfNeeded(User user, DateTime today, CancellationToken cancellationToken)
    {
        var branchIds = (user.BranchId ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => ulong.TryParse(x, out var id) ? id : (ulong?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToArray();

        var isHoliday = false;
        if (branchIds.Length > 0)
        {
            var holidayRows = await _dbContext.Holidays.Where(x => x.Branch.HasValue && branchIds.Contains(x.Branch.Value)).Select(x => x.HolidayDate).ToListAsync(cancellationToken);
            isHoliday = holidayRows
                .SelectMany(x => (x ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Any(x => string.Equals(x, today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal));
        }

        if (today.DayOfWeek == DayOfWeek.Sunday || isHoliday)
        {
            await _dbContext.CompOffLeaves.AddAsync(new CompOffLeave
            {
                UserId = (long)user.Id,
                CompOffDate = today,
                ExpiryDate = today.AddDays(60),
                IsUsed = false,
                Balance = 1,
                CreatedAt = IndiaNow(),
                UpdatedAt = IndiaNow()
            }, cancellationToken);
        }
    }

    private async Task AddBeatSchedules(ulong userId, string? beats, string? tourId, DateTime today, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(beats)) return;
        var parsedTourId = ulong.TryParse((tourId ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault(), out var tour) ? tour : (ulong?)null;
        foreach (var beat in beats.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!ulong.TryParse(beat, out var beatId)) continue;
            await _dbContext.BeatSchedules.AddAsync(new BeatSchedule
            {
                UserId = userId,
                BeatId = beatId,
                TourId = parsedTourId,
                BeatDate = today,
                CreatedAt = IndiaNow()
            }, cancellationToken);
        }
    }

    private async Task UpdateTourOnPunchIn(PunchInForm request, DateTime today, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TourId)) return;
        var tourIds = request.TourId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => ulong.TryParse(x, out var id) ? id : (ulong?)null).Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        var tours = await _dbContext.TourProgrammes.Where(x => tourIds.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var tour in tours)
        {
            tour.Type = Truncate(request.Type?.Trim() ?? string.Empty, 50);
            tour.UpdatedAt = IndiaNow();
        }

        if (string.IsNullOrWhiteSpace(request.City)) return;
        foreach (var city in request.City.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var cityId = await ResolveCityId(city, cancellationToken);
            if (!cityId.HasValue) continue;
            foreach (var tourId in tourIds)
            {
                var detail = await _dbContext.TourDetails.FirstOrDefaultAsync(x => x.TourId == tourId && x.VisitedCityId == null, cancellationToken);
                if (detail is null)
                {
                    await _dbContext.TourDetails.AddAsync(new TourDetail
                    {
                        TourId = tourId,
                        VisitedCityId = cityId.Value,
                        VisitedDate = today,
                        LastVisited = today,
                        CreatedAt = IndiaNow(),
                        UpdatedAt = IndiaNow()
                    }, cancellationToken);
                }
                else
                {
                    detail.VisitedCityId = cityId.Value;
                    detail.VisitedDate = today;
                    detail.UpdatedAt = IndiaNow();
                }
            }
        }
    }

    private async Task<ulong?> ResolveCityId(string city, CancellationToken cancellationToken)
    {
        if (ulong.TryParse(city, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cityId)) return cityId;
        var row = (await QueryRows("SELECT id FROM cities WHERE city_name = @city_name AND deleted_at IS NULL LIMIT 1", cancellationToken, ("@city_name", city))).FirstOrDefault();
        return row is null ? null : ToUInt64(row["id"]);
    }

    private async Task<string> SaveAttendanceFile(IFormFile file, string prefix, CancellationToken cancellationToken)
    {
        var root = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "attendances");
        Directory.CreateDirectory(root);
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{prefix}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}";
        var path = Path.Combine(root, fileName);
        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream, cancellationToken);
        return $"attendances/{fileName}";
    }

    private async Task<decimal> QueryScalarDecimal(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var value = await QueryScalar(sql, cancellationToken, parameters);
        return ToDecimal(value);
    }

    private async Task<long> QueryScalarLong(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var value = await QueryScalar(sql, cancellationToken, parameters);
        return value is null or DBNull ? 0 : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private async Task<object?> QueryScalar(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }

        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task<int> Execute(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> QueryRows(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }

        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }

        return rows;
    }

    private async Task<List<ulong>> VisibleUserIds(ulong userId, CancellationToken cancellationToken)
    {
        var rows = await QueryRows("SELECT id, reportingid FROM users WHERE deleted_at IS NULL", cancellationToken);
        var visible = new HashSet<ulong> { userId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var row in rows)
            {
                var id = ToUInt64(Obj(row, "id"));
                var reportingId = ToUInt64(Obj(row, "reportingid"));
                if (reportingId > 0 && visible.Contains(reportingId) && visible.Add(id)) changed = true;
            }
        }

        return visible.ToList();
    }

    private async Task<List<ulong>> FilterUserIds(List<ulong> userIds, IReadOnlyDictionary<string, string> body, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return userIds;
        var where = new List<string>
        {
            $"u.id IN ({string.Join(',', userIds.Distinct())})",
            @"NOT EXISTS (
                SELECT 1 FROM model_has_roles m
                INNER JOIN roles r ON r.id = m.role_id
                WHERE m.model_id = u.id AND m.model_type = 'App\\Models\\User'
                AND (r.name = 'Distributor' OR m.role_id = 61)
            )"
        };
        var parameters = new List<(string, object?)>();
        var designationIds = ParseIds(RequestValue("designation", body));
        if (designationIds.Count > 0) where.Add($"u.designation_id IN ({string.Join(',', designationIds)})");

        var zoneId = ULongValue("zone_id", body);
        var zone = RequestValue("zone", body);
        if (zoneId.HasValue)
        {
            where.Add("u.division_id = @zone_id");
            parameters.Add(("@zone_id", zoneId.Value));
        }
        else if (!string.IsNullOrWhiteSpace(zone))
        {
            where.Add("d.division_name LIKE @zone");
            parameters.Add(("@zone", "%" + zone.Trim() + "%"));
        }

        var branchIds = ParseIds(RequestValue("branch_id", body));
        var branch = RequestValue("branch", body);
        if (branchIds.Count == 0 && !string.IsNullOrWhiteSpace(branch))
        {
            var branchRows = await QueryRows("SELECT id FROM branches WHERE branch_name LIKE @branch AND deleted_at IS NULL", cancellationToken, ("@branch", "%" + branch.Trim() + "%"));
            branchIds = branchRows.Select(x => ToUInt64(Obj(x, "id"))).Where(x => x > 0).ToList();
        }

        var searchBranches = ParseIds(RequestValue("search_branches", body));
        if (searchBranches.Count > 0) branchIds = searchBranches;
        if (branchIds.Count > 0)
        {
            where.Add("(" + string.Join(" OR ", branchIds.Select((id, i) => $"u.branch_id = @branch_{i} OR FIND_IN_SET(@branch_{i}, u.branch_id)")) + ")");
            for (var i = 0; i < branchIds.Count; i++) parameters.Add(($"@branch_{i}", branchIds[i].ToString(CultureInfo.InvariantCulture)));
        }

        var rows = await QueryRows($@"SELECT u.id
FROM users u
LEFT JOIN divisions d ON d.id = u.division_id
WHERE {string.Join(" AND ", where)}", cancellationToken, parameters.ToArray());
        return rows.Select(x => ToUInt64(Obj(x, "id"))).Where(x => x > 0).ToList();
    }

    private async Task<List<object>> UserOptions(IReadOnlyList<ulong> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return [];
        var rows = await QueryRows($"SELECT id, name FROM users WHERE id IN ({string.Join(',', userIds.Distinct())}) ORDER BY name ASC", cancellationToken);
        return rows.Select(row => (object)new { id = Obj(row, "id"), name = Str(row, "name") }).ToList();
    }

    private async Task<List<object>> BranchOptions(IReadOnlyList<ulong> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return [];
        var rows = await QueryRows($@"SELECT DISTINCT b.id, b.branch_name
FROM users u
INNER JOIN branches b ON b.id = CAST(NULLIF(SUBSTRING_INDEX(u.branch_id, ',', 1), '') AS UNSIGNED)
WHERE u.id IN ({string.Join(',', userIds.Distinct())}) AND b.deleted_at IS NULL
ORDER BY b.branch_name ASC", cancellationToken);
        return rows.Select(row => (object)new { id = Obj(row, "id"), name = Str(row, "branch_name") }).ToList();
    }

    private async Task<int> HierarchyLevel(ulong targetUserId, ulong authUserId, CancellationToken cancellationToken)
    {
        if (targetUserId == authUserId) return 0;
        var rows = await QueryRows("SELECT id, reportingid FROM users WHERE deleted_at IS NULL", cancellationToken);
        var current = targetUserId;
        for (var level = 1; level <= 20; level++)
        {
            var row = rows.FirstOrDefault(x => ToUInt64(Obj(x, "id")) == current);
            if (row is null) return -1;
            var reportingId = ToUInt64(Obj(row, "reportingid"));
            if (reportingId == authUserId) return level;
            if (reportingId == 0 || reportingId == current) return -1;
            current = reportingId;
        }

        return -1;
    }

    private async Task<List<object>> FormattedTourDetails(string? tourIdString, CancellationToken cancellationToken)
    {
        var tourIds = ParseIds(tourIdString);
        if (tourIds.Count == 0) return [];
        var rows = await QueryRows($@"SELECT tp.id, COALESCE(c.city_name, '') AS town_name, COALESCE(d.district_name, '') AS district_name, COALESCE(tp.objectives, '') AS objective
FROM tour_programmes tp
LEFT JOIN cities c ON c.id = CAST(NULLIF(tp.town, '') AS UNSIGNED)
LEFT JOIN districts d ON d.id = tp.district
WHERE tp.id IN ({string.Join(',', tourIds)})", cancellationToken);
        return rows.Select(row => (object)new
        {
            id = Obj(row, "id"),
            town_name = Str(row, "town_name"),
            district_name = Str(row, "district_name"),
            objective = Str(row, "objective")
        }).ToList();
    }

    private async Task<string> CityNamesString(string? cityIdsText, CancellationToken cancellationToken)
    {
        var cityIds = ParseIds(cityIdsText);
        if (cityIds.Count == 0) return string.Empty;
        var rows = await QueryRows($"SELECT id, city_name FROM cities WHERE id IN ({string.Join(',', cityIds)})", cancellationToken);
        var names = cityIds
            .Select(id => rows.FirstOrDefault(row => ToUInt64(Obj(row, "id")) == id))
            .Where(row => row is not null)
            .Select(row => Str(row!, "city_name"))
            .Where(name => !string.IsNullOrWhiteSpace(name));
        return string.Join(", ", names);
    }

    private static (DateTime From, DateTime To) ResolveDashboardDates(DashboardQuery query)
    {
        var now = IndiaNow();
        var from = ParseDate(query.FromDate) ?? now.Date;
        var to = ParseDate(query.ToDate) ?? now.Date;
        var year = now.Month > 3 ? now.AddYears(1).Year : now.Year;
        var lastYear = now.Month < 4 ? now.AddYears(-1).Year : now.Year;

        return query.FilterDate switch
        {
            "Today" => (now.Date, now.Date),
            "This Month" => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month))),
            "Last Month" => (new DateTime(now.AddMonths(-1).Year, now.AddMonths(-1).Month, 1), new DateTime(now.AddMonths(-1).Year, now.AddMonths(-1).Month, DateTime.DaysInMonth(now.AddMonths(-1).Year, now.AddMonths(-1).Month))),
            "Quarter 1" => (new DateTime(now.Year, 4, 1), new DateTime(now.Year, 6, 30)),
            "Quarter 2" => (new DateTime(now.Year, 7, 1), new DateTime(now.Year, 9, 30)),
            "Quarter 3" => (new DateTime(now.Year, 10, 1), new DateTime(now.Year, 12, 31)),
            "Quarter 4" => (new DateTime(year, 1, 1), new DateTime(year, 3, 31)),
            "YTM" => (new DateTime(lastYear, 4, 1), new DateTime(now.AddMonths(-1).Year, now.AddMonths(-1).Month, DateTime.DaysInMonth(now.AddMonths(-1).Year, now.AddMonths(-1).Month))),
            "Last Year" => (new DateTime(lastYear, 4, 1), new DateTime(year, 3, 31)),
            _ => (from, to)
        };
    }

    private static DateTime IndiaNow()
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
        }
        catch
        {
            return DateTime.UtcNow.AddHours(5).AddMinutes(30);
        }
    }

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ? date.Date : null;

    private static List<ulong> ParseIds(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => ulong.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

    private static object? Obj(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not DBNull ? value : null;

    private static string Str(IReadOnlyDictionary<string, object?> row, string key) =>
        Obj(row, key)?.ToString() ?? string.Empty;

    private static int ToInt(IReadOnlyDictionary<string, object?> row, string key) =>
        int.TryParse(Str(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static string DateSql(IReadOnlyDictionary<string, object?> row, string key) =>
        Obj(row, key) is DateTime date ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : Str(row, key);

    private static string DateDisplay(IReadOnlyDictionary<string, object?> row, string key) =>
        Obj(row, key) is DateTime date
            ? date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : DateTime.TryParse(Str(row, key), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
                ? parsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : string.Empty;

    private static string TimeDisplay(IReadOnlyDictionary<string, object?> row, string key) =>
        Obj(row, key) switch
        {
            null => string.Empty,
            TimeSpan time => time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
            DateTime date => date.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            var value => value.ToString() ?? string.Empty
        };

    private static decimal ToDecimal(object? value) =>
        value is null or DBNull ? 0 : Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    private static ulong ToUInt64(object? value) =>
        value is null or DBNull ? 0 : Convert.ToUInt64(value, CultureInfo.InvariantCulture);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static decimal WorkingDayValue(string? workedTime)
    {
        if (!TimeSpan.TryParse(workedTime, CultureInfo.InvariantCulture, out var time)) return 0;
        if (time.Hours >= 7) return 1m;
        if (time.Hours >= 4) return 0.5m;
        return 0;
    }

    private static string AmountConversion(decimal amount)
    {
        if (amount >= 1000 && amount <= 100000) return $"{amount / 1000:0.##}K";
        if (amount > 100000) return $"{amount / 100000:0.##}L";
        return amount.ToString(CultureInfo.InvariantCulture);
    }

    private static string Percent(decimal numerator, decimal denominator) =>
        $"{((numerator * 100) / denominator).ToString("0.0", CultureInfo.InvariantCulture)} %";

    private static string FormatTime(TimeSpan value) => value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

    private static string WorkedTime(DateTime now, DateTime punchinDate, TimeSpan punchinTime)
    {
        var total = now - punchinDate.Date.Add(punchinTime);
        if (total < TimeSpan.Zero) total = TimeSpan.Zero;
        return total.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string ExceptionMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException is not null) current = current.InnerException;
        return current.Message;
    }

    private static Dictionary<string, string[]> ValidationErrors(params (string Field, string? Value)[] fields) =>
        fields
            .Where(x => string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Field, x => new[] { $"The {x.Field.Replace('_', ' ')} field is required." });

    private static object ToAttendanceObject(Attendance attendance) => new
    {
        id = attendance.Id,
        active = attendance.Active,
        user_id = attendance.UserId,
        punchin_date = DateOnly.FromDateTime(attendance.PunchinDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        punchin_time = FormatTime(attendance.PunchinTime),
        punchin_longitude = attendance.PunchinLongitude,
        punchin_latitude = attendance.PunchinLatitude,
        punchin_address = attendance.PunchinAddress,
        punchin_image = attendance.PunchinImage,
        punchout_date = attendance.PunchoutDate.HasValue ? DateOnly.FromDateTime(attendance.PunchoutDate.Value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
        punchout_time = attendance.PunchoutTime.HasValue ? FormatTime(attendance.PunchoutTime.Value) : null,
        punchout_latitude = attendance.PunchoutLatitude,
        punchout_longitude = attendance.PunchoutLongitude,
        punchout_address = attendance.PunchoutAddress,
        punchout_image = attendance.PunchoutImage,
        punchin_summary = attendance.PunchinSummary,
        punchout_summary = attendance.PunchoutSummary,
        flag = attendance.Flag,
        worked_time = attendance.WorkedTime,
        attendance_status = attendance.AttendanceStatus,
        beat_id = attendance.BeatId,
        punchin_from = attendance.PunchinFrom,
        remark_status = attendance.RemarkStatus,
        approve_reject_by = attendance.ApproveRejectBy,
        created_at = attendance.CreatedAt,
        updated_at = attendance.UpdatedAt,
        working_type = attendance.WorkingType,
        tourid = attendance.TourId,
        city = attendance.City
    };

    private async Task<PunchInForm> ReadPunchInForm(CancellationToken cancellationToken)
    {
        var body = await RequestBody(cancellationToken);
        return new PunchInForm
        {
            PunchinLatitude = RequestValue("punchin_latitude", body),
            PunchinLongitude = RequestValue("punchin_longitude", body),
            PunchinAddress = RequestValue("punchin_address", body),
            Address = RequestValue("address", body),
            PunchinSummary = RequestValue("punchin_summary", body),
            Type = RequestValue("type", body),
            TourId = RequestValue("tourid", body) ?? RequestValue("tour_id", body),
            City = RequestValue("city", body),
            Beats = RequestValue("beats", body),
            Image = Request.HasFormContentType && Request.Form.Files.Count > 0 ? Request.Form.Files["image"] ?? Request.Form.Files[0] : null
        };
    }

    private async Task<PunchOutForm> ReadPunchOutForm(CancellationToken cancellationToken)
    {
        var body = await RequestBody(cancellationToken);
        return new PunchOutForm
        {
            PunchinId = ULongValue("punchin_id", body),
            PunchoutLatitude = RequestValue("punchout_latitude", body),
            PunchoutLongitude = RequestValue("punchout_longitude", body),
            PunchoutAddress = RequestValue("punchout_address", body),
            Address = RequestValue("address", body),
            PunchoutSummary = RequestValue("punchout_summary", body),
            Image = Request.HasFormContentType && Request.Form.Files.Count > 0 ? Request.Form.Files["image"] ?? Request.Form.Files[0] : null
        };
    }

    private async Task<Dictionary<string, string>> RequestBody(CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (Request.HasFormContentType)
        {
            foreach (var item in Request.Form)
            {
                values[item.Key] = item.Value.ToString();
            }

            return values;
        }

        if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                values[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }
        }

        return values;
    }

    private async Task<List<LiveLocationInput>> ReadLiveLocations(CancellationToken cancellationToken)
    {
        if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.TryGetProperty("locations", out var locationsElement) && locationsElement.ValueKind == JsonValueKind.Array)
            {
                return locationsElement.EnumerateArray().Select(LiveLocationFromJson).Where(x => x is not null).Cast<LiveLocationInput>().ToList();
            }

            var single = LiveLocationFromJson(root);
            return single is null ? [] : [single];
        }

        var body = await RequestBody(cancellationToken);
        var latitude = RequestValue("latitude", body);
        var longitude = RequestValue("longitude", body);
        var timeText = RequestValue("time", body);
        if (string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude)) return [];
        return [new LiveLocationInput(latitude, longitude, ParseDateTime(timeText) ?? IndiaNow())];
    }

    private static LiveLocationInput? LiveLocationFromJson(JsonElement element)
    {
        var latitude = JsonString(element, "latitude");
        var longitude = JsonString(element, "longitude");
        if (string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude)) return null;
        return new LiveLocationInput(latitude, longitude, ParseDateTime(JsonString(element, "time")) ?? IndiaNow());
    }

    private static string? JsonString(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var property)
            ? property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString()
            : null;

    private static DateTime? ParseDateTime(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ? date : null;

    private static bool ShouldStoreLiveLocation(IReadOnlyDictionary<string, object?>? lastLocation, string latitude, string longitude, DateTime locationTime)
    {
        if (lastLocation is null) return true;
        var lastTimeText = Str(lastLocation, "time");
        if (string.IsNullOrWhiteSpace(lastTimeText)) lastTimeText = Str(lastLocation, "created_at");
        if (!DateTime.TryParse(lastTimeText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var lastTime)) return true;
        return Math.Abs((locationTime - lastTime).TotalSeconds) >= 170;
    }

    private string? RequestValue(string key, IReadOnlyDictionary<string, string> body)
    {
        if (body.TryGetValue(key, out var bodyValue) && !string.IsNullOrWhiteSpace(bodyValue)) return bodyValue;
        if (Request.Query.TryGetValue(key, out var queryValue) && !string.IsNullOrWhiteSpace(queryValue)) return queryValue.ToString();
        return null;
    }

    private ulong? ULongValue(string key, IReadOnlyDictionary<string, string> body) =>
        ulong.TryParse(RequestValue(key, body), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    public sealed class DashboardQuery
    {
        [FromQuery(Name = "filter_date")]
        public string? FilterDate { get; init; }

        [FromQuery(Name = "fromdate")]
        public string? FromDate { get; init; }

        [FromQuery(Name = "todate")]
        public string? ToDate { get; init; }
    }

    public sealed class PunchInForm
    {
        [FromForm(Name = "punchin_latitude")]
        public string? PunchinLatitude { get; init; }

        [FromForm(Name = "punchin_longitude")]
        public string? PunchinLongitude { get; init; }

        [FromForm(Name = "punchin_address")]
        public string? PunchinAddress { get; init; }

        [FromForm(Name = "address")]
        public string? Address { get; init; }

        [FromForm(Name = "punchin_summary")]
        public string? PunchinSummary { get; init; }

        [FromForm(Name = "type")]
        public string? Type { get; init; }

        [FromForm(Name = "tourid")]
        public string? TourId { get; init; }

        [FromForm(Name = "city")]
        public string? City { get; init; }

        [FromForm(Name = "beats")]
        public string? Beats { get; init; }

        [FromForm(Name = "image")]
        public IFormFile? Image { get; init; }
    }

    public sealed class PunchOutForm
    {
        [FromForm(Name = "punchin_id")]
        public ulong? PunchinId { get; init; }

        [FromForm(Name = "punchout_latitude")]
        public string? PunchoutLatitude { get; init; }

        [FromForm(Name = "punchout_longitude")]
        public string? PunchoutLongitude { get; init; }

        [FromForm(Name = "punchout_address")]
        public string? PunchoutAddress { get; init; }

        [FromForm(Name = "address")]
        public string? Address { get; init; }

        [FromForm(Name = "punchout_summary")]
        public string? PunchoutSummary { get; init; }

        [FromForm(Name = "image")]
        public IFormFile? Image { get; init; }
    }

    private sealed record LiveLocationInput(string Latitude, string Longitude, DateTime Time);
}
