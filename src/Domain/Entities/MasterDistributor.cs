namespace Domain.Entities;

public sealed class MasterDistributor : BaseEntity
{
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string DistributorCode { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingDistrict { get; set; }
    public string? BillingState { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPincode { get; set; }
}
