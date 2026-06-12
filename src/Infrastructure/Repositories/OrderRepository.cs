using Application.DTOs.Orders;
using Application.DTOs.Users;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private const ulong DistributorCustomerType = 1;
    private const ulong RetailerCustomerType = 2;
    private const ulong InfluencerCustomerType = 3;
    private const int MaxRows = 50000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;

    public OrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<OrderDto>> GetOrdersAsync(OrderFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _dbContext.Orders.AsNoTracking();
        query = await ApplyVisibilityAsync(query, filter.ActorUserId, cancellationToken);
        query = ApplyFilters(query, filter);

        var rows = await (
            from order in query
            join executiveRow in _dbContext.Users.AsNoTracking() on order.ExecutiveId equals executiveRow.Id into executives
            from executive in executives.DefaultIfEmpty()
            join creatorRow in _dbContext.Users.AsNoTracking() on order.CreatedBy equals creatorRow.Id into creators
            from creator in creators.DefaultIfEmpty()
            join buyerRow in _dbContext.Customers.AsNoTracking() on order.BuyerId equals buyerRow.Id into buyers
            from buyer in buyers.DefaultIfEmpty()
            join sellerRow in _dbContext.Customers.AsNoTracking() on order.SellerId equals sellerRow.Id into sellers
            from seller in sellers.DefaultIfEmpty()
            orderby order.CreatedAt descending, order.Id descending
            select new OrderProjection(
                order.Id,
                order.Active,
                order.OrderDate,
                order.CompletedDate,
                order.OrderNo,
                order.BuyerId,
                buyer.Name,
                buyer.CustomFields,
                order.SellerId,
                seller.Name,
                seller.CustomFields,
                order.ExecutiveId,
                executive.Name,
                executive.BranchId,
                order.TotalQty,
                order.SubTotal,
                order.GrandTotal,
                order.StatusId,
                order.CreatedBy,
                creator.Name,
                order.CreatedAt,
                order.OrderType))
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            rows = rows.Where(row =>
                row.OrderNo.ToLowerInvariant().Contains(search)
                || (row.BuyerName ?? string.Empty).ToLowerInvariant().Contains(search)
                || (row.SellerName ?? string.Empty).ToLowerInvariant().Contains(search)
                || (row.ExecutiveName ?? string.Empty).ToLowerInvariant().Contains(search)
                || (row.CreatedByName ?? string.Empty).ToLowerInvariant().Contains(search)).ToList();
        }

        var branchIds = rows.Select(row => FirstBranchId(row.BranchId)).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var branches = await _dbContext.Branches.AsNoTracking()
            .Where(x => branchIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.BranchName, cancellationToken);

        return rows.Select(row => new OrderDto
        {
            Id = row.Id,
            Active = row.Active,
            OrderDate = row.OrderDate,
            CompletedDate = row.CompletedDate,
            OrderNo = row.OrderNo,
            BuyerId = row.BuyerId,
            BuyerName = CustomerDisplayName(row.BuyerName, row.BuyerFields, "shop_name", "owner_name"),
            SellerId = row.SellerId,
            SellerName = CustomerDisplayName(row.SellerName, row.SellerFields, "legal_name", "shop_name", "distributor_code"),
            ExecutiveId = row.ExecutiveId,
            ExecutiveName = row.ExecutiveName,
            BranchName = FirstBranchId(row.BranchId) is { } branchId && branches.TryGetValue(branchId, out var branchName) ? branchName : null,
            TotalQty = row.TotalQty,
            SubTotal = row.SubTotal,
            GrandTotal = row.GrandTotal,
            StatusId = row.StatusId,
            StatusName = StatusName(row.StatusId),
            CreatedBy = row.CreatedBy,
            CreatedByName = row.CreatedByName,
            CreatedAt = row.CreatedAt,
            OrderType = row.OrderType
        }).ToArray();
    }

    public async Task<OrderDto?> GetOrderAsync(ulong id, CancellationToken cancellationToken) =>
        (await GetOrdersAsync(new OrderFilterDto(), cancellationToken)).FirstOrDefault(x => x.Id == id);

    public async Task<IReadOnlyCollection<OrderExportRowDto>> GetOrderExportRowsAsync(OrderFilterDto filter, CancellationToken cancellationToken)
    {
        var orderQuery = _dbContext.Orders.AsNoTracking();
        orderQuery = await ApplyVisibilityAsync(orderQuery, filter.ActorUserId, cancellationToken);
        orderQuery = ApplyFilters(orderQuery, filter);

        var rows = await (
            from detail in _dbContext.OrderDetails.AsNoTracking()
            join order in orderQuery on detail.OrderId equals order.Id
            join employeeRow in _dbContext.Users.AsNoTracking() on order.ExecutiveId equals employeeRow.Id into employees
            from employee in employees.DefaultIfEmpty()
            join reportingRow in _dbContext.Users.AsNoTracking() on employee.ReportingId equals reportingRow.Id into reportings
            from reporting in reportings.DefaultIfEmpty()
            join designationRow in _dbContext.Designations.AsNoTracking() on employee.DesignationId equals designationRow.Id into designations
            from designation in designations.DefaultIfEmpty()
            join divisionRow in _dbContext.Divisions.AsNoTracking() on employee.DivisionId equals divisionRow.Id into divisions
            from division in divisions.DefaultIfEmpty()
            join buyerRow in _dbContext.Customers.AsNoTracking() on order.BuyerId equals buyerRow.Id into buyers
            from buyer in buyers.DefaultIfEmpty()
            join sellerRow in _dbContext.Customers.AsNoTracking() on order.SellerId equals sellerRow.Id into sellers
            from seller in sellers.DefaultIfEmpty()
            join productRow in _dbContext.Products.AsNoTracking() on detail.ProductId equals productRow.Id into products
            from product in products.DefaultIfEmpty()
            join segmentRow in _dbContext.ProductCategories.AsNoTracking() on product.CategoryId equals segmentRow.Id into segments
            from segment in segments.DefaultIfEmpty()
            join familyRow in _dbContext.ProductFamilies.AsNoTracking() on product.SubcategoryId equals familyRow.Id into families
            from family in families.DefaultIfEmpty()
            orderby order.CreatedAt descending, order.Id descending, detail.Id
            select new OrderExportProjection(
                order.OrderDate,
                order.OrderNo,
                employee.Name,
                reporting.Name,
                designation.DesignationName,
                employee.BranchId,
                buyer.Name,
                buyer.CustomFields,
                seller.Name,
                seller.CustomFields,
                product.PartNo,
                product.ProductName,
                detail.Quantity,
                detail.Price,
                detail.LineTotal,
                employee.EmployeeCodes,
                order.BuyerId,
                order.SellerId,
                order.OrderRemark,
                segment.CategoryName,
                family.SubcategoryName,
                detail.Id,
                division.DivisionName))
            .ToListAsync(cancellationToken);

        var branchIds = rows.Select(row => FirstBranchId(row.BranchId)).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var branches = await _dbContext.Branches.AsNoTracking()
            .Where(x => branchIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.BranchName, cancellationToken);

        return rows.Select(row => new OrderExportRowDto
        {
            OrderDate = row.OrderDate,
            OrderNo = row.OrderNo,
            EmployeeName = row.EmployeeName,
            ReportingManager = row.ReportingManager,
            Designation = row.Designation,
            Branch = FirstBranchId(row.BranchId) is { } branchId && branches.TryGetValue(branchId, out var branchName) ? branchName : null,
            RetailerName = CustomerDisplayName(row.RetailerName, row.RetailerFields, "shop_name", "owner_name"),
            DistributorName = CustomerDisplayName(row.DistributorName, row.DistributorFields, "legal_name", "shop_name"),
            DistributorCode = CustomerField(row.DistributorFields, "distributor_code"),
            ProductCode = row.ProductCode,
            ProductName = row.ProductName,
            Quantity = row.Quantity,
            Rate = row.Rate,
            TotalOrderValue = row.TotalOrderValue,
            EmployeeCode = row.EmployeeCode,
            RetailerId = row.RetailerId,
            DistributorId = row.DistributorId,
            OrderRemark = row.OrderRemark,
            Segment = row.Segment,
            Family = row.Family,
            DetailId = row.DetailId,
            Zone = row.Zone
        }).ToArray();
    }

    public async Task<IReadOnlyCollection<OrderDetailDto>> GetOrderDetailsAsync(ulong orderId, CancellationToken cancellationToken) =>
        await (
            from detail in _dbContext.OrderDetails.AsNoTracking()
            join productRow in _dbContext.Products.AsNoTracking() on detail.ProductId equals productRow.Id into products
            from product in products.DefaultIfEmpty()
            join familyRow in _dbContext.ProductFamilies.AsNoTracking() on detail.SubcategoryId equals familyRow.Id into families
            from family in families.DefaultIfEmpty()
            where detail.OrderId == orderId
            orderby detail.Id
            select new OrderDetailDto
            {
                Id = detail.Id,
                ProductId = detail.ProductId,
                ProductName = product.ProductName,
                SubcategoryId = detail.SubcategoryId,
                SubcategoryName = family.SubcategoryName,
                Quantity = detail.Quantity,
                Price = detail.Price,
                Gst = detail.Gst,
                TaxAmount = detail.TaxAmount,
                LineTotal = detail.LineTotal
            })
            .ToListAsync(cancellationToken);

    public async Task<OrderOptionsDto> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken)
    {
        var visibleUserIds = await ReportingVisibility.GetVisibleUserIdsAsync(_dbContext, actorUserId, cancellationToken);
        return new OrderOptionsDto
        {
            Users = await ReportingVisibility.InternalUsersQuery(_dbContext, _dbContext.Users.AsNoTracking())
                .Where(x => x.Active == "Y" && visibleUserIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new OptionDto { Id = x.Id, Name = x.Name })
                .ToListAsync(cancellationToken),
            Divisions = await _dbContext.Divisions.AsNoTracking()
                .Where(x => x.Active == "Y")
                .OrderBy(x => x.DivisionName)
                .Select(x => new OptionDto { Id = x.Id, Name = x.DivisionName })
                .ToListAsync(cancellationToken),
            Designations = await _dbContext.Designations.AsNoTracking()
                .Where(x => x.Active == "Y")
                .OrderBy(x => x.DesignationName)
                .Select(x => new OptionDto { Id = x.Id, Name = x.DesignationName })
                .ToListAsync(cancellationToken),
            Retailers = await CustomerOptionsAsync([RetailerCustomerType, InfluencerCustomerType], ["shop_name", "owner_name"], cancellationToken),
            Distributors = await CustomerOptionsAsync([DistributorCustomerType], ["legal_name", "shop_name", "distributor_code"], cancellationToken),
            Families = await _dbContext.ProductFamilies.AsNoTracking()
                .Where(x => x.Active == "Y")
                .OrderBy(x => x.SubcategoryName)
                .Select(x => new OptionDto { Id = x.Id, Name = x.SubcategoryName })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<IReadOnlyCollection<OrderProductOptionDto>> GetProductsByFamilyAsync(ulong familyId, CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products.AsNoTracking()
            .Where(x => x.Active == "Y" && x.SubcategoryId == familyId)
            .OrderBy(x => x.ProductName)
            .Select(x => new OrderProductOptionDto
            {
                Id = x.Id,
                Name = x.ProductName,
                ProductCode = x.ProductCode,
                HsnSac = x.HsnSac,
                Price = _dbContext.ProductDetails.AsNoTracking()
                    .Where(detail => detail.ProductId == x.Id)
                    .OrderBy(detail => detail.Id)
                    .Select(detail => detail.Mrp ?? detail.Price ?? detail.SellingPrice)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return products;
    }

    public Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken) =>
        _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Order?> GetOrderEntityAsync(ulong id, CancellationToken cancellationToken) =>
        _dbContext.Orders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddOrderAsync(Order order, CancellationToken cancellationToken) =>
        await _dbContext.Orders.AddAsync(order, cancellationToken);

    public async Task AddOrderDetailsAsync(IReadOnlyCollection<OrderDetail> details, CancellationToken cancellationToken) =>
        await _dbContext.OrderDetails.AddRangeAsync(details, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private async Task<IQueryable<Order>> ApplyVisibilityAsync(IQueryable<Order> query, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var visibleUserIds = await ReportingVisibility.GetVisibleUserIdsAsync(_dbContext, actorUserId, cancellationToken);
        return query.Where(x =>
            (x.ExecutiveId.HasValue && visibleUserIds.Contains(x.ExecutiveId.Value))
            || (x.CreatedBy.HasValue && visibleUserIds.Contains(x.CreatedBy.Value)));
    }

    private IQueryable<Order> ApplyFilters(IQueryable<Order> query, OrderFilterDto filter)
    {
        if (filter.RetailersId.HasValue) query = query.Where(x => x.BuyerId == filter.RetailersId.Value);
        if (filter.DistributorId.HasValue) query = query.Where(x => x.SellerId == filter.DistributorId.Value);
        if (filter.UserId.HasValue) query = query.Where(x => x.CreatedBy == filter.UserId.Value);
        if (filter.PendingStatus.HasValue) query = filter.PendingStatus.Value == 0 ? query.Where(x => x.StatusId == null) : query.Where(x => x.StatusId == (ulong)filter.PendingStatus.Value);
        if (filter.StartDate.HasValue) query = query.Where(x => x.OrderDate >= filter.StartDate.Value.Date);
        if (filter.EndDate.HasValue) query = query.Where(x => x.OrderDate <= filter.EndDate.Value.Date);

        if (filter.DivisionId.HasValue)
        {
            var userIds = _dbContext.Users.AsNoTracking().Where(x => x.DivisionId == filter.DivisionId.Value).Select(x => x.Id);
            query = query.Where(x => x.ExecutiveId.HasValue && userIds.Contains(x.ExecutiveId.Value));
        }

        if (filter.DesignationIds.Count > 0)
        {
            var userIds = _dbContext.Users.AsNoTracking().Where(x => x.DesignationId.HasValue && filter.DesignationIds.Contains(x.DesignationId.Value)).Select(x => x.Id);
            query = query.Where(x => x.CreatedBy.HasValue && userIds.Contains(x.CreatedBy.Value));
        }

        return query;
    }

    private async Task<IReadOnlyCollection<OptionDto>> CustomerOptionsAsync(IReadOnlyCollection<ulong> customerTypes, string[] preferredFields, CancellationToken cancellationToken)
    {
        var customers = await _dbContext.Customers.AsNoTracking()
            .Where(x => x.Active == "Y" && x.CustomerType.HasValue && customerTypes.Contains(x.CustomerType.Value))
            .OrderBy(x => x.Name)
            .Take(MaxRows)
            .Select(x => new { x.Id, x.Name, x.CustomFields })
            .ToListAsync(cancellationToken);

        return customers
            .Select(customer => new OptionDto
            {
                Id = customer.Id,
                Name = CustomerDisplayName(customer.Name, customer.CustomFields, preferredFields)
            })
            .Where(option => !string.IsNullOrWhiteSpace(option.Name))
            .OrderBy(option => option.Name)
            .ToArray();
    }

    private static string CustomerDisplayName(string? fallback, string? customFields, params string[] preferredFields)
    {
        var fields = DeserializeFields(customFields);
        foreach (var field in preferredFields)
        {
            if (fields.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return fallback?.Trim() ?? string.Empty;
    }

    private static string? CustomerField(string? customFields, string field)
    {
        var fields = DeserializeFields(customFields);
        return fields.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
    }

    private static Dictionary<string, string?> DeserializeFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static ulong? FirstBranchId(string? branchIds)
    {
        if (string.IsNullOrWhiteSpace(branchIds)) return null;
        var first = branchIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return ulong.TryParse(first, out var id) ? id : null;
    }

    private static string StatusName(ulong? statusId) =>
        statusId switch
        {
            1 => "Dispatch",
            2 => "Partial Dispatch",
            _ => "Pending"
        };

    private sealed record OrderProjection(
        ulong Id,
        string Active,
        DateTime? OrderDate,
        DateTime? CompletedDate,
        string OrderNo,
        ulong? BuyerId,
        string? BuyerName,
        string? BuyerFields,
        ulong? SellerId,
        string? SellerName,
        string? SellerFields,
        ulong? ExecutiveId,
        string? ExecutiveName,
        string? BranchId,
        decimal TotalQty,
        decimal SubTotal,
        decimal GrandTotal,
        ulong? StatusId,
        ulong? CreatedBy,
        string? CreatedByName,
        DateTime? CreatedAt,
        string? OrderType);

    private sealed record OrderExportProjection(
        DateTime? OrderDate,
        string OrderNo,
        string? EmployeeName,
        string? ReportingManager,
        string? Designation,
        string? BranchId,
        string? RetailerName,
        string? RetailerFields,
        string? DistributorName,
        string? DistributorFields,
        string? ProductCode,
        string? ProductName,
        long Quantity,
        decimal Rate,
        decimal TotalOrderValue,
        string? EmployeeCode,
        ulong? RetailerId,
        ulong? DistributorId,
        string? OrderRemark,
        string? Segment,
        string? Family,
        ulong DetailId,
        string? Zone);
}
