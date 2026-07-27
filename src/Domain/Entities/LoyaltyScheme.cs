namespace Domain.Entities;

public sealed class LoyaltyScheme : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string SchemeName { get; set; } = string.Empty;
    public string SchemeCode { get; set; } = string.Empty;
    public string? SchemeDescription { get; set; }
    public string SchemeTag { get; set; } = "Regular";
    public string CustomerType { get; set; } = string.Empty;
    public string AreaScope { get; set; } = "All";
    public string AreaValues { get; set; } = "[]";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string SchemeType { get; set; } = "Invoice";
    public string BasedOn { get; set; } = "Value";
    public bool RedemptionEnabled { get; set; }
    public string Status { get; set; } = "Draft";
    public string? BrochurePath { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public ulong? SubmittedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public ulong? ApprovedBy { get; set; }
    public string? ApprovalRemark { get; set; }
    public DateTime? RejectedAt { get; set; }
    public ulong? RejectedBy { get; set; }
    public string? RejectionRemark { get; set; }
    public ulong? CreatedBy { get; set; }
    public ulong? UpdatedBy { get; set; }
    public ICollection<LoyaltySchemeSlab> Slabs { get; set; } = [];
}
