using System.Text.Json.Serialization;

namespace Application.DTOs.Auth;

public sealed class LoginUserInfoDto
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("profile_image")]
    public string? ProfileImage { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("region_id")]
    public ulong? RegionId { get; set; }

    [JsonPropertyName("division_id")]
    public ulong? DivisionId { get; set; }

    [JsonPropertyName("dividion_id")]
    public ulong? DividionId { get; set; }

    [JsonPropertyName("payroll_id")]
    public string? PayrollId { get; set; }

    [JsonPropertyName("employee_codes")]
    public string? EmployeeCodes { get; set; }

    [JsonPropertyName("access_token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("roles")]
    public IReadOnlyCollection<ulong> Roles { get; set; } = [];

    [JsonPropertyName("permissions")]
    public IReadOnlyCollection<string> Permissions { get; set; } = [];

    [JsonPropertyName("user_type")]
    public IReadOnlyCollection<string> UserType { get; set; } = [];

    [JsonPropertyName("leave_balance")]
    public decimal LeaveBalance { get; set; }

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "users";

    [JsonPropertyName("todayBeatSchedule")]
    public bool TodayBeatSchedule { get; set; }

    [JsonPropertyName("beatUser")]
    public bool BeatUser { get; set; }
}
