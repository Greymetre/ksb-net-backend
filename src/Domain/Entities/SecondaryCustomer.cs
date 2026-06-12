namespace Domain.Entities;

public sealed class SecondaryCustomer : BaseEntity
{
    public string Type { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? Active { get; set; }
    public ulong? CountryId { get; set; }
    public ulong? StateId { get; set; }
    public ulong? DistrictId { get; set; }
    public ulong? CityId { get; set; }
    public ulong? PincodeId { get; set; }
    public string? DistributorName { get; set; }
    public string? AgriDistributor { get; set; }
    public long? CreatedBy { get; set; }
    public string? EmployeeId { get; set; }
}
