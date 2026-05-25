using System.Text.Json.Serialization;

namespace Application.DTOs.Auth;

public sealed class LoginUserInfoDto
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("profile_image")]
    public string? ProfileImage { get; set; }

    [JsonPropertyName("access_token")]
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
}
