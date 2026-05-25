namespace Application.DTOs.Auth;

public sealed class CustomerSignupRequestDto
{
    public string? Name { get; set; }
    public string? ShopName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Mobile { get; set; }
    public string? Address { get; set; }
    public ulong? CustomerType { get; set; }
    public string? Pincode { get; set; }
    public string? FcmToken { get; set; }
    public string? UniqueId { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceName { get; set; }
    public string? AppVersion { get; set; }
}
