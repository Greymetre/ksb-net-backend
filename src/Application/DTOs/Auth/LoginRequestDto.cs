namespace Application.DTOs.Auth;

public sealed class LoginRequestDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? UniqueId { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceName { get; set; }
    public string? AppVersion { get; set; }
    public string? FcmToken { get; set; }
    public DateTime? LoginAt { get; set; }
}
