using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.DTOs.Users;

public sealed class UserExportFiltersDto
{
    public string? UserType { get; set; }
    public string? Active { get; set; }
    public ulong? DivisionId { get; set; }
    public string? BranchId { get; set; }
    public ulong? DepartmentId { get; set; }
}

public sealed class UserListFiltersDto
{
    public string? Search { get; set; }
    public string? UserType { get; set; }
    public string? Active { get; set; }
    public ulong? DivisionId { get; set; }
    public string? BranchId { get; set; }
    public ulong? DepartmentId { get; set; }
}

public sealed class UserRoleDto
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UserDto
{
    public ulong Id { get; set; }
    public string Active { get; set; } = "Y";
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? EmployeeCodes { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? BranchId { get; set; }
    public string? BranchNames { get; set; }
    public ulong? DesignationId { get; set; }
    public string? DesignationName { get; set; }
    public ulong? DivisionId { get; set; }
    public string? DivisionName { get; set; }
    public ulong? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public ulong? ReportingId { get; set; }
    public string? ReportingName { get; set; }
    public string? Location { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Payroll { get; set; }
    public ulong? WarehouseId { get; set; }
    public string? SalesType { get; set; }
    public string? ShowAttandanceReport { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public string? PasswordString { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyCollection<ulong> CityIds { get; set; } = [];
    public IReadOnlyCollection<UserRoleDto> Roles { get; set; } = [];
}

public sealed class UserRequestDto
{
    public string? Active { get; set; }
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmployeeCodes { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? BranchId { get; set; }
    public IReadOnlyCollection<ulong>? BranchIds { get; set; }
    public ulong? DesignationId { get; set; }
    public ulong? DivisionId { get; set; }
    public ulong? DepartmentId { get; set; }
    public ulong? ReportingId { get; set; }
    public string? Location { get; set; }
    public string? BaseLocationCoordinates { get; set; }
    public string? Payroll { get; set; }
    public ulong? WarehouseId { get; set; }
    public string? SalesType { get; set; }
    public string? ShowAttandanceReport { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public IReadOnlyCollection<ulong>? Roles { get; set; }
    public IReadOnlyCollection<ulong>? CityIds { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }

    public string? StringAlias(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Extra?.TryGetValue(key, out var value) != true) continue;
            var text = ReadString(value);
            if (text is not null) return text;
        }

        return null;
    }

    public ulong? ULongAlias(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Extra?.TryGetValue(key, out var value) != true) continue;
            var number = ReadULong(value);
            if (number.HasValue) return number;
        }

        return null;
    }

    public IReadOnlyCollection<ulong>? ULongListAlias(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Extra?.TryGetValue(key, out var value) != true) continue;
            var list = ReadULongList(value);
            if (list is not null) return list;
        }

        return null;
    }

    public DateTime? DateAlias(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Extra?.TryGetValue(key, out var value) != true) continue;
            var date = ReadDate(value);
            if (date.HasValue) return date;
        }

        return null;
    }

    private static string? ReadString(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "1",
            JsonValueKind.False => "0",
            _ => null
        };

    private static ulong? ReadULong(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String && ulong.TryParse(value.GetString(), out number)) return number;
        return null;
    }

    private static IReadOnlyCollection<ulong>? ReadULongList(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray()
                .Select(ReadULong)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();
        }

        var text = ReadString(value);
        if (string.IsNullOrWhiteSpace(text)) return [];
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => ulong.TryParse(part, out var parsed) ? parsed : (ulong?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();
    }

    private static DateTime? ReadDate(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (string.IsNullOrWhiteSpace(text)) return null;
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
                ? parsed
                : null;
        }

        return value.ValueKind == JsonValueKind.Null ? null : null;
    }
}

public sealed class UserOptionsDto
{
    public IReadOnlyCollection<OptionDto> Roles { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Branches { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Designations { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Divisions { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Departments { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Reportings { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Cities { get; set; } = [];
}

public sealed class OptionDto
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UserExcelRowDto
{
    public ulong Id { get; set; }
    public string? EmployeeCodes { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DesignationName { get; set; }
    public string? RoleNames { get; set; }
    public string? BranchNames { get; set; }
    public string? Location { get; set; }
    public string? DepartmentName { get; set; }
    public string? DivisionName { get; set; }
    public string? ReportingName { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfConfirmation { get; set; }
    public DateTime? DateOfLeaving { get; set; }
    public string? Grade { get; set; }
    public string? DesignationCode { get; set; }
    public string? EmployeeSuperCode { get; set; }
    public string? BaseLocationCoordinates { get; set; }
    public ulong? ReportingId { get; set; }
    public string? RoleIds { get; set; }
    public string? Payroll { get; set; }
    public ulong? DesignationId { get; set; }
    public string? BranchId { get; set; }
    public ulong? DivisionId { get; set; }
    public ulong? DepartmentId { get; set; }
    public string? AttendanceSummaryReport { get; set; }
}

public sealed class UserImportResultDto
{
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int FailedRows { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; } = [];
}
