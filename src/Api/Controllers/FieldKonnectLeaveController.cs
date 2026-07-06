using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class FieldKonnectLeaveController : ControllerBase
{
    private static readonly HashSet<string> ValidLeaveTypes = ["Leave", "Full Day Leave", "First Half Leave", "Second Half Leave"];
    private static readonly HashSet<string> ValidBalanceTypes = ["Casual Balance", "Sick Balance", "Earned Balance", "Comp-off Balance"];
    private readonly AppDbContext _dbContext;

    public FieldKonnectLeaveController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [AcceptVerbs("GET", "POST")]
    [Route("addLeaves")]
    public async Task<IActionResult> AddLeaves(CancellationToken cancellationToken)
    {
        try
        {
            var request = await ReadLeaveForm(cancellationToken);
            var errors = ValidateLeave(request, requireUserId: true);
            if (errors.Count > 0)
            {
                return BadRequest(new { status = "error", message = "Validation failed", errors });
            }

            var user = (await QueryRows("SELECT id, casual_leave_balance, sick_leave_balance, earned_leave_balance, compb_off FROM users WHERE id = @id AND deleted_at IS NULL LIMIT 1", cancellationToken, ("@id", request.UserId!.Value))).FirstOrDefault();
            if (user is null)
            {
                return BadRequest(new { status = "error", message = "Validation failed", errors = new { user_id = new[] { "The selected user id is invalid." } } });
            }

            var from = request.FromDate!.Value.Date;
            var to = request.ToDate!.Value.Date;
            var leaveDays = LeaveDays(from, to, request.Type!);
            var balanceColumn = BalanceColumn(request.BalType!);
            var available = Dec(user, balanceColumn);
            if (request.BalType != "Casual Balance" && available < leaveDays)
            {
                return BadRequest(new { status = "error", message = $"Insufficient {request.BalType} balance. Available: {available}, Required: {leaveDays}" });
            }

            var now = IndiaNow();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await Execute($"UPDATE users SET {balanceColumn} = @balance, updated_at = @now WHERE id = @user_id", cancellationToken,
                ("@balance", Math.Max(0, available - leaveDays)),
                ("@now", now),
                ("@user_id", request.UserId.Value));

            await Execute(@"INSERT INTO leaves (active, user_id, from_date, to_date, type, bal_type, reason, created_by, status, created_at, updated_at)
VALUES ('Y', @user_id, @from_date, @to_date, @type, @bal_type, @reason, @created_by, 0, @now, @now)", cancellationToken,
                ("@user_id", request.UserId.Value),
                ("@from_date", from),
                ("@to_date", to),
                ("@type", request.Type),
                ("@bal_type", request.BalType),
                ("@reason", request.Reason ?? string.Empty),
                ("@created_by", CurrentUserIdOr(request.UserId.Value)),
                ("@now", now));

            var leaveId = Convert.ToUInt64(await QueryScalar("SELECT LAST_INSERT_ID()", cancellationToken), CultureInfo.InvariantCulture);
            await MarkLeaveInAttendance(request.UserId.Value, from, to, request.Type!, request.Reason, now, cancellationToken);
            if (request.BalType == "Comp-off Balance")
            {
                await MarkCompOffAsUsed(request.UserId.Value, leaveId, leaveDays, request.Type is "First Half Leave" or "Second Half Leave", now, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            var leave = await LeaveWithUsers(leaveId, cancellationToken);
            return StatusCode(201, new { status = "success", message = "Leave applied successfully", data = leave });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = "Failed to apply leave: " + exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getLeaves")]
    public async Task<IActionResult> GetLeaves([FromQuery(Name = "user_id")] ulong? queryUserId, [FromForm(Name = "user_id")] ulong? formUserId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = queryUserId ?? formUserId;
            if (!userId.HasValue)
            {
                return BadRequest(new { status = "error", message = "Validation failed", errors = new { user_id = new[] { "The user id field is required." } } });
            }

            var exists = await QueryScalarLong("SELECT COUNT(*) FROM users WHERE id = @id AND deleted_at IS NULL", cancellationToken, ("@id", userId.Value));
            if (exists == 0)
            {
                return BadRequest(new { status = "error", message = "Validation failed", errors = new { user_id = new[] { "The selected user id is invalid." } } });
            }

            var rows = await QueryRows(@"SELECT l.*, u.name AS user_name, u.employee_codes AS user_employee_code, cb.name AS created_by_name
FROM leaves l
LEFT JOIN users u ON u.id = l.user_id
LEFT JOIN users cb ON cb.id = l.created_by
WHERE l.user_id = @user_id
ORDER BY l.created_at DESC, l.id DESC", cancellationToken, ("@user_id", userId.Value));
            var data = rows.Select(LeaveObject).ToList();
            return Ok(new { status = "success", message = "Leaves retrieved successfully", data });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpGet("leaves/balance")]
    public async Task<IActionResult> GetMyBalances(CancellationToken cancellationToken)
    {
        try
        {
            var row = (await QueryRows(@"SELECT casual_leave_balance, sick_leave_balance, earned_leave_balance, claimable_earned_leave_balance
FROM users WHERE id = @id AND deleted_at IS NULL LIMIT 1", cancellationToken, ("@id", CurrentUserId()))).FirstOrDefault();
            if (row is null) return Unauthorized(new { status = false, message = "Unauthenticated" });

            var activeCompOff = await QueryScalarDecimal("SELECT COALESCE(SUM(balance), 0) FROM comp_off_leaves WHERE user_id = @user_id AND is_used = 0 AND expiry_date >= @today", cancellationToken,
                ("@user_id", CurrentUserId()),
                ("@today", IndiaNow().Date));
            var data = new
            {
                casual = Dec(row, "casual_leave_balance"),
                sick = Dec(row, "sick_leave_balance"),
                earned = Dec(row, "earned_leave_balance"),
                claimable_earned = Dec(row, "claimable_earned_leave_balance"),
                comp_off = Math.Round(activeCompOff, 2)
            };
            return Ok(new { status = true, message = "Balances fetched successfully", data });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = false, message = "Failed to fetch balances", error = exception.Message });
        }
    }

    private async Task MarkLeaveInAttendance(ulong userId, DateTime from, DateTime to, string type, string? reason, DateTime now, CancellationToken cancellationToken)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var existing = await QueryScalar("SELECT id FROM attendances WHERE user_id = @user_id AND punchin_date = @date AND deleted_at IS NULL LIMIT 1", cancellationToken,
                ("@user_id", userId), ("@date", date));
            if (existing is null or DBNull)
            {
                await Execute(@"INSERT INTO attendances (active, user_id, punchin_date, punchin_time, punchin_summary, working_type, punchin_from, attendance_status, created_at, updated_at)
VALUES ('Y', @user_id, @date, '10:00:00', @summary, @type, 'App', 0, @now, @now)", cancellationToken,
                    ("@user_id", userId), ("@date", date), ("@summary", reason ?? "Leave applied"), ("@type", type), ("@now", now));
            }
            else
            {
                await Execute(@"UPDATE attendances SET active = 'Y', punchin_time = '10:00:00', punchin_summary = @summary,
working_type = @type, punchin_from = 'App', attendance_status = 0, updated_at = @now WHERE id = @id", cancellationToken,
                    ("@summary", reason ?? "Leave applied"), ("@type", type), ("@now", now), ("@id", existing));
            }
        }
    }

    private async Task MarkCompOffAsUsed(ulong userId, ulong leaveId, decimal daysNeeded, bool isHalfDay, DateTime now, CancellationToken cancellationToken)
    {
        if (isHalfDay)
        {
            var compOff = (await QueryRows(@"SELECT id, leave_id, balance FROM comp_off_leaves
WHERE user_id = @user_id AND is_used = 0 AND expiry_date >= @today AND balance >= 0.5
ORDER BY expiry_date ASC, id ASC LIMIT 1", cancellationToken, ("@user_id", userId), ("@today", now.Date))).FirstOrDefault();
            if (compOff is null) return;
            var balance = Dec(compOff, "balance") - 0.5m;
            await Execute("UPDATE comp_off_leaves SET balance = @balance, is_used = @is_used, leave_id = @leave_id, updated_at = @now WHERE id = @id", cancellationToken,
                ("@balance", Math.Max(0, balance)),
                ("@is_used", balance <= 0 ? 1 : 0),
                ("@leave_id", AppendLeaveId(Str(compOff, "leave_id"), leaveId)),
                ("@now", now),
                ("@id", ULong(compOff, "id")));
            return;
        }

        var rows = await QueryRows($@"SELECT id, leave_id FROM comp_off_leaves
WHERE user_id = @user_id AND is_used = 0 AND expiry_date >= @today AND balance >= 1
ORDER BY expiry_date ASC, id ASC LIMIT {(int)Math.Ceiling(daysNeeded)}", cancellationToken, ("@user_id", userId), ("@today", now.Date));
        foreach (var row in rows)
        {
            await Execute("UPDATE comp_off_leaves SET balance = 0, is_used = 1, leave_id = @leave_id, updated_at = @now WHERE id = @id", cancellationToken,
                ("@leave_id", AppendLeaveId(Str(row, "leave_id"), leaveId)), ("@now", now), ("@id", ULong(row, "id")));
        }
    }

    private async Task<object?> LeaveWithUsers(ulong leaveId, CancellationToken cancellationToken)
    {
        var row = (await QueryRows(@"SELECT l.*, u.name AS user_name, u.employee_codes AS user_employee_code, cb.name AS created_by_name
FROM leaves l
LEFT JOIN users u ON u.id = l.user_id
LEFT JOIN users cb ON cb.id = l.created_by
WHERE l.id = @id LIMIT 1", cancellationToken, ("@id", leaveId))).FirstOrDefault();
        return row is null ? null : LeaveObject(row);
    }

    private static object LeaveObject(Dictionary<string, object?> row) => new
    {
        id = ULong(row, "id"),
        active = Str(row, "active"),
        user_id = Obj(row, "user_id"),
        from_date = DateString(row, "from_date"),
        to_date = DateString(row, "to_date"),
        type = Str(row, "type"),
        bal_type = Str(row, "bal_type"),
        status = Obj(row, "status"),
        reason = Str(row, "reason"),
        remark_status = Str(row, "remark_status"),
        created_by = Obj(row, "created_by"),
        created_at = Obj(row, "created_at"),
        updated_at = Obj(row, "updated_at"),
        users = new { id = Obj(row, "user_id"), name = Str(row, "user_name"), employee_codes = Str(row, "user_employee_code") },
        createdbyname = new { id = Obj(row, "created_by"), name = Str(row, "created_by_name") }
    };

    private static Dictionary<string, string[]> ValidateLeave(LeaveForm request, bool requireUserId)
    {
        var errors = new Dictionary<string, string[]>();
        if (requireUserId && !request.UserId.HasValue) errors["user_id"] = ["The user id field is required."];
        if (!request.FromDate.HasValue) errors["from_date"] = ["The from date field is required."];
        if (!request.ToDate.HasValue) errors["to_date"] = ["The to date field is required."];
        if (request.FromDate.HasValue && request.ToDate.HasValue && request.ToDate.Value.Date < request.FromDate.Value.Date) errors["to_date"] = ["The to date must be a date after or equal to from date."];
        if (string.IsNullOrWhiteSpace(request.Type) || !ValidLeaveTypes.Contains(request.Type)) errors["type"] = ["The selected type is invalid."];
        if (string.IsNullOrWhiteSpace(request.BalType) || !ValidBalanceTypes.Contains(request.BalType)) errors["bal_type"] = ["The selected bal type is invalid."];
        if (request.Reason?.Length > 500) errors["reason"] = ["The reason may not be greater than 500 characters."];
        return errors;
    }

    private static decimal LeaveDays(DateTime from, DateTime to, string type)
    {
        var days = (decimal)(to.Date - from.Date).TotalDays + 1m;
        return type is "First Half Leave" or "Second Half Leave" ? 0.5m : days;
    }

    private static string BalanceColumn(string balanceType) => balanceType switch
    {
        "Casual Balance" => "casual_leave_balance",
        "Sick Balance" => "sick_leave_balance",
        "Earned Balance" => "earned_leave_balance",
        "Comp-off Balance" => "compb_off",
        _ => throw new InvalidOperationException("Invalid balance type")
    };

    private async Task<object?> QueryScalar(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        AttachTransaction(command);
        command.CommandText = sql;
        AddParameters(command, parameters);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task<long> QueryScalarLong(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var value = await QueryScalar(sql, cancellationToken, parameters);
        return value is null or DBNull ? 0 : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private async Task<decimal> QueryScalarDecimal(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var value = await QueryScalar(sql, cancellationToken, parameters);
        return value is null or DBNull ? 0 : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private async Task<int> Execute(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        AttachTransaction(command);
        command.CommandText = sql;
        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> QueryRows(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        AttachTransaction(command);
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

    private void AttachTransaction(IDbCommand command)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            command.Transaction = _dbContext.Database.CurrentTransaction.GetDbTransaction();
        }
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

    private static object? Obj(Dictionary<string, object?> row, string key) => row.TryGetValue(key, out var value) && value is not DBNull ? value : null;
    private static string Str(Dictionary<string, object?> row, string key) => Convert.ToString(Obj(row, key), CultureInfo.InvariantCulture) ?? string.Empty;
    private static ulong ULong(Dictionary<string, object?> row, string key) => Obj(row, key) is null ? 0 : Convert.ToUInt64(Obj(row, key), CultureInfo.InvariantCulture);
    private static decimal Dec(Dictionary<string, object?> row, string key) => Obj(row, key) is null ? 0 : Convert.ToDecimal(Obj(row, key), CultureInfo.InvariantCulture);
    private static string DateString(Dictionary<string, object?> row, string key) => Obj(row, key) is null ? string.Empty : Convert.ToDateTime(Obj(row, key), CultureInfo.InvariantCulture).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string AppendLeaveId(string existing, ulong leaveId) => string.IsNullOrWhiteSpace(existing) ? leaveId.ToString(CultureInfo.InvariantCulture) : $"{existing},{leaveId}";
    private static DateTime IndiaNow() => DateTime.UtcNow.AddHours(5).AddMinutes(30);
    private ulong CurrentUserId() => ulong.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new InvalidOperationException("Unauthenticated.");
    private ulong CurrentUserIdOr(ulong fallback) => ulong.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : fallback;

    private async Task<LeaveForm> ReadLeaveForm(CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var query in Request.Query) values[query.Key] = query.Value.ToString();
        if (Request.HasFormContentType)
        {
            foreach (var item in Request.Form) values[item.Key] = item.Value.ToString();
        }
        else if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                values[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }
        }

        return new LeaveForm
        {
            UserId = ULongValue(values, "user_id"),
            FromDate = DateValue(values, "from_date"),
            ToDate = DateValue(values, "to_date"),
            Type = Value(values, "type"),
            BalType = Value(values, "bal_type"),
            Reason = Value(values, "reason")
        };
    }

    private static string? Value(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static ulong? ULongValue(IReadOnlyDictionary<string, string> values, string key) =>
        ulong.TryParse(Value(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static DateTime? DateValue(IReadOnlyDictionary<string, string> values, string key)
    {
        var value = Value(values, key);
        if (string.IsNullOrWhiteSpace(value)) return null;
        var formats = new[] { "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy/MM/dd" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var exactDate)) return exactDate;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ? date : null;
    }

    public sealed class LeaveForm
    {
        [FromForm(Name = "user_id")] public ulong? UserId { get; init; }
        [FromForm(Name = "from_date")] public DateTime? FromDate { get; init; }
        [FromForm(Name = "to_date")] public DateTime? ToDate { get; init; }
        public string? Type { get; init; }
        [FromForm(Name = "bal_type")] public string? BalType { get; init; }
        public string? Reason { get; init; }
    }
}
