using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class MobileOrderFlowController : ControllerBase
{
    private const ulong DistributorCustomerType = 1;
    private const ulong RetailerCustomerType = 2;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _db;

    public MobileOrderFlowController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("getCategoryList")]
    public async Task<IActionResult> GetCategoryList(CancellationToken cancellationToken)
    {
        var rows = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.Active == "Y")
            .OrderBy(x => x.Ranking)
            .ThenBy(x => x.CategoryName)
            .Select(x => new { x.Id, x.CategoryName })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            status = true,
            message = "Data retrieved successfully",
            data = rows.Select(x => new { id = x.Id, category_name = x.CategoryName }).ToList()
        });
    }

    [HttpGet("getSubCategoryList")]
    public async Task<IActionResult> GetSubCategoryList([FromQuery(Name = "category_id")] ulong? categoryId, CancellationToken cancellationToken)
    {
        var query = _db.ProductFamilies.AsNoTracking().Where(x => x.Active == "Y");
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);

        var rows = await query
            .OrderBy(x => x.Ranking)
            .ThenBy(x => x.SubcategoryName)
            .Select(x => new { x.Id, x.SubcategoryName, x.CategoryId })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            status = true,
            message = "Data retrieved successfully",
            data = rows.Select(x => new { id = x.Id, subcategory_name = x.SubcategoryName, category_id = x.CategoryId }).ToList()
        });
    }

    [HttpGet("getProductList")]
    public async Task<IActionResult> GetProductList([FromQuery(Name = "subcategory_id")] ulong? subcategoryId, CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking().Where(x => x.Active == "Y");
        if (subcategoryId.HasValue) query = query.Where(x => x.SubcategoryId == subcategoryId.Value);

        var rows = await (
            from product in query
            join familyRow in _db.ProductFamilies.AsNoTracking() on product.SubcategoryId equals familyRow.Id into families
            from family in families.DefaultIfEmpty()
            join categoryRow in _db.ProductCategories.AsNoTracking() on product.CategoryId equals categoryRow.Id into categories
            from category in categories.DefaultIfEmpty()
            orderby product.Ranking, product.ProductName
            select new
            {
                product.Id,
                product.ProductName,
                product.SubcategoryId,
                SubcategoryName = family.SubcategoryName,
                product.CategoryId,
                CategoryName = category.CategoryName
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            status = true,
            message = "Data retrieved successfully",
            data = rows.Select(x => new
            {
                id = x.Id,
                product_name = x.ProductName,
                subcategory_id = x.SubcategoryId,
                subcategory_name = x.SubcategoryName,
                category_id = x.CategoryId,
                category_name = x.CategoryName
            }).ToList()
        });
    }

    [HttpGet("getProductDetails")]
    public async Task<IActionResult> GetProductDetails([FromQuery(Name = "product_id")] ulong? productId, CancellationToken cancellationToken)
    {
        if (!productId.HasValue || productId.Value == 0) return BadRequest(Message(false, "product_id is required"));

        var row = await (
            from product in _db.Products.AsNoTracking()
            join familyRow in _db.ProductFamilies.AsNoTracking() on product.SubcategoryId equals familyRow.Id into families
            from family in families.DefaultIfEmpty()
            join categoryRow in _db.ProductCategories.AsNoTracking() on product.CategoryId equals categoryRow.Id into categories
            from category in categories.DefaultIfEmpty()
            where product.Id == productId.Value && product.Active == "Y"
            select new
            {
                product.Id,
                product.ProductName,
                product.ProductCode,
                product.SubcategoryId,
                SubcategoryName = family.SubcategoryName,
                product.CategoryId,
                CategoryName = category.CategoryName,
                Mrp = _db.ProductDetails.AsNoTracking()
                    .Where(detail => detail.ProductId == product.Id && detail.Active == "Y")
                    .OrderBy(detail => detail.Id)
                    .Select(detail => detail.Mrp ?? detail.Price ?? detail.SellingPrice ?? 0)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) return NotFound(Message(false, "Product not found"));

        return Ok(new
        {
            status = true,
            message = "Data retrieved successfully",
            data = new
            {
                id = row.Id,
                product_name = row.ProductName,
                product_code = row.ProductCode,
                subcategory_id = row.SubcategoryId,
                subcategory_name = row.SubcategoryName,
                category_id = row.CategoryId,
                category_name = row.CategoryName,
                mrp = row.Mrp
            }
        });
    }

    [HttpGet("order/secondary-customers")]
    public async Task<IActionResult> OrderSecondaryCustomers(CancellationToken cancellationToken)
    {
        var rows = await ScopedCustomerRows(RetailerCustomerType, cancellationToken);

        return Ok(new
        {
            status = true,
            message = "Data retrieved successfully",
            data = rows.Select(x => new
            {
                id = ULong(x, "id"),
                shop_name = CustomerDisplayName(Str(x, "name"), Str(x, "custom_fields"), "shop_name", "owner_name")
            }).ToList()
        });
    }

    [HttpGet("order/distributors")]
    public async Task<IActionResult> OrderDistributors(CancellationToken cancellationToken)
    {
        var rows = await ScopedCustomerRows(DistributorCustomerType, cancellationToken);

        return Ok(new
        {
            status = true,
            message = "Data retrieved successfully",
            data = rows.Select(x => new
            {
                id = ULong(x, "id"),
                distributor_code = FirstNonEmpty(Field(Str(x, "custom_fields"), "distributor_code"), Str(x, "customer_code")),
                legal_name = CustomerDisplayName(Str(x, "name"), Str(x, "custom_fields"), "legal_name", "trade_name", "shop_name")
            }).ToList()
        });
    }

    [HttpPost("insertOrder")]
    public async Task<IActionResult> InsertOrder([FromBody] MobileOrderRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateOrderRequest(request);
        if (validation is not null) return BadRequest(Message(false, validation));

        var userId = CurrentUserId();
        if (userId == 0) return Unauthorized(Message(false, "Unauthenticated."));

        var seller = await _db.Customers.FirstOrDefaultAsync(x => x.Id == request.SellerId && x.Active == "Y" && x.CustomerType == DistributorCustomerType, cancellationToken);
        if (seller is null) return BadRequest(Message(false, "seller_id is invalid"));

        if (request.BuyerId.HasValue)
        {
            var buyerExists = await _db.Customers.AnyAsync(x => x.Id == request.BuyerId.Value && x.Active == "Y" && x.CustomerType == RetailerCustomerType, cancellationToken);
            if (!buyerExists) return BadRequest(Message(false, "buyer_id is invalid"));
        }

        var productIds = request.OrderDetail.Select(x => x.ProductId!.Value).Distinct().ToArray();
        var productMap = await _db.Products
            .Where(x => productIds.Contains(x.Id) && x.Active == "Y")
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (productMap.Count != productIds.Length) return BadRequest(Message(false, "One or more products are invalid"));

        var productDetailIds = request.OrderDetail.Select(x => x.ProductDetailId!.Value).Distinct().ToArray();
        var validDetailPairs = await _db.ProductDetails.AsNoTracking()
            .Where(x => productDetailIds.Contains(x.Id) && x.ProductId.HasValue && productIds.Contains(x.ProductId.Value) && x.Active == "Y")
            .Select(x => new { x.Id, x.ProductId })
            .ToListAsync(cancellationToken);
        var validDetailSet = validDetailPairs.Select(x => $"{x.Id}:{x.ProductId}").ToHashSet(StringComparer.Ordinal);
        if (request.OrderDetail.Any(x => !validDetailSet.Contains($"{x.ProductDetailId!.Value}:{x.ProductId!.Value}")))
        {
            return BadRequest(Message(false, "One or more product_detail_id values are invalid"));
        }

        var executionStrategy = _db.Database.CreateExecutionStrategy();
        var result = await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var totalQty = request.OrderDetail.Sum(x => x.Quantity!.Value);
            var grandTotal = request.OrderDetail.Sum(CalculatedLineTotal);
            var firstProduct = productMap[request.OrderDetail.First().ProductId!.Value];

            var order = new Order
            {
                Active = "Y",
                BuyerId = request.BuyerId,
                SellerId = request.SellerId,
                ExecutiveId = userId,
                TotalQty = (long)totalQty,
                ShippedQty = 0,
                OrderDate = now.Date,
                OrderTaking = "MobileApp",
                OrderType = request.BuyerId.HasValue ? "SECONDARY_CUSTOMER" : "DISTRIBUTOR",
                ProductCatId = firstProduct.CategoryId,
                OrderRemark = string.IsNullOrWhiteSpace(request.Remark) ? "NA" : request.Remark.Trim(),
                CreatedBy = userId,
                UpdatedBy = userId,
                SubTotal = grandTotal,
                TotalGst = 0,
                GrandTotal = grandTotal,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _db.Orders.AddAsync(order, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            order.OrderNo = $"{now:yyyy}-{order.SellerId ?? 0}-{order.BuyerId ?? 0}-{order.Id}";

            var details = request.OrderDetail.Select(item =>
            {
                var product = productMap[item.ProductId!.Value];
                return new OrderDetail
                {
                    Active = "Y",
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    ProductDetailId = item.ProductDetailId,
                    Quantity = (long)item.Quantity!.Value,
                    ShippedQty = 0,
                    Price = item.Price!.Value,
                    LineTotal = CalculatedLineTotal(item),
                    EbdAmount = item.EbdAmount,
                    CategoryId = product.CategoryId,
                    SubcategoryId = product.SubcategoryId,
                    StatusId = null,
                    CreatedAt = now,
                    UpdatedAt = now
                };
            }).ToArray();

            await _db.OrderDetails.AddRangeAsync(details, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new { order_id = order.Id, orderno = order.OrderNo };
        });

        return Ok(new
        {
            status = true,
            message = "Order placed successfully",
            data = result
        });
    }

    [HttpGet("getOrderList")]
    public async Task<IActionResult> GetOrderList(
        [FromQuery] int? page,
        [FromQuery(Name = "pageSqueryize")] int? pageSqueryize,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery(Name = "startdate")] DateTime? startDate,
        [FromQuery(Name = "enddate")] DateTime? endDate,
        [FromQuery(Name = "user_id")] ulong? userId,
        CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserId();
        var visibleUserIds = await VisibleUserIds(currentUserId, cancellationToken);
        var selectedUserId = userId.HasValue && visibleUserIds.Contains(userId.Value) ? userId.Value : (ulong?)null;
        var pageNumber = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSqueryize ?? perPage ?? 10, 1, 100);

        var userScope = selectedUserId.HasValue
            ? $"(o.created_by = {selectedUserId.Value} OR o.executive_id = {selectedUserId.Value})"
            : $@"(
    o.created_by IN ({string.Join(',', visibleUserIds)})
    OR o.executive_id IN ({string.Join(',', visibleUserIds)})
)";
        var where = new List<string> { "o.deleted_at IS NULL", userScope };
        var dateExpr = "COALESCE(o.order_date, o.created_at)";
        if (startDate.HasValue)
        {
            where.Add($"{dateExpr} >= '{startDate.Value.Date:yyyy-MM-dd}'");
        }
        if (endDate.HasValue)
        {
            where.Add($"{dateExpr} < '{endDate.Value.Date.AddDays(1):yyyy-MM-dd}'");
        }
        var whereSql = string.Join("\nAND ", where);

        var totalRow = (await QueryRows($@"SELECT COUNT(*) AS total
FROM orders o
WHERE {whereSql}", cancellationToken)).FirstOrDefault();
        var total = totalRow is null ? 0 : (int)Long(totalRow, "total");
        var pageCount = total == 0 ? 1 : (int)Math.Ceiling(total / (double)size);
        var effectivePage = Math.Min(pageNumber, pageCount);
        var offset = (effectivePage - 1) * size;
        var orders = await QueryRows($@"SELECT
    o.id,
    o.orderno,
    {dateExpr} AS display_order_date,
    o.buyer_id,
    o.seller_id,
    o.total_qty,
    o.grand_total,
    o.order_remark,
    buyer.name AS buyer_name,
    buyer.custom_fields AS buyer_custom_fields,
    seller.name AS seller_name,
    seller.custom_fields AS seller_custom_fields,
    creator.name AS creator_name,
    executive.name AS executive_name,
    COALESCE(SUM(od.quantity), 0) AS detail_qty,
    COALESCE(SUM(od.line_total), 0) AS detail_total
FROM orders o
LEFT JOIN customers buyer ON buyer.id = o.buyer_id
LEFT JOIN customers seller ON seller.id = o.seller_id
LEFT JOIN users creator ON creator.id = o.created_by
LEFT JOIN users executive ON executive.id = o.executive_id
LEFT JOIN order_details od ON od.order_id = o.id
WHERE {whereSql}
GROUP BY o.id, o.orderno, display_order_date, o.buyer_id, o.seller_id, o.total_qty, o.grand_total, o.order_remark,
    buyer.name, buyer.custom_fields, seller.name, seller.custom_fields, creator.name, executive.name
ORDER BY display_order_date DESC, o.id DESC
LIMIT {size} OFFSET {offset}", cancellationToken);
        var users = await _db.Users.AsNoTracking()
            .Where(x => x.Active == "Y" && visibleUserIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new { id = x.Id, name = x.Name })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            status = true,
            message = orders.Count > 0 ? "Data retrieved successfully." : "No Record Found.",
            data = orders.Select(order => new
            {
                order_id = ULong(order, "id"),
                orderno = Str(order, "orderno"),
                order_date = DateOnlyString(DateTimeValue(order, "display_order_date")),
                buyer_name = CustomerDisplayName(Str(order, "buyer_name"), Str(order, "buyer_custom_fields"), "shop_name", "owner_name"),
                seller_name = CustomerDisplayName(Str(order, "seller_name"), Str(order, "seller_custom_fields"), "legal_name", "trade_name", "shop_name"),
                total_qty = Long(order, "detail_qty") > 0 ? Long(order, "detail_qty") : Long(order, "total_qty"),
                grand_total = DecimalValue(order, "detail_total") > 0 ? DecimalValue(order, "detail_total") : DecimalValue(order, "grand_total"),
                order_remark = Str(order, "order_remark"),
                creatd_by = FirstNonEmpty(Str(order, "creator_name"), Str(order, "executive_name")) ?? string.Empty
            }).ToList(),
            users,
            page_count = pageCount
        });
    }

    [HttpGet("getOrderDetails")]
    public async Task<IActionResult> GetOrderDetails([FromQuery(Name = "order_id")] ulong? orderId, CancellationToken cancellationToken)
    {
        if (!orderId.HasValue || orderId.Value == 0) return BadRequest(Message(false, "order_id is required"));

        var currentUserId = CurrentUserId();
        var visibleUserIds = await VisibleUserIds(currentUserId, cancellationToken);
        var scope = $@"(
    o.created_by IN ({string.Join(',', visibleUserIds)})
    OR o.executive_id IN ({string.Join(',', visibleUserIds)})
)";
        var order = (await QueryRows($@"SELECT
    o.id,
    o.orderno,
    COALESCE(o.order_date, o.created_at) AS display_order_date,
    o.buyer_id,
    o.seller_id,
    o.order_remark,
    buyer.name AS buyer_name,
    buyer.mobile AS buyer_mobile,
    buyer.custom_fields AS buyer_custom_fields,
    seller.name AS seller_name,
    seller.mobile AS seller_mobile,
    seller.custom_fields AS seller_custom_fields,
    COALESCE(o.created_by, o.executive_id) AS display_user_id,
    COALESCE(creator.name, executive.name) AS display_user_name
FROM orders o
LEFT JOIN customers buyer ON buyer.id = o.buyer_id
LEFT JOIN customers seller ON seller.id = o.seller_id
LEFT JOIN users creator ON creator.id = o.created_by
LEFT JOIN users executive ON executive.id = o.executive_id
WHERE o.id = {orderId.Value}
AND o.deleted_at IS NULL
AND {scope}
LIMIT 1", cancellationToken)).FirstOrDefault();
        if (order is null) return NotFound(Message(false, "Order not found"));

        var details = await QueryRows($@"SELECT
    od.id,
    od.product_id,
    p.product_name,
    od.quantity,
    od.price,
    od.line_total
FROM order_details od
LEFT JOIN products p ON p.id = od.product_id
WHERE od.order_id = {orderId.Value}
ORDER BY od.id ASC", cancellationToken);

        var buyerId = ULong(order, "buyer_id");
        var sellerId = ULong(order, "seller_id");
        var addresses = await AddressMap(new[] { buyerId, sellerId }.Where(x => x > 0).ToArray(), cancellationToken);
        var displayUserId = ULong(order, "display_user_id");

        return Ok(new
        {
            status = true,
            message = "Data retrieved successfully.",
            data = new
            {
                order_id = ULong(order, "id"),
                orderno = Str(order, "orderno"),
                order_date = DateOnlyString(DateTimeValue(order, "display_order_date")),
                buyer_name = CustomerDisplayName(Str(order, "buyer_name"), Str(order, "buyer_custom_fields"), "shop_name", "owner_name"),
                buyer_address = buyerId > 0 && addresses.TryGetValue(buyerId, out var buyerAddress) ? buyerAddress : string.Empty,
                seller_name = CustomerDisplayName(Str(order, "seller_name"), Str(order, "seller_custom_fields"), "legal_name", "trade_name", "shop_name"),
                seller_address = sellerId > 0 && addresses.TryGetValue(sellerId, out var sellerAddress) ? sellerAddress : string.Empty,
                order_remark = Str(order, "order_remark"),
                grand_total = details.Sum(x => DecimalValue(x, "line_total")),
                total_qty = details.Sum(x => Long(x, "quantity")),
                buyers = buyerId == 0 ? null : new { id = buyerId, mobile_number = Str(order, "buyer_mobile") },
                seller = sellerId == 0 ? null : new { id = sellerId, mobile = Str(order, "seller_mobile") },
                createdbyname = displayUserId == 0 ? null : new { id = displayUserId, name = Str(order, "display_user_name") },
                orderdetails = details.Select(x => new
                {
                    id = ULong(x, "id"),
                    product_id = ULong(x, "product_id"),
                    product_name = Str(x, "product_name"),
                    quantity = Long(x, "quantity"),
                    price = DecimalValue(x, "price"),
                    line_total = DecimalValue(x, "line_total")
                }).ToList()
            }
        });
    }

    private static string? ValidateOrderRequest(MobileOrderRequest request)
    {
        if (request is null) return "Invalid request payload";
        if (request.SellerId == 0) return "seller_id is required";
        if (request.OrderDetail.Count == 0) return "orderdetail is required";

        for (var index = 0; index < request.OrderDetail.Count; index++)
        {
            var item = request.OrderDetail.ElementAt(index);
            var label = $"orderdetail[{index}]";
            if (!item.ProductId.HasValue || item.ProductId.Value == 0) return $"{label}.product_id is required";
            if (!item.ProductDetailId.HasValue || item.ProductDetailId.Value == 0) return $"{label}.product_detail_id is required";
            if (!item.Quantity.HasValue || item.Quantity.Value <= 0) return $"{label}.quantity must be greater than 0";
            if (!item.Price.HasValue || item.Price.Value <= 0) return $"{label}.price must be greater than 0";
            if (!item.LineTotal.HasValue || item.LineTotal.Value <= 0) return $"{label}.line_total is required";
        }

        return null;
    }

    private async Task<IReadOnlyCollection<ulong>> VisibleUserIds(ulong userId, CancellationToken cancellationToken)
    {
        var users = await _db.Users.AsNoTracking()
            .Where(x => !x.IsDeleted && x.DeletedAt == null)
            .Select(x => new { x.Id, x.ReportingId })
            .ToListAsync(cancellationToken);
        var visible = new HashSet<ulong> { userId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var user in users)
            {
                if (user.ReportingId.HasValue && visible.Contains(user.ReportingId.Value) && visible.Add(user.Id)) changed = true;
            }
        }

        return visible;
    }

    private async Task<Dictionary<ulong, Customer>> CustomerMap(IReadOnlyCollection<ulong> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return [];
        return await _db.Customers.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> ScopedCustomerRows(ulong customerType, CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserId();
        var allAccess = await CanSeeAllCustomers(currentUserId, cancellationToken);
        IReadOnlyCollection<ulong> visibleUserIds = allAccess ? Array.Empty<ulong>() : await VisibleUserIds(currentUserId, cancellationToken);
        if (!allAccess && visibleUserIds.Count == 0) return [];

        var scope = allAccess
            ? string.Empty
            : $@"AND (
    c.created_by IN ({string.Join(',', visibleUserIds)})
    OR c.executive_id IN ({string.Join(',', visibleUserIds)})
    OR EXISTS (
        SELECT 1 FROM employee_details ed
        WHERE ed.customer_id = c.id
        AND ed.deleted_at IS NULL
        AND ed.user_id IN ({string.Join(',', visibleUserIds)})
    )
)";

        return await QueryRows($@"SELECT DISTINCT c.id, c.name, c.customer_code, c.custom_fields
FROM customers c
WHERE c.active = 'Y'
AND c.deleted_at IS NULL
AND c.customertype = {customerType}
{scope}
ORDER BY c.name ASC", cancellationToken);
    }

    private async Task<bool> CanSeeAllCustomers(ulong userId, CancellationToken cancellationToken)
    {
        if (userId == 0) return false;
        var rows = await QueryRows($@"SELECT r.name
FROM model_has_roles m
INNER JOIN roles r ON r.id = m.role_id
WHERE m.model_id = {userId}
AND r.name IN ('superadmin', 'subAdmin', 'Sub_Admin', 'Admin', 'HO')", cancellationToken);
        return rows.Count > 0;
    }

    private async Task<Dictionary<ulong, string>> AddressMap(IReadOnlyCollection<ulong> customerIds, CancellationToken cancellationToken)
    {
        if (customerIds.Count == 0) return [];
        var idList = string.Join(',', customerIds.Distinct());
        var rows = await QueryRows($@"SELECT customer_id, CONCAT_WS(', ', NULLIF(address1, ''), NULLIF(address2, '')) AS address
FROM addresses
WHERE deleted_at IS NULL AND customer_id IN ({idList})", cancellationToken);
        return rows.GroupBy(x => ULong(x, "customer_id")).ToDictionary(x => x.Key, x => Str(x.First(), "address"));
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> QueryRows(string sql, CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    private ulong CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : 0;
    }

    private static object Message(bool status, string message) => new { status, message };

    private static decimal CalculatedLineTotal(MobileOrderDetailRequest item)
    {
        if (item.LineTotal.HasValue && item.LineTotal.Value > 0) return item.LineTotal.Value;
        return item.Quantity!.Value * item.Price!.Value;
    }

    private static string OrderUserName(Order order, IReadOnlyDictionary<ulong, string> users)
    {
        if (order.CreatedBy.HasValue && users.TryGetValue(order.CreatedBy.Value, out var createdByName)) return createdByName;
        if (order.ExecutiveId.HasValue && users.TryGetValue(order.ExecutiveId.Value, out var executiveName)) return executiveName;
        return string.Empty;
    }

    private static string CustomerDisplayName(string fallback, string? customFields, params string[] preferredFields)
    {
        foreach (var field in preferredFields)
        {
            var value = Field(customFields, field);
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }

        return fallback.Trim();
    }

    private static string? Field(string? customFields, string key)
    {
        var fields = DeserializeFields(customFields);
        return fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
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

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string DateOnlyString(DateTime? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string OrderDateString(Order order) =>
        DateOnlyString(order.OrderDate ?? order.CreatedAt);

    private static object? Obj(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not DBNull ? value : null;

    private static string Str(IReadOnlyDictionary<string, object?> row, string key) =>
        Convert.ToString(Obj(row, key), CultureInfo.InvariantCulture) ?? string.Empty;

    private static ulong ULong(IReadOnlyDictionary<string, object?> row, string key) =>
        Obj(row, key) is null ? 0 : Convert.ToUInt64(Obj(row, key), CultureInfo.InvariantCulture);

    private static long Long(IReadOnlyDictionary<string, object?> row, string key) =>
        Obj(row, key) is null ? 0 : Convert.ToInt64(Obj(row, key), CultureInfo.InvariantCulture);

    private static decimal DecimalValue(IReadOnlyDictionary<string, object?> row, string key) =>
        Obj(row, key) is null ? 0 : Convert.ToDecimal(Obj(row, key), CultureInfo.InvariantCulture);

    private static DateTime? DateTimeValue(IReadOnlyDictionary<string, object?> row, string key)
    {
        var value = Obj(row, key);
        if (value is null) return null;
        if (value is DateTime dateTime) return dateTime;
        return DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }
}

public sealed class MobileOrderRequest
{
    [JsonPropertyName("buyer_id")]
    public ulong? BuyerId { get; set; }

    [JsonPropertyName("seller_id")]
    public ulong SellerId { get; set; }

    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    [JsonPropertyName("orderdetail")]
    public IReadOnlyCollection<MobileOrderDetailRequest> OrderDetail { get; set; } = [];
}

public sealed class MobileOrderDetailRequest
{
    [JsonPropertyName("product_id")]
    public ulong? ProductId { get; set; }

    [JsonPropertyName("product_detail_id")]
    public ulong? ProductDetailId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("ebd_amount")]
    public decimal? EbdAmount { get; set; }

    [JsonPropertyName("line_total")]
    public decimal? LineTotal { get; set; }
}
