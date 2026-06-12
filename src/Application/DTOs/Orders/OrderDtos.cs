using Application.DTOs.Users;

namespace Application.DTOs.Orders;

public sealed class OrderFilterDto
{
    public ulong? RetailersId { get; set; }
    public ulong? DistributorId { get; set; }
    public ulong? UserId { get; set; }
    public ulong? DivisionId { get; set; }
    public IReadOnlyCollection<ulong> DesignationIds { get; set; } = [];
    public int? PendingStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Search { get; set; }
    public ulong? ActorUserId { get; set; }
}

public sealed class OrderDto
{
    public ulong Id { get; set; }
    public string Active { get; set; } = "Y";
    public DateTime? OrderDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public ulong? BuyerId { get; set; }
    public string? BuyerName { get; set; }
    public ulong? SellerId { get; set; }
    public string? SellerName { get; set; }
    public ulong? ExecutiveId { get; set; }
    public string? ExecutiveName { get; set; }
    public string? BranchName { get; set; }
    public decimal TotalQty { get; set; }
    public decimal SubTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public ulong? StatusId { get; set; }
    public string StatusName { get; set; } = "Pending";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? OrderType { get; set; }
}

public sealed class OrderDetailDto
{
    public ulong Id { get; set; }
    public ulong? ProductId { get; set; }
    public string? ProductName { get; set; }
    public ulong? SubcategoryId { get; set; }
    public string? SubcategoryName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Gst { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class OrderRequestDto
{
    public DateTime? OrderDate { get; set; }
    public ulong? ExecutiveId { get; set; }
    public string? Type { get; set; }
    public ulong? BuyerId { get; set; }
    public ulong? SellerId { get; set; }
    public decimal? GrandTotal { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? TotalQty { get; set; }
    public decimal? TotalGst { get; set; }
    public string? OrderRemark { get; set; }
    public IReadOnlyCollection<OrderDetailRequestDto> OrderDetail { get; set; } = [];
}

public sealed class OrderDetailRequestDto
{
    public ulong? ProductId { get; set; }
    public ulong? ProductDetail { get; set; }
    public ulong? SubcategoryId { get; set; }
    public ulong? CategoryId { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Mrp { get; set; }
    public decimal? Gst { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? LineTotal { get; set; }
}

public sealed class OrderOptionsDto
{
    public IReadOnlyCollection<OptionDto> Users { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Divisions { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Designations { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Retailers { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Distributors { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Families { get; set; } = [];
}

public sealed class OrderProductOptionDto
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public long HsnSac { get; set; }
    public decimal? Price { get; set; }
}

public sealed class OrderExportRowDto
{
    public DateTime? OrderDate { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public string? ReportingManager { get; set; }
    public string? Designation { get; set; }
    public string? Branch { get; set; }
    public string? RetailerName { get; set; }
    public string? DistributorName { get; set; }
    public string? DistributorCode { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal TotalOrderValue { get; set; }
    public string? EmployeeCode { get; set; }
    public ulong? RetailerId { get; set; }
    public ulong? DistributorId { get; set; }
    public string? OrderRemark { get; set; }
    public string? Segment { get; set; }
    public string? Family { get; set; }
    public ulong DetailId { get; set; }
    public string? Zone { get; set; }
}
