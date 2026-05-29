namespace Application.DTOs.Customers;

public sealed class CustomerDto
{
    public ulong Id { get; set; }
    public string Active { get; set; } = "Y";
    public string Name { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? CustomerCode { get; set; }
    public string? ProfileImage { get; set; }
    public string? ShopImage { get; set; }
    public ulong? CustomerType { get; set; }
    public string CustomerTypeName { get; set; } = string.Empty;
    public string? SapCode { get; set; }
    public ulong? ParentId { get; set; }
    public string? ParentName { get; set; }
    public ulong? CountryId { get; set; }
    public string? CountryName { get; set; }
    public ulong? StateId { get; set; }
    public string? StateName { get; set; }
    public ulong? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public ulong? CityId { get; set; }
    public string? CityName { get; set; }
    public ulong? PincodeId { get; set; }
    public string? Pincode { get; set; }
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal TotalRegularPoints { get; set; }
    public decimal TotalBoosterPoints { get; set; }
    public decimal TotalRedeemPoints { get; set; }
    public decimal TotalRejectedPoints { get; set; }
    public decimal TotalBalancePoints { get; set; }
    public Dictionary<string, string?> CustomFields { get; set; } = [];
}

public sealed class CustomerRequestDto
{
    public string? Active { get; set; }
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Mobile { get; set; }
    public string? MobileNumber { get; set; }
    public string? ContactNumber { get; set; }
    public string? WhatsappNumber { get; set; }
    public string? AlternateMobile { get; set; }
    public string? Email { get; set; }
    public string? CustomerCode { get; set; }
    public string? ProfileImage { get; set; }
    public string? ShopImage { get; set; }
    public ulong? CustomerType { get; set; }
    public ulong? FirmType { get; set; }
    public ulong? ParentId { get; set; }
    public string? SapCode { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerPhone { get; set; }
    public Dictionary<string, string?>? CustomFields { get; set; }
}

public sealed class CustomerListFilterDto
{
    public ulong? CustomerType { get; set; }
    public string? Search { get; set; }
    public string? Active { get; set; }
    public ulong? StateId { get; set; }
    public ulong? CityId { get; set; }
    public ulong? PincodeId { get; set; }
}

public sealed class CustomerKycApprovalRequestDto
{
    public string? Remark { get; set; }
}
