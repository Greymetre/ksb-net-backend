namespace Application.DTOs.NewInvoices;

public sealed class NewInvoiceDto
{
    public ulong Id { get; set; }
    public ulong SecondaryCustomerId { get; set; }
    public string RetailerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? CityName { get; set; }
    public string? ZoneName { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Points { get; set; }
    public ulong? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public string? SchemeCode { get; set; }
    public string? SchemeTag { get; set; }
    public string? SchemeBasedOn { get; set; }
    public decimal? SchemeRewardValue { get; set; }
    public decimal SchemePoints { get; set; }
    public string? SchemeHintMessage { get; set; }
    public decimal RegularWalletPoints { get; set; }
    public decimal BoosterWalletPoints { get; set; }
    public string? Attachment { get; set; }
    public int ApprovalStatus { get; set; }
    public string ApprovalStatusLabel { get; set; } = string.Empty;
    public string? ApprovalRemark { get; set; }
    public ulong CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyCollection<NewInvoiceApprovalLogDto> ApprovalLogs { get; set; } = [];
}

public sealed class NewInvoiceApprovalLogDto
{
    public ulong Id { get; set; }
    public DateTime? LogDate { get; set; }
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? EmployeeCode { get; set; }
    public string StatusType { get; set; } = string.Empty;
    public int? FromStatus { get; set; }
    public int? ToStatus { get; set; }
    public string? Remark { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class NewInvoiceRequestDto
{
    public ulong SecondaryCustomerId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Points { get; set; }
    public string? Attachment { get; set; }
}

public sealed class NewInvoiceFilterDto
{
    public string? RetailerSearch { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? ApprovalStatus { get; set; }
    public ulong? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
}

public sealed class NewInvoiceSummaryDto
{
    public int TotalInvoices { get; set; }
    public int TotalRetailers { get; set; }
    public int ApprovedSs { get; set; }
    public int ApprovedSales { get; set; }
    public int ApprovedHo { get; set; }
    public int Pending { get; set; }
    public int Rejected { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class RetailerOptionDto
{
    public ulong Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? CityName { get; set; }
}

public sealed class NewInvoiceApprovalRequestDto
{
    public string? Remark { get; set; }
}
