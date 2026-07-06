using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class FieldKonnectTourPlanController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public FieldKonnectTourPlanController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [AcceptVerbs("GET", "POST")]
    [Route("userCityList")]
    public async Task<IActionResult> UserCityList(CancellationToken cancellationToken)
    {
        try
        {
            var userId = ULongValue("user_id") ?? CurrentUserId();
            var cityName = RequestValue("cityname");
            var parameters = new List<(string, object?)> { ("@user_id", userId) };
            var where = "uca.userid = @user_id AND c.deleted_at IS NULL";
            if (!string.IsNullOrWhiteSpace(cityName))
            {
                where += " AND c.city_name LIKE @city_name";
                parameters.Add(("@city_name", cityName.Trim() + "%"));
            }

            var data = await QueryRows($@"SELECT c.id, c.city_name, c.grade
FROM user_city_assigns uca
INNER JOIN cities c ON c.id = uca.city_id
WHERE {where}
GROUP BY c.id, c.city_name, c.grade
ORDER BY c.city_name ASC", cancellationToken, parameters.ToArray());

            if (data.Count == 0) return Ok(new { status = "error", message = "No Record Found.", data });
            return Ok(new { status = "success", message = "Data retrieved successfully.", data });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpGet("tour/userlist")]
    public async Task<IActionResult> TourUserList(CancellationToken cancellationToken)
    {
        try
        {
            var authUserId = CurrentUserId();
            var userIds = await VisibleUserIds(authUserId, cancellationToken);

            var page = Math.Max(1, (int)(ULongValue("page") ?? 1));
            var perPage = Math.Clamp((int)(ULongValue("per_page") ?? ULongValue("pageSize") ?? 20), 1, 200);
            var offset = (page - 1) * perPage;
            var (where, parameters) = await UserFilterWhere(userIds, cancellationToken);
            parameters.Add(("@auth_user", authUserId));
            var total = await QueryScalarLong($"SELECT COUNT(DISTINCT u.id) FROM users u LEFT JOIN divisions d ON d.id = u.division_id WHERE {where}", cancellationToken, parameters.ToArray());
            var lastPage = total == 0 ? 1 : (long)Math.Ceiling(total / (double)perPage);
            var effectivePage = Math.Min(page, (int)lastPage);
            offset = (effectivePage - 1) * perPage;
            var rows = await QueryRows($@"SELECT u.id, u.name
FROM users u
LEFT JOIN divisions d ON d.id = u.division_id
WHERE {where}
GROUP BY u.id, u.name
ORDER BY CASE WHEN u.id = @auth_user THEN 0 ELSE 1 END, u.name
LIMIT {perPage} OFFSET {offset}", cancellationToken, parameters.ToArray());
            var users = rows.Select(x => new { user_id = ULong(x, "id"), name = Str(x, "name") }).ToList();
            var data = new
            {
                current_page = effectivePage,
                data = users,
                first_page_url = (string?)null,
                from = users.Count == 0 ? null : (int?)offset + 1,
                last_page = lastPage,
                last_page_url = (string?)null,
                links = Array.Empty<object>(),
                next_page_url = effectivePage < lastPage ? string.Empty : null,
                path = string.Empty,
                per_page = perPage,
                prev_page_url = effectivePage > 1 ? string.Empty : null,
                to = users.Count == 0 ? null : (int?)offset + users.Count,
                total
            };

            return Ok(new
            {
                status = "success",
                message = "Data retrieved successfully.",
                data,
                users,
                page_count = lastPage,
                total,
                current_page = effectivePage
            });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpGet("tour/show")]
    [HttpGet("tour-plans")]
    public async Task<IActionResult> ShowTour(CancellationToken cancellationToken)
    {
        try
        {
            var authUserId = CurrentUserId();
            var userId = ULongValue("user_id") ?? authUserId;
            var visibleUserIds = await VisibleUserIds(authUserId, cancellationToken);
            if (!visibleUserIds.Contains(userId))
            {
                return StatusCode(403, new { status = "error", message = "You can view only your own or reporting users tour plans." });
            }

            var exists = await QueryScalarLong("SELECT COUNT(*) FROM users WHERE id = @user_id AND deleted_at IS NULL", cancellationToken, ("@user_id", userId));
            if (exists == 0)
            {
                return BadRequest(new { status = "error", message = "The selected user id is invalid." });
            }

            var page = Math.Max(1, (int)(ULongValue("page") ?? 1));
            var perPage = Math.Clamp((int)(ULongValue("per_page") ?? 30), 1, 200);
            var offset = (page - 1) * perPage;
            var where = new List<string> { "tp.userid = @user_id", "tp.deleted_at IS NULL" };
            var parameters = new List<(string, object?)> { ("@user_id", userId) };

            var startDate = DateValue("start_date");
            var endDate = DateValue("end_date");
            if (startDate.HasValue && endDate.HasValue)
            {
                where.Add("tp.date BETWEEN @start_date AND @end_date");
                parameters.Add(("@start_date", startDate.Value.Date));
                parameters.Add(("@end_date", endDate.Value.Date));
            }

            var whereSql = string.Join(" AND ", where);
            var total = await QueryScalarLong($"SELECT COUNT(*) FROM tour_programmes tp WHERE {whereSql}", cancellationToken, parameters.ToArray());
            var rows = await QueryRows($@"SELECT tp.id, tp.date, tp.userid, tp.town, tp.district, tp.objectives, tp.type, tp.status, tp.created_by,
tp.created_at, tp.updated_at, tp.remark,
COALESCE(c.city_name, tp.town) AS town_name,
COALESCE(d.district_name, CAST(tp.district AS CHAR)) AS district_name
FROM tour_programmes tp
LEFT JOIN cities c ON c.id = CAST(NULLIF(tp.town, '') AS UNSIGNED)
LEFT JOIN districts d ON d.id = tp.district
WHERE {whereSql}
ORDER BY tp.date DESC
LIMIT {perPage} OFFSET {offset}", cancellationToken, parameters.ToArray());

            var formatted = rows.Select(row => new
            {
                id = Obj(row, "id"),
                date = DateDisplay(row, "date"),
                userid = Obj(row, "userid"),
                town = Obj(row, "town"),
                district = Obj(row, "district"),
                objectives = Str(row, "objectives"),
                type = Str(row, "type"),
                status = StatusLabel(row),
                created_by = Obj(row, "created_by"),
                created_at = Obj(row, "created_at"),
                updated_at = Obj(row, "updated_at"),
                remark = Obj(row, "remark"),
                self = ULong(row, "userid") == CurrentUserId() ? "true" : "false",
                town_name = Str(row, "town_name"),
                district_name = Str(row, "district_name")
            }).ToList();

            var lastPage = total == 0 ? 1 : (long)Math.Ceiling(total / (double)perPage);
            var data = new
            {
                current_page = page,
                data = formatted,
                first_page_url = (string?)null,
                from = formatted.Count == 0 ? null : (int?)offset + 1,
                last_page = lastPage,
                last_page_url = (string?)null,
                next_page_url = page < lastPage ? string.Empty : null,
                path = string.Empty,
                per_page = perPage,
                prev_page_url = page > 1 ? string.Empty : null,
                to = formatted.Count == 0 ? null : (int?)offset + formatted.Count,
                total
            };

            return Ok(new
            {
                status = "success",
                message = formatted.Count > 0 ? "Data retrieved successfully." : "No records found.",
                data,
                pagination = new { current_page = page, last_page = lastPage, per_page = perPage, total }
            });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpGet("tour/global")]
    public async Task<IActionResult> GlobalTour(CancellationToken cancellationToken)
    {
        try
        {
            var authUserId = CurrentUserId();
            var targetUserId = ULongValue("user_id") ?? authUserId;
            var page = Math.Max(1, (int)(ULongValue("page") ?? 1));
            var perPage = Math.Clamp((int)(ULongValue("per_page") ?? 30), 1, 200);
            var where = new List<string> { "tp.deleted_at IS NULL" };
            var parameters = new List<(string, object?)>();
            var startDate = DateValue("start_date");
            var endDate = DateValue("end_date");
            if (startDate.HasValue && endDate.HasValue)
            {
                where.Add("tp.date BETWEEN @start_date AND @end_date");
                parameters.Add(("@start_date", startDate.Value.Date));
                parameters.Add(("@end_date", endDate.Value.Date));
            }

            var requestedUserId = ULongValue("user_id");
            if (requestedUserId.HasValue)
            {
                where.Add("tp.userid = @user_id");
                parameters.Add(("@user_id", requestedUserId.Value));
            }

            var whereSql = string.Join(" AND ", where);
            var total = await QueryScalarLong($"SELECT COUNT(*) FROM tour_programmes tp WHERE {whereSql}", cancellationToken, parameters.ToArray());
            var rows = await QueryRows($@"SELECT tp.id, tp.date, tp.userid, tp.town, tp.district, tp.objectives, tp.type, tp.status, tp.created_by,
tp.created_at, tp.updated_at, tp.remark, u.name AS user_name,
COALESCE(c.city_name, tp.town) AS town_name,
COALESCE(d.district_name, CAST(tp.district AS CHAR)) AS district_name
FROM tour_programmes tp
LEFT JOIN users u ON u.id = tp.userid
LEFT JOIN cities c ON c.id = CAST(NULLIF(tp.town, '') AS UNSIGNED)
LEFT JOIN districts d ON d.id = tp.district
WHERE {whereSql}
ORDER BY tp.date DESC
LIMIT {perPage} OFFSET {(page - 1) * perPage}", cancellationToken, parameters.ToArray());
            var data = rows.Select(TourPlanObject).ToList();
            var hierarchyLevel = await HierarchyLevel(targetUserId, authUserId, cancellationToken);
            return Ok(new
            {
                status = "success",
                message = "Global tour plans retrieved successfully.",
                hierarchy_level = hierarchyLevel,
                hierarchy_label = hierarchyLevel == 0 ? "Self" : hierarchyLevel == -1 ? "Not in Hierarchy" : $"Level {hierarchyLevel}",
                data,
                pagination = new { current_page = page, last_page = total == 0 ? 1 : (long)Math.Ceiling(total / (double)perPage), per_page = perPage, total }
            });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpPost("tour-plan/changeStatus")]
    public async Task<IActionResult> ChangeTourStatus(CancellationToken cancellationToken)
    {
        try
        {
            var body = await RequestBody(cancellationToken);
            var tourId = ULongValue("tour_id", body);
            var statusText = RequestValue("status", body);
            var remark = RequestValue("remark", body);
            if (!tourId.HasValue) return BadRequest(new { status = "error", message = "The tour id field is required." });
            if (!int.TryParse(statusText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var status) || status is < 0 or > 2)
            {
                return BadRequest(new { status = "error", message = "The selected status is invalid." });
            }

            if (status == 2 && string.IsNullOrWhiteSpace(remark)) return BadRequest(new { status = "error", message = "Remark is required when rejecting." });
            var tour = (await QueryRows("SELECT id, userid FROM tour_programmes WHERE id = @id AND deleted_at IS NULL LIMIT 1", cancellationToken, ("@id", tourId.Value))).FirstOrDefault();
            if (tour is null) return BadRequest(new { status = "error", message = "The selected tour id is invalid." });
            var tourUserId = ULong(tour, "userid");
            var authUserId = CurrentUserId();
            var visibleUserIds = await VisibleUserIds(authUserId, cancellationToken);
            if (tourUserId == authUserId)
            {
                return StatusCode(403, new { status = "error", message = "You cannot approve or reject your own tour plan." });
            }
            if (!visibleUserIds.Contains(tourUserId))
            {
                return StatusCode(403, new { status = "error", message = "You can approve only reporting users tour plans." });
            }

            var now = IndiaNow();
            await Execute("UPDATE tour_programmes SET status = @status, remark = @remark, updated_at = @now WHERE id = @id", cancellationToken,
                ("@status", status), ("@remark", status == 2 ? remark : null), ("@now", now), ("@id", tourId.Value));
            var action = status == 1 ? "approved" : status == 2 ? "rejected" : "pending";
            var statusLabel = status == 1 ? "Approved" : status == 2 ? "Rejected" : "Pending";
            await InsertTourLog(tourId.Value, action, status.ToString(CultureInfo.InvariantCulture), string.IsNullOrWhiteSpace(remark) ? $"Status changed to {statusLabel}" : remark, now, cancellationToken);
            return Ok(new { status = "success", message = "Status updated successfully.", data = new { tour_id = tourId.Value, status, remark = status == 2 ? remark : null } });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("userDistrictList")]
    public async Task<IActionResult> UserDistrictList(CancellationToken cancellationToken)
    {
        try
        {
            var userId = ULongValue("user_id") ?? CurrentUserId();
            var districtName = RequestValue("districtname");
            var parameters = new List<(string, object?)> { ("@user_id", userId) };
            var where = "uca.userid = @user_id AND d.deleted_at IS NULL";
            if (!string.IsNullOrWhiteSpace(districtName))
            {
                where += " AND d.district_name LIKE @district_name";
                parameters.Add(("@district_name", districtName.Trim() + "%"));
            }

            var data = await QueryRows($@"SELECT DISTINCT d.id, d.district_name, d.state_id
FROM user_city_assigns uca
INNER JOIN cities c ON c.id = uca.city_id
INNER JOIN districts d ON d.id = c.district_id
WHERE {where}
ORDER BY d.district_name ASC", cancellationToken, parameters.ToArray());
            if (data.Count == 0) return Ok(new { status = "error", message = "No Record Found.", data });
            return Ok(new { status = "success", message = "Data retrieved successfully.", data });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("userCitiesByDistrict")]
    public async Task<IActionResult> UserCitiesByDistrict(CancellationToken cancellationToken)
    {
        try
        {
            var districtId = ULongValue("district_id");
            if (!districtId.HasValue) return BadRequest(new { status = "error", message = "district_id is required" });
            var userId = ULongValue("user_id") ?? CurrentUserId();
            var cityName = RequestValue("cityname");
            var parameters = new List<(string, object?)> { ("@user_id", userId), ("@district_id", districtId.Value) };
            var where = "uca.userid = @user_id AND c.district_id = @district_id AND c.deleted_at IS NULL";
            if (!string.IsNullOrWhiteSpace(cityName))
            {
                where += " AND c.city_name LIKE @city_name";
                parameters.Add(("@city_name", cityName.Trim() + "%"));
            }

            var data = await QueryRows($@"SELECT DISTINCT c.id, c.city_name, c.grade
FROM user_city_assigns uca
INNER JOIN cities c ON c.id = uca.city_id
WHERE {where}
ORDER BY c.city_name ASC", cancellationToken, parameters.ToArray());
            if (data.Count == 0) return Ok(new { status = "error", message = "No cities found in this district for the user.", data });
            return Ok(new { status = "success", message = "Cities retrieved successfully.", data });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpGet("user-attendance-zone-branch")]
    public async Task<IActionResult> UserAttendanceZoneBranch(CancellationToken cancellationToken)
    {
        try
        {
            var authUserId = CurrentUserId();
            var userIds = await VisibleUserIds(authUserId, cancellationToken);
            if (!userIds.Contains(authUserId)) userIds.Insert(0, authUserId);
            var (where, parameters) = await UserFilterWhere(userIds, cancellationToken, includeSearchName: false);
            var rows = await QueryRows($@"SELECT u.id, u.name, u.division_id, u.branch_id, d.id AS zone_id, d.division_name
FROM users u
LEFT JOIN divisions d ON d.id = u.division_id
WHERE {where}
ORDER BY u.name", cancellationToken, parameters.ToArray());
            var users = rows.Select(x => new { id = ULong(x, "id"), name = Str(x, "name") }).ToList();
            var zones = rows.Where(x => Obj(x, "zone_id") is not null)
                .GroupBy(x => ULong(x, "zone_id"))
                .Select(g => new { id = g.Key, name = Str(g.First(), "division_name"), zone_name = Str(g.First(), "division_name") })
                .OrderBy(x => x.name)
                .ToList();
            var branchZonePairs = new Dictionary<ulong, ulong?>();
            foreach (var row in rows)
            {
                foreach (var branchId in ParseIds(Str(row, "branch_id")))
                {
                    branchZonePairs[branchId] = Obj(row, "zone_id") is null ? null : ULong(row, "zone_id");
                }
            }

            var branches = branchZonePairs.Count == 0
                ? []
                : (await QueryRows($"SELECT id, branch_name FROM branches WHERE id IN ({string.Join(',', branchZonePairs.Keys)}) AND deleted_at IS NULL ORDER BY branch_name", cancellationToken))
                    .Select(x => new { id = ULong(x, "id"), name = Str(x, "branch_name"), branch_name = Str(x, "branch_name"), zone_id = branchZonePairs.GetValueOrDefault(ULong(x, "id")) })
                    .ToList<object>();
            return Ok(new { status = "success", success = true, message = "Assigned users basic list fetched successfully", data = new { users, zones, branches } });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", success = false, message = exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("upcommingTourProgramme")]
    public async Task<IActionResult> UpcomingTourProgramme(CancellationToken cancellationToken)
    {
        try
        {
            var userId = CurrentUserId();
            var filter = RequestValue("filter");
            var where = new List<string> { "tp.userid = @user_id", "tp.deleted_at IS NULL", "tp.type = ''" };
            var parameters = new List<(string, object?)> { ("@user_id", userId) };
            if (!string.IsNullOrWhiteSpace(filter))
            {
                where.Add("DATE(tp.date) = @today");
                parameters.Add(("@today", IndiaNow().Date));
            }

            var data = await QueryRows($@"SELECT tp.id, tp.date, tp.userid, tp.town, tp.objectives, tp.type, tp.status
FROM tour_programmes tp
WHERE {string.Join(" AND ", where)}
ORDER BY tp.id DESC", cancellationToken, parameters.ToArray());

            if (data.Count == 0) return Ok(new { status = "error", message = "No Record Found.", data });
            return Ok(new { status = "success", message = "Data retrieved successfully.", data });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpPost("tour/add")]
    [HttpPost("tour-plans")]
    [HttpPost("addTourProgramme")]
    public async Task<IActionResult> CreateTourPlans(CancellationToken cancellationToken)
    {
        try
        {
            var inputs = await TourInputs(cancellationToken);
            if (inputs.Count == 0)
            {
                return StatusCode(422, new { status = "error", message = new { tours = new[] { "The tours/programme field is required." } } });
            }

            var authUserId = CurrentUserId();
            var userId = inputs.FirstOrDefault(input => input.UserId.HasValue)?.UserId ?? ULongValue("user_id") ?? authUserId;
            var visibleUserIds = await VisibleUserIds(authUserId, cancellationToken);
            if (!visibleUserIds.Contains(userId))
            {
                return StatusCode(403, new { status = "error", message = "You can create tour plans only for yourself or reporting users." });
            }
            var now = IndiaNow();
            var created = new List<object?>();
            foreach (var input in inputs)
            {
                if (!input.Date.HasValue)
                {
                    return StatusCode(422, new { status = "error", message = new { date = new[] { "The date field is required." } } });
                }

                var town = await ResolveTown(input.Town, input.CityId, cancellationToken);
                if (string.IsNullOrWhiteSpace(town))
                {
                    return StatusCode(422, new { status = "error", message = new { town = new[] { "The town field is required." } } });
                }

                var district = await ResolveDistrict(input.District, input.CityId, town, cancellationToken);
                var tourId = await NextId("tour_programmes", cancellationToken);
                await Execute(@"INSERT INTO tour_programmes (id, date, userid, town, district, objectives, type, status, created_by, created_at, updated_at)
VALUES (@id, @date, @user_id, @town, @district, @objectives, @type, 0, @created_by, @now, @now)", cancellationToken,
                    ("@id", tourId),
                    ("@date", (object?)input.Date.Value.Date),
                    ("@user_id", userId),
                    ("@town", town),
                    ("@district", district ?? 0),
                    ("@objectives", input.Objectives ?? string.Empty),
                    ("@type", input.Type ?? string.Empty),
                    ("@created_by", CurrentUserId()),
                    ("@now", (object?)now));

                await CreateTourDetailIfCityResolved(tourId, userId, input.CityId, town, now, cancellationToken);
                await InsertTourLog(tourId, "created", "0", "Tour plan created", now, cancellationToken);
                created.Add(await TourRow(tourId, cancellationToken));
            }

            var message = Request.Path.Value?.Contains("tour-plans", StringComparison.OrdinalIgnoreCase) == true
                ? $"{created.Count} tour plan(s) created"
                : Request.Path.Value?.Contains("tour/add", StringComparison.OrdinalIgnoreCase) == true
                    ? $"Tour plan(s) processed successfully. Created: {created.Count}."
                    : "Data inserted successfully.";
            var statusCode = Request.Path.Value?.Contains("tour-plans", StringComparison.OrdinalIgnoreCase) == true ? 201 : 200;
            return StatusCode(statusCode, new { status = "success", message, data = created });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    private async Task<object?> QueryScalar(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task<long> QueryScalarLong(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var value = await QueryScalar(sql, cancellationToken, parameters);
        return value is null or DBNull ? 0 : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private async Task<ulong> NextId(string table, CancellationToken cancellationToken)
    {
        var value = await QueryScalar($"SELECT COALESCE(MAX(id), 0) + 1 FROM {table}", cancellationToken);
        return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
    }

    private async Task InsertTourLog(ulong tourId, string action, string status, string remark, DateTime now, CancellationToken cancellationToken)
    {
        var logId = await NextId("tour_logs", cancellationToken);
        await Execute(@"INSERT INTO tour_logs (id, tour_programme_id, action, status, performed_by, remark, created_at, updated_at)
VALUES (@id, @tour_id, @action, @status, @user_id, @remark, @now, @now)", cancellationToken,
            ("@id", logId),
            ("@tour_id", tourId),
            ("@action", action),
            ("@status", status),
            ("@user_id", CurrentUserId()),
            ("@remark", remark),
            ("@now", now));
    }

    private async Task<int> Execute(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> QueryRows(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    private static void AddParameters(IDbCommand command, IEnumerable<(string Name, object? Value)> parameters)
    {
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }
    }

    private string? RequestValue(string key)
    {
        if (Request.Query.TryGetValue(key, out var queryValue) && !string.IsNullOrWhiteSpace(queryValue)) return queryValue.ToString();
        if (Request.HasFormContentType && Request.Form.TryGetValue(key, out var formValue) && !string.IsNullOrWhiteSpace(formValue)) return formValue.ToString();
        return null;
    }

    private async Task<IReadOnlyList<TourInput>> TourInputs(CancellationToken cancellationToken)
    {
        if (Request.HasFormContentType)
        {
            var form = Request.Form;
            var dates = Values(form, "date").DefaultIfEmpty(form["programme_date"].ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            var towns = Values(form, "town");
            var districts = Values(form, "district");
            var objectives = Values(form, "objectives");
            var types = Values(form, "type");
            var cityIds = Values(form, "city_id");
            if (dates.Length == 0 && form.Keys.Any(x => x.StartsWith("programme", StringComparison.OrdinalIgnoreCase)))
            {
                dates = IndexedValues(form, "programme", "programme_date");
                towns = IndexedValues(form, "programme", "town");
                objectives = IndexedValues(form, "programme", "objectives");
                types = IndexedValues(form, "programme", "type");
                cityIds = IndexedValues(form, "programme", "city_id");
            }

            return Enumerable.Range(0, dates.Length)
                .Select(i => new TourInput(ParseDate(dates.ElementAtOrDefault(i)), towns.ElementAtOrDefault(i), districts.ElementAtOrDefault(i), objectives.ElementAtOrDefault(i), types.ElementAtOrDefault(i), ParseULong(cityIds.ElementAtOrDefault(i)), ParseULong(form["user_id"].ToString())))
                .ToList();
        }

        if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var bodyUserId = ParseULong(JsonString(root, "user_id"));
            if (TryArray(root, "tours", out var tours)) return TourInputsFromArray(tours, "date", bodyUserId).ToList();
            if (TryArray(root, "programme", out var programme)) return TourInputsFromArray(programme, "programme_date", bodyUserId).ToList();
            if (TryArray(root, "date", out var dates)) return TourInputsFromParallelArrays(root, dates, bodyUserId).ToList();
            if (root.ValueKind == JsonValueKind.Object)
            {
                return [new TourInput(ParseDate(JsonString(root, "date") ?? JsonString(root, "programme_date")), JsonString(root, "town"), JsonString(root, "district"), JsonString(root, "objectives"), JsonString(root, "type"), ParseULong(JsonString(root, "city_id")), bodyUserId)];
            }
        }

        return [];
    }

    private async Task<string?> ResolveTown(string? town, ulong? cityId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(town)) return town.Trim();
        if (!cityId.HasValue) return null;
        var city = await QueryScalar("SELECT city_name FROM cities WHERE id = @id AND deleted_at IS NULL LIMIT 1", cancellationToken, ("@id", cityId.Value));
        return Convert.ToString(city, CultureInfo.InvariantCulture);
    }

    private async Task<ulong?> ResolveDistrict(string? district, ulong? cityId, string town, CancellationToken cancellationToken)
    {
        if (ulong.TryParse(district, NumberStyles.Integer, CultureInfo.InvariantCulture, out var districtId)) return districtId;
        if (!string.IsNullOrWhiteSpace(district))
        {
            var fromName = await QueryScalar("SELECT id FROM districts WHERE district_name = @name AND deleted_at IS NULL LIMIT 1", cancellationToken, ("@name", district.Trim()));
            if (fromName is not null and not DBNull) return Convert.ToUInt64(fromName, CultureInfo.InvariantCulture);
        }

        if (cityId.HasValue)
        {
            var fromCity = await QueryScalar("SELECT district_id FROM cities WHERE id = @id AND deleted_at IS NULL LIMIT 1", cancellationToken, ("@id", cityId.Value));
            if (fromCity is not null and not DBNull) return Convert.ToUInt64(fromCity, CultureInfo.InvariantCulture);
        }

        var fromTown = await QueryScalar("SELECT district_id FROM cities WHERE city_name = @name AND deleted_at IS NULL LIMIT 1", cancellationToken, ("@name", town));
        return fromTown is null or DBNull ? null : Convert.ToUInt64(fromTown, CultureInfo.InvariantCulture);
    }

    private async Task CreateTourDetailIfCityResolved(ulong tourId, ulong userId, ulong? cityId, string town, DateTime now, CancellationToken cancellationToken)
    {
        var resolvedCityId = cityId;
        if (!resolvedCityId.HasValue)
        {
            var city = await QueryScalar("SELECT id FROM cities WHERE city_name = @name AND deleted_at IS NULL LIMIT 1", cancellationToken, ("@name", town));
            if (city is not null and not DBNull) resolvedCityId = Convert.ToUInt64(city, CultureInfo.InvariantCulture);
        }

        if (!resolvedCityId.HasValue) return;
        var lastVisited = await QueryScalar(@"SELECT td.visited_date FROM tour_details td
INNER JOIN tour_programmes tp ON tp.id = td.tourid
WHERE tp.userid = @user_id AND td.visited_cityid = @city_id AND td.visited_date IS NOT NULL
ORDER BY td.visited_date DESC LIMIT 1", cancellationToken, ("@user_id", userId), ("@city_id", resolvedCityId.Value));
        var detailId = await NextId("tour_details", cancellationToken);
        await Execute("INSERT INTO tour_details (id, tourid, city_id, last_visited, created_at, updated_at) VALUES (@id, @tour_id, @city_id, @last_visited, @now, @now)", cancellationToken,
            ("@id", detailId), ("@tour_id", tourId), ("@city_id", resolvedCityId.Value), ("@last_visited", lastVisited is DBNull ? null : lastVisited), ("@now", now));
    }

    private async Task<object?> TourRow(ulong tourId, CancellationToken cancellationToken)
    {
        var row = (await QueryRows("SELECT id, date, userid, town, district, objectives, type, status, created_by, created_at, updated_at FROM tour_programmes WHERE id = @id LIMIT 1", cancellationToken, ("@id", tourId))).FirstOrDefault();
        return row is null ? null : new
        {
            id = Obj(row, "id"),
            date = Obj(row, "date"),
            userid = Obj(row, "userid"),
            town = Obj(row, "town"),
            district = Obj(row, "district"),
            objectives = Str(row, "objectives"),
            type = Str(row, "type"),
            status = Obj(row, "status"),
            created_by = Obj(row, "created_by"),
            created_at = Obj(row, "created_at"),
            updated_at = Obj(row, "updated_at")
        };
    }

    private static string[] Values(IFormCollection form, string key) => form.TryGetValue(key, out var values) ? values.Select(x => x ?? string.Empty).ToArray() : [];
    private static string[] IndexedValues(IFormCollection form, string root, string key) =>
        form.Where(x => x.Key.StartsWith(root + "[", StringComparison.OrdinalIgnoreCase) && x.Key.EndsWith("][" + key + "]", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Value.ToString())
            .ToArray();

    private static IEnumerable<TourInput> TourInputsFromArray(JsonElement array, string dateKey, ulong? bodyUserId)
    {
        foreach (var item in array.EnumerateArray())
        {
            yield return new TourInput(ParseDate(JsonString(item, dateKey) ?? JsonString(item, "date")), JsonString(item, "town"), JsonString(item, "district"), JsonString(item, "objectives"), JsonString(item, "type"), ParseULong(JsonString(item, "city_id")), ParseULong(JsonString(item, "user_id")) ?? bodyUserId);
        }
    }

    private static IEnumerable<TourInput> TourInputsFromParallelArrays(JsonElement root, JsonElement dates, ulong? bodyUserId)
    {
        var towns = JsonArray(root, "town");
        var districts = JsonArray(root, "district");
        var objectives = JsonArray(root, "objectives");
        var types = JsonArray(root, "type");
        var cityIds = JsonArray(root, "city_id");
        var dateItems = dates.EnumerateArray().ToArray();
        for (var i = 0; i < dateItems.Length; i++)
        {
            yield return new TourInput(ParseDate(dateItems[i].ToString()), JsonAt(towns, i), JsonAt(districts, i), JsonAt(objectives, i), JsonAt(types, i), ParseULong(JsonAt(cityIds, i)), bodyUserId);
        }
    }

    private ulong? ULongValue(string key) => ulong.TryParse(RequestValue(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    private DateTime? DateValue(string key) => DateTime.TryParse(RequestValue(key), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var value) ? value : null;
    private static ulong? ParseULong(string? value) => ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var formats = new[] { "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy/MM/dd" };
        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var exactDate)) return exactDate;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ? date : null;
    }
    private static bool TryArray(JsonElement root, string key, out JsonElement array) => root.TryGetProperty(key, out array) && array.ValueKind == JsonValueKind.Array;
    private static string? JsonString(JsonElement item, string key) => item.ValueKind == JsonValueKind.Object && item.TryGetProperty(key, out var value) ? value.ToString() : null;
    private static JsonElement[] JsonArray(JsonElement root, string key) => root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().ToArray() : [];
    private static string? JsonAt(JsonElement[] array, int index) => index < array.Length ? array[index].ToString() : null;
    private static object? Obj(Dictionary<string, object?> row, string key) => row.TryGetValue(key, out var value) && value is not DBNull ? value : null;
    private static string Str(Dictionary<string, object?> row, string key) => Convert.ToString(Obj(row, key), CultureInfo.InvariantCulture) ?? string.Empty;
    private static ulong ULong(Dictionary<string, object?> row, string key) => Obj(row, key) is null ? 0 : Convert.ToUInt64(Obj(row, key), CultureInfo.InvariantCulture);
    private static string DateDisplay(Dictionary<string, object?> row, string key) => Obj(row, key) is null ? string.Empty : Convert.ToDateTime(Obj(row, key), CultureInfo.InvariantCulture).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    private static DateTime IndiaNow() => DateTime.UtcNow.AddHours(5).AddMinutes(30);
    private static string StatusLabel(Dictionary<string, object?> row) => Convert.ToInt32(Obj(row, "status") ?? 0, CultureInfo.InvariantCulture) switch
    {
        1 => "Approved",
        2 => "Rejected",
        _ => "Pending"
    };

    private async Task<Dictionary<string, string>> RequestBody(CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (Request.HasFormContentType)
        {
            foreach (var item in Request.Form) values[item.Key] = item.Value.ToString();
            return values;
        }

        if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                values[property.Name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString();
            }
        }

        return values;
    }

    private string? RequestValue(string key, IReadOnlyDictionary<string, string> body)
    {
        if (body.TryGetValue(key, out var bodyValue) && !string.IsNullOrWhiteSpace(bodyValue)) return bodyValue;
        return RequestValue(key);
    }

    private ulong? ULongValue(string key, IReadOnlyDictionary<string, string> body) =>
        ulong.TryParse(RequestValue(key, body), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

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
                var id = ULong(row, "id");
                var reportingId = ULong(row, "reportingid");
                if (reportingId > 0 && visible.Contains(reportingId) && visible.Add(id)) changed = true;
            }
        }

        return visible.ToList();
    }

    private async Task<(string Where, List<(string, object?)> Parameters)> UserFilterWhere(IReadOnlyList<ulong> userIds, CancellationToken cancellationToken, bool includeSearchName = true)
    {
        var where = new List<string>
        {
            userIds.Count == 0 ? "1 = 0" : $"u.id IN ({string.Join(',', userIds.Distinct())})",
            "u.deleted_at IS NULL",
            "u.active = 'Y'",
            "COALESCE(u.isDeleted, 0) = 0",
            @"NOT EXISTS (
                SELECT 1 FROM model_has_roles m
                INNER JOIN roles r ON r.id = m.role_id
                WHERE m.model_id = u.id AND m.model_type = 'App\\Models\\User'
                AND (r.name = 'Distributor' OR m.role_id = 61)
            )"
        };
        var parameters = new List<(string, object?)>();

        var designation = RequestValue("designation");
        var designationIds = ParseIds(designation);
        if (designationIds.Count > 0) where.Add($"u.designation_id IN ({string.Join(',', designationIds)})");

        var zoneId = ULongValue("zone_id");
        var zone = RequestValue("zone");
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

        var branchIds = ParseIds(RequestValue("branch_id"));
        var branch = RequestValue("branch");
        if (branchIds.Count == 0 && !string.IsNullOrWhiteSpace(branch))
        {
            var branchRows = await QueryRows("SELECT id FROM branches WHERE branch_name LIKE @branch AND deleted_at IS NULL", cancellationToken, ("@branch", "%" + branch.Trim() + "%"));
            branchIds = branchRows.Select(x => ULong(x, "id")).Where(x => x > 0).ToList();
        }

        if (branchIds.Count > 0)
        {
            where.Add("(" + string.Join(" OR ", branchIds.Select((id, i) => $"u.branch_id = @branch_{i} OR FIND_IN_SET(@branch_{i}, u.branch_id)")) + ")");
            for (var i = 0; i < branchIds.Count; i++) parameters.Add(($"@branch_{i}", branchIds[i].ToString(CultureInfo.InvariantCulture)));
        }

        var searchName = RequestValue("search_name");
        if (includeSearchName && !string.IsNullOrWhiteSpace(searchName))
        {
            where.Add("u.name LIKE @search_name");
            parameters.Add(("@search_name", "%" + searchName.Trim() + "%"));
        }

        return (string.Join(" AND ", where), parameters);
    }

    private async Task<int> HierarchyLevel(ulong targetUserId, ulong authUserId, CancellationToken cancellationToken)
    {
        if (targetUserId == authUserId) return 0;
        var rows = await QueryRows("SELECT id, reportingid FROM users WHERE deleted_at IS NULL", cancellationToken);
        var current = targetUserId;
        for (var level = 1; level <= 20; level++)
        {
            var row = rows.FirstOrDefault(x => ULong(x, "id") == current);
            if (row is null) return -1;
            var reportingId = ULong(row, "reportingid");
            if (reportingId == authUserId) return level;
            if (reportingId == 0 || reportingId == current) return -1;
            current = reportingId;
        }

        return -1;
    }

    private static List<ulong> ParseIds(string? csv) =>
        (csv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => ulong.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0)
        .Where(x => x > 0)
        .ToList();

    private object TourPlanObject(Dictionary<string, object?> row) => new
    {
        id = Obj(row, "id"),
        date = DateDisplay(row, "date"),
        userid = Obj(row, "userid"),
        user = new { id = Obj(row, "userid"), name = Str(row, "user_name") },
        user_name = Str(row, "user_name"),
        town = Obj(row, "town"),
        district = Obj(row, "district"),
        objectives = Str(row, "objectives"),
        type = Str(row, "type"),
        status = StatusLabel(row),
        created_by = Obj(row, "created_by"),
        created_at = Obj(row, "created_at"),
        updated_at = Obj(row, "updated_at"),
        remark = Obj(row, "remark"),
        self = ULong(row, "userid") == CurrentUserId() ? "true" : "false",
        town_name = Str(row, "town_name"),
        district_name = Str(row, "district_name")
    };

    private ulong CurrentUserId() => ulong.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new InvalidOperationException("Unauthenticated.");

    private sealed record TourInput(DateTime? Date, string? Town, string? District, string? Objectives, string? Type, ulong? CityId, ulong? UserId);
}
