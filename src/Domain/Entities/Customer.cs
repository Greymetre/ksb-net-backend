namespace Domain.Entities;

public sealed class Customer : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
    public string NotificationId { get; set; } = string.Empty;
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string ProfileImage { get; set; } = string.Empty;
    public string? ShopImage { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public ulong? StatusId { get; set; }
    public ulong? CustomerType { get; set; }
    public ulong? RegionId { get; set; }
    public ulong? FirmType { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
    public ulong? ExecutiveId { get; set; }
    public ulong? BeatScheduleId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public string ManagerPhone { get; set; } = string.Empty;
    public string? Otp { get; set; }
    public string? CustomFields { get; set; }
    public bool? SameAddress { get; set; }
    public ulong? ParentId { get; set; }
    public string? SapCode { get; set; }
}
