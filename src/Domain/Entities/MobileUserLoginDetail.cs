namespace Domain.Entities;

public sealed class MobileUserLoginDetail : BaseEntity
{
    public ulong? UserId { get; set; }
    public ulong? CustomerId { get; set; }
    public string? AppVersion { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? UniqueId { get; set; }
    public DateTime? FirstLoginDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string LoginStatus { get; set; } = "0";
    public string MultiLogin { get; set; } = "0";
    public string? App { get; set; }
    public DateTime? LoginAt { get; set; }
}
