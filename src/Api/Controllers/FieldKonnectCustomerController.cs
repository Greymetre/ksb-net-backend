using System.Data;
using System.Globalization;
using System.Security.Claims;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class FieldKonnectCustomerController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public FieldKonnectCustomerController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getRetailers")]
    public async Task<IActionResult> GetRetailers([FromQuery] CustomerListQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var userId = CurrentUserId();
            var isAdmin = await IsAdminUser(userId, cancellationToken, includeHrAndHo: true);
            var visibleUsers = await VisibleUserIds(userId, cancellationToken);
            var customerTypes = await CustomerTypes(cancellationToken);
            var cityCustomerIds = await CustomerIdsByCities(query.CityId, cancellationToken);
            var branchUserIds = await UserIdsByBranches(query.BranchId, cancellationToken);

            var rows = await QueryCustomerRows(new CustomerRowFilter
            {
                Search = query.Search,
                CustomerType = query.Customertype,
                ActiveOnly = true,
                RetailerOnly = !query.Customertype.HasValue,
                VisibleUserIds = isAdmin ? null : visibleUsers,
                CityCustomerIds = cityCustomerIds,
                BranchUserIds = branchUserIds,
                PageSize = query.PageSize ?? 10000,
                OrderByName = true
            }, cancellationToken);

            var data = rows.Select(row => new
            {
                customer_id = ULong(row, "id"),
                name = CustomerNameWithSap(row),
                mobile = Str(row, "mobile"),
                email = Str(row, "email"),
                profile_image = Str(row, "profile_image"),
                address1 = Str(row, "address1"),
                address2 = Str(row, "address2"),
                latitude = Str(row, "latitude"),
                longitude = Str(row, "longitude"),
                grade = Str(row, "grade"),
                visit_status = Str(row, "visit_status"),
                customer_type = Str(row, "customertype_name"),
                distance = string.Empty
            }).ToList();

            return Ok(new { status = "success", message = "Data retrieved successfully.", customerTypes, data });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getDistributors")]
    public async Task<IActionResult> GetDistributors([FromQuery] CustomerListQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var userId = CurrentUserId();
            var isAdmin = await IsAdminUser(userId, cancellationToken, includeHrAndHo: false);
            var visibleUsers = await VisibleUserIds(userId, cancellationToken);
            var rows = await QueryCustomerRows(new CustomerRowFilter
            {
                Search = query.Search,
                ActiveOnly = false,
                DistributorOnly = true,
                VisibleUserIds = isAdmin ? null : visibleUsers,
                PageSize = query.PageSize,
                OrderByName = true
            }, cancellationToken);

            var data = rows.Select(row => new
            {
                customer_id = ULong(row, "id"),
                name = CustomerNameWithSap(row),
                first_name = Str(row, "first_name"),
                last_name = Str(row, "last_name"),
                mobile = Str(row, "mobile"),
                email = Str(row, "email"),
                profile_image = Str(row, "profile_image"),
                customer_code = Str(row, "customer_code")
            }).ToList();

            if (data.Count == 0) return Ok(new { status = "error", message = "No Record Found.", data });
            return Ok(new { status = "success", message = "Data retrieved successfully.", data });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getCustomerList")]
    public async Task<IActionResult> GetCustomerList([FromQuery] CustomerListQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var visibleUsers = await VisibleUserIds(CurrentUserId(), cancellationToken);
            var rows = await QueryCustomerRows(new CustomerRowFilter
            {
                ExecutiveUserIds = visibleUsers,
                PageSize = query.PageSize,
                Latest = true
            }, cancellationToken);

            var data = rows.Select(row => new
            {
                customer_id = ULong(row, "id"),
                name = Str(row, "name"),
                mobile = Str(row, "mobile"),
                email = Str(row, "email"),
                profile_image = Str(row, "profile_image"),
                customer_code = Str(row, "customer_code"),
                address1 = Str(row, "address1"),
                address2 = Str(row, "address2"),
                latitude = Str(row, "latitude"),
                longitude = Str(row, "longitude"),
                grade = Str(row, "grade"),
                visit_status = Str(row, "visit_status")
            }).ToList();

            if (data.Count == 0) return Ok(new { status = "error", message = "No Record Found.", data });
            return Ok(new { status = "success", message = "Data retrieved successfully.", data });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [AcceptVerbs("GET", "POST")]
    [Route("getCustomerInfo")]
    public async Task<IActionResult> GetCustomerInfo([FromQuery(Name = "customer_id")] ulong? customerId, [FromQuery] string? fromDate, [FromQuery] string? toDate, CancellationToken cancellationToken)
    {
        try
        {
            if (!customerId.HasValue)
            {
                return BadRequest(new { status = "error", message = new[] { "The customer id field is required." } });
            }

            var customer = (await QueryCustomerRows(new CustomerRowFilter { CustomerId = customerId, PageSize = 1 }, cancellationToken)).FirstOrDefault();
            if (customer is null)
            {
                return BadRequest(new { status = "error", message = new[] { "The selected customer id is invalid." } });
            }

            var from = ParseDate(fromDate);
            var to = ParseDate(toDate);
            var orderFilter = from.HasValue && to.HasValue
                ? "buyer_id = @customer_id AND order_date BETWEEN @from_date AND @to_date"
                : "buyer_id = @customer_id AND order_date >= @month_start";
            var monthStart = new DateTime(IndiaNow().Year, IndiaNow().Month, 1);
            var orders = await QueryRows($"SELECT id, sub_total, total_qty, order_date FROM orders WHERE {orderFilter} AND deleted_at IS NULL", cancellationToken,
                ("@customer_id", customerId.Value),
                ("@from_date", from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("@to_date", to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("@month_start", monthStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

            var orderIds = orders.Select(x => ULong(x, "id")).Where(x => x > 0).ToArray();
            var sumQuantity = orderIds.Length == 0 ? 0 : await QueryScalarLong($"SELECT COALESCE(SUM(quantity), 0) FROM order_details WHERE order_id IN ({string.Join(',', orderIds)})", cancellationToken);
            var sales = await QueryRows("SELECT grand_total FROM sales WHERE buyer_id = @customer_id", cancellationToken, ("@customer_id", customerId.Value));
            var checkins = await QueryRows("SELECT checkin_date, checkin_time FROM check_in WHERE customer_id = @customer_id AND deleted_at IS NULL ORDER BY id DESC LIMIT 10", cancellationToken, ("@customer_id", customerId.Value));
            var lastOrderDate = await QueryScalar("SELECT order_date FROM orders WHERE buyer_id = @customer_id AND deleted_at IS NULL ORDER BY id DESC LIMIT 1", cancellationToken, ("@customer_id", customerId.Value));
            var beat = (await QueryRows("SELECT b.id, b.beat_name FROM beats b INNER JOIN beat_customers bc ON bc.beat_id = b.id WHERE bc.customer_id = @customer_id LIMIT 1", cancellationToken, ("@customer_id", customerId.Value))).FirstOrDefault();
            var parents = await QueryRows("SELECT pd.parent_id, p.name FROM parent_details pd LEFT JOIN customers p ON p.id = pd.parent_id WHERE pd.customer_id = @customer_id AND pd.deleted_at IS NULL", cancellationToken, ("@customer_id", customerId.Value));
            var activities = await QueryRows("SELECT userid, time, description, type FROM user_activities WHERE customerid = @customer_id AND deleted_at IS NULL ORDER BY id DESC LIMIT 5", cancellationToken, ("@customer_id", customerId.Value));
            var tasks = await QueryRows("SELECT user_id, title, descriptions, datetime FROM tasks WHERE completed = 0 AND customer_id = @customer_id ORDER BY datetime ASC LIMIT 5", cancellationToken, ("@customer_id", customerId.Value));
            var wallet = (await QueryRows("SELECT COALESCE(SUM(points), 0) AS total_points, COALESCE(SUM(quantity), 0) AS total_coupon_scan FROM wallets WHERE customer_id = @customer_id AND transaction_type = 'Cr' AND deleted_at IS NULL", cancellationToken, ("@customer_id", customerId.Value))).FirstOrDefault();

            var totalAmount = await QueryScalarDecimal("SELECT COALESCE(SUM(grand_total), 0) FROM sales WHERE buyer_id = @customer_id", cancellationToken, ("@customer_id", customerId.Value));
            var totalPaid = await QueryScalarDecimal("SELECT COALESCE(SUM(paid_amount), 0) FROM sales WHERE buyer_id = @customer_id", cancellationToken, ("@customer_id", customerId.Value));
            var totalOrderValue = orders.Sum(x => Dec(x, "sub_total"));
            var totalOrderQty = orders.Sum(x => Dec(x, "total_qty"));
            var visitsInfo = await QueryRows("SELECT id, customer_id, description, report_title, visit_image, user_id, created_at FROM visit_reports WHERE customer_id = @customer_id ORDER BY id DESC", cancellationToken, ("@customer_id", customerId.Value));

            var data = new Dictionary<string, object?>
            {
                ["id"] = customerId.Value,
                ["name"] = Str(customer, "name"),
                ["first_name"] = Str(customer, "first_name"),
                ["last_name"] = Str(customer, "last_name"),
                ["mobile"] = Str(customer, "mobile"),
                ["email"] = Str(customer, "email"),
                ["profile_image"] = Str(customer, "profile_image"),
                ["customer_code"] = Str(customer, "customer_code"),
                ["customertype"] = Obj(customer, "customertype"),
                ["contact_number"] = Str(customer, "contact_number"),
                ["latitude"] = Str(customer, "latitude"),
                ["longitude"] = Str(customer, "longitude"),
                ["sap_code"] = Str(customer, "sap_code"),
                ["customeraddress"] = AddressObject(customer),
                ["customerdetails"] = CustomerDetailsObject(customer),
                ["customertypes"] = new { customertype_name = Str(customer, "customertype_name"), type_name = Str(customer, "type_name") },
                ["activity"] = visitsInfo.Select(VisitActivity).ToList(),
                ["parent_id"] = string.Join(',', parents.Select(x => Str(x, "parent_id")).Where(x => !string.IsNullOrWhiteSpace(x))),
                ["parent_name"] = string.Join(',', parents.Select(x => Str(x, "name")).Where(x => !string.IsNullOrWhiteSpace(x))),
                ["beat_name"] = beat is null ? string.Empty : Str(beat, "beat_name"),
                ["beat_id"] = beat is null ? null : Obj(beat, "id"),
                ["outstanding"] = totalAmount - totalPaid,
                ["total_order_value"] = totalOrderValue,
                ["total_order_quantity"] = (int)sumQuantity,
                ["avg_order_value"] = totalOrderValue >= 1 && orders.Count > 0 ? $"{(totalOrderValue / orders.Count).ToString("0.0", CultureInfo.InvariantCulture)} %" : string.Empty,
                ["avg_order_quantity"] = totalOrderQty >= 1 && orders.Count > 0 ? $"{(totalOrderQty / orders.Count).ToString("0.0", CultureInfo.InvariantCulture)} %" : string.Empty,
                ["total_sales_value"] = sales.Sum(x => Dec(x, "grand_total")),
                ["last_order_date"] = FormatDate(lastOrderDate),
                ["last_visited"] = visitsInfo.Count > 0 ? FormatDate(Obj(visitsInfo[0], "created_at")) : string.Empty,
                ["visited"] = checkins,
                ["activities"] = activities,
                ["tasks"] = tasks,
                ["total_points"] = wallet is null ? 0 : Obj(wallet, "total_points"),
                ["total_coupon_scan"] = wallet is null ? 0 : Obj(wallet, "total_coupon_scan"),
                ["visitsinfo"] = visitsInfo
            };

            return Ok(new { status = "success", message = "Data retrieved successfully.", data, customers = visitsInfo });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpPost("updateCustomerLocation")]
    public async Task<IActionResult> UpdateCustomerLocation([FromForm] CustomerMutationForm request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.CustomerId.HasValue) return BadRequest(new { status = "error", message = new[] { "The customer id field is required." } });
            var rows = await Execute("UPDATE customers SET latitude = @latitude, longitude = @longitude, updated_at = @updated_at WHERE id = @id", cancellationToken,
                ("@latitude", request.Latitude),
                ("@longitude", request.Longitude),
                ("@updated_at", IndiaNow()),
                ("@id", request.CustomerId.Value));
            if (rows > 0) return Ok(new { status = "success", message = "Data updated successfully." });
            return Ok(new { status = "error", message = "No Record Updated." });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpPost("customers-active")]
    public async Task<IActionResult> Active([FromForm] CustomerMutationForm request, CancellationToken cancellationToken)
    {
        if (!request.Id.HasValue) return BadRequest(new { status = "error", message = new[] { "The id field is required." } });
        var active = request.Active == "Y" ? "Y" : "N";
        var rows = await Execute("UPDATE customers SET active = @active, updated_at = @updated_at WHERE id = @id", cancellationToken,
            ("@active", active),
            ("@updated_at", IndiaNow()),
            ("@id", request.Id.Value));
        if (rows > 0) return Ok(new { status = "success", message = $"Customer {(active == "N" ? "Inactive" : "Active")} Successfully!" });
        return Ok(new { status = "error", message = "Error in Status Update" });
    }

    [HttpPost("storeCustomer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> StoreCustomer([FromForm] CustomerMutationForm request, CancellationToken cancellationToken)
    {
        try
        {
            var mobile = NormalizeMobile(request.Mobile);
            if (string.IsNullOrWhiteSpace(mobile)) return BadRequest(new { status = "error", message = new[] { "The mobile field is required." } });
            if (await QueryScalarLong("SELECT COUNT(*) FROM customers WHERE mobile = @mobile AND deleted_at IS NULL", cancellationToken, ("@mobile", mobile)) > 0)
            {
                return BadRequest(new { status = "error", message = "Mobile Number Already Exist" });
            }

            var customerId = await InsertCustomer(request, mobile, cancellationToken);
            await UpsertAddress(customerId, request, cancellationToken);
            await UpsertCustomerDetails(customerId, request, cancellationToken);
            await UpsertBeatCustomer(customerId, request.BeatId, cancellationToken);
            await Execute("INSERT INTO employee_details (active, customer_id, user_id, created_by, created_at, updated_at) VALUES ('Y', @customer_id, @user_id, @user_id, @now, @now)", cancellationToken,
                ("@customer_id", customerId),
                ("@user_id", CurrentUserId()),
                ("@now", IndiaNow()));

            return Ok(new { status = "success", message = "Data inserted successfully." });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = exception.Message });
        }
    }

    [HttpPost("updateCustomerProfile")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateCustomerProfile([FromForm] CustomerMutationForm request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.CustomerId.HasValue) return Ok(new { status = 201, msg = "The customer id field is required." });
            if (string.IsNullOrWhiteSpace(request.Name)) return Ok(new { status = 201, msg = "The name field is required." });

            var (firstName, lastName) = SplitName(request.FullName, request.FirstName, request.LastName);
            await Execute(@"UPDATE customers SET name = @name, first_name = @first_name, last_name = @last_name, email = @email, mobile = @mobile,
latitude = @latitude, longitude = @longitude, gender = @gender, firmtype = @firmtype, contact_number = @contact_number, updated_at = @now WHERE id = @id", cancellationToken,
                ("@name", request.Name),
                ("@first_name", firstName),
                ("@last_name", lastName),
                ("@email", request.Email),
                ("@mobile", request.Mobile),
                ("@latitude", request.Latitude),
                ("@longitude", request.Longitude),
                ("@gender", request.Gender ?? string.Empty),
                ("@firmtype", request.FirmType),
                ("@contact_number", request.ContactNumber),
                ("@now", IndiaNow()),
                ("@id", request.CustomerId.Value));
            await UpsertAddress(request.CustomerId.Value, request, cancellationToken);
            await UpsertCustomerDetails(request.CustomerId.Value, request, cancellationToken);
            await UpsertBeatCustomer(request.CustomerId.Value, request.BeatId, cancellationToken);
            await SyncParentDetails(request.CustomerId.Value, request.ParentId, cancellationToken);
            return Ok(new { status = 200, msg = "Customer Update Successfully", data = 1 });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = 201, msg = exception.Message });
        }
    }

    private async Task<ulong> InsertCustomer(CustomerMutationForm request, string mobile, CancellationToken cancellationToken)
    {
        var (firstName, lastName) = SplitName(request.FullName, request.FirstName, request.LastName);
        var name = FirstNonEmpty(request.Name, request.FullName, $"{firstName} {lastName}".Trim()) ?? string.Empty;
        await Execute(@"INSERT INTO customers (active, name, first_name, last_name, mobile, email, password, notification_id, latitude, longitude, device_type, gender,
customer_code, profile_image, status_id, customertype, firmtype, created_by, manager_name, manager_phone, contact_number, created_at, updated_at)
VALUES ('Y', @name, @first_name, @last_name, @mobile, @email, '', @notification_id, @latitude, @longitude, @device_type, @gender,
@customer_code, '', @status_id, @customertype, @firmtype, @created_by, @manager_name, @manager_phone, @contact_number, @now, @now)", cancellationToken,
            ("@name", Capitalize(name)),
            ("@first_name", Capitalize(firstName)),
            ("@last_name", Capitalize(lastName)),
            ("@mobile", mobile),
            ("@email", request.Email),
            ("@notification_id", request.NotificationId ?? string.Empty),
            ("@latitude", request.Latitude),
            ("@longitude", request.Longitude),
            ("@device_type", Capitalize(request.DeviceType)),
            ("@gender", Capitalize(request.Gender)),
            ("@customer_code", request.CustomerCode ?? string.Empty),
            ("@status_id", request.StatusId ?? 2),
            ("@customertype", request.Customertype ?? 2),
            ("@firmtype", request.FirmType),
            ("@created_by", CurrentUserId()),
            ("@manager_name", request.ManagerName ?? string.Empty),
            ("@manager_phone", request.ManagerPhone ?? string.Empty),
            ("@contact_number", request.ContactNumber ?? string.Empty),
            ("@now", IndiaNow()));
        return Convert.ToUInt64(await QueryScalar("SELECT LAST_INSERT_ID()", cancellationToken));
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> QueryCustomerRows(CustomerRowFilter filter, CancellationToken cancellationToken)
    {
        var where = new List<string> { "c.deleted_at IS NULL" };
        var parameters = new List<(string, object?)>();
        if (filter.CustomerId.HasValue) { where.Add("c.id = @customer_id"); parameters.Add(("@customer_id", filter.CustomerId.Value)); }
        if (filter.ActiveOnly) where.Add("c.active = 'Y'");
        if (filter.CustomerType.HasValue) { where.Add("c.customertype = @customertype"); parameters.Add(("@customertype", filter.CustomerType.Value)); }
        if (filter.DistributorOnly) where.Add("(ct.type_name IN ('distributor', 'Dealer') OR c.customertype IN (1,3)) AND c.sap_code IS NOT NULL");
        if (filter.RetailerOnly) where.Add("(ct.type_name IS NULL OR ct.type_name NOT IN ('distributor', 'Dealer'))");
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Add("(c.name LIKE @search OR c.first_name LIKE @search OR c.last_name LIKE @search OR c.email LIKE @search OR c.mobile LIKE @search)");
            parameters.Add(("@search", $"%{filter.Search}%"));
        }
        AddAssignedCustomerVisibility(where, filter.VisibleUserIds);
        AddIn(where, "c.id", filter.CityCustomerIds, "city_customer");
        AddAssignedCustomerVisibility(where, filter.BranchUserIds);
        AddIn(where, "c.executive_id", filter.ExecutiveUserIds, "executive");

        var sql = $@"SELECT DISTINCT c.id, c.name, c.first_name, c.last_name, c.mobile, c.email, c.profile_image, c.customer_code, c.latitude, c.longitude,
c.customertype, c.sap_code, c.contact_number, c.firmtype, a.id AS address_id, a.address1, a.address2, a.landmark, a.locality, a.country_id, a.state_id,
a.district_id, a.city_id, a.pincode_id, a.zipcode, cd.gstin_no, cd.pan_no, cd.aadhar_no, cd.otherid_no, cd.shop_image, cd.visiting_card, cd.grade,
cd.visit_status, ct.customertype_name, ct.type_name
FROM customers c
LEFT JOIN addresses a ON a.customer_id = c.id AND a.deleted_at IS NULL
LEFT JOIN customer_details cd ON cd.customer_id = c.id AND cd.deleted_at IS NULL
LEFT JOIN customer_types ct ON ct.id = c.customertype AND ct.deleted_at IS NULL
LEFT JOIN employee_details ed ON ed.customer_id = c.id AND ed.deleted_at IS NULL
WHERE {string.Join(" AND ", where)}
ORDER BY {(filter.Latest ? "c.id DESC" : filter.OrderByName ? "c.name ASC" : "c.id DESC")}
{(filter.PageSize.HasValue ? $"LIMIT {Math.Clamp(filter.PageSize.Value, 1, 50000)}" : string.Empty)}";
        return await QueryRows(sql, cancellationToken, parameters.ToArray());
    }

    private async Task<IReadOnlyList<ulong>> VisibleUserIds(ulong userId, CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users.IgnoreQueryFilters().Select(x => new { x.Id, x.ReportingId }).ToListAsync(cancellationToken);
        var visible = new HashSet<ulong> { userId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var user in users.Where(x => x.ReportingId.HasValue && visible.Contains(x.ReportingId.Value)))
            {
                if (visible.Add(user.Id)) changed = true;
            }
        }
        return visible.ToArray();
    }

    private async Task<bool> IsAdminUser(ulong userId, CancellationToken cancellationToken, bool includeHrAndHo)
    {
        var roles = await _dbContext.ModelHasRoles
            .Where(x => x.ModelId == userId)
            .Join(_dbContext.Roles, model => model.RoleId, role => role.Id, (_, role) => role.Name)
            .ToListAsync(cancellationToken);
        return roles.Any(role =>
            role is "superadmin" or "Admin" or "Sub_Admin"
            || includeHrAndHo && (role == "HR_Admin" || role == "HO_Account"));
    }

    private async Task<IReadOnlyList<ulong>?> CustomerIdsByCities(string? cityIds, CancellationToken cancellationToken)
    {
        var ids = ParseIds(cityIds);
        if (ids.Count == 0) return null;
        var rows = await QueryRows($"SELECT customer_id FROM addresses WHERE city_id IN ({string.Join(',', ids)}) AND customer_id IS NOT NULL AND deleted_at IS NULL", cancellationToken);
        return rows.Select(x => ULong(x, "customer_id")).Where(x => x > 0).ToArray();
    }

    private async Task<IReadOnlyList<ulong>?> UserIdsByBranches(string? branchIds, CancellationToken cancellationToken)
    {
        var ids = ParseIds(branchIds);
        if (ids.Count == 0) return null;
        var rows = await QueryRows($"SELECT id FROM users WHERE branch_id IN ({string.Join(',', ids)}) AND deleted_at IS NULL", cancellationToken);
        return rows.Select(x => ULong(x, "id")).Where(x => x > 0).ToArray();
    }

    private async Task<IReadOnlyList<object>> CustomerTypes(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return
        [
            new { id = 1UL, customertype_name = "Dealer" },
            new { id = 2UL, customertype_name = "Retailer" },
            new { id = 3UL, customertype_name = "Influencer" }
        ];
    }

    private async Task UpsertAddress(ulong customerId, CustomerMutationForm request, CancellationToken cancellationToken)
    {
        var addressId = request.AddressId ?? await ExistingId("addresses", "customer_id", customerId, cancellationToken);
        if (addressId.HasValue)
        {
            var rows = await Execute(@"UPDATE addresses SET address1=@address1,address2=@address2,landmark=@landmark,locality=@locality,country_id=@country_id,state_id=@state_id,
district_id=@district_id,city_id=@city_id,pincode_id=@pincode_id,zipcode=@zipcode,updated_at=@now WHERE id=@id AND customer_id=@customer_id", cancellationToken,
                AddressParams(customerId, request).Append(("@id", addressId.Value)).ToArray());
            if (rows > 0) return;
        }
        await Execute(@"INSERT INTO addresses (active, customer_id, address1, address2, landmark, locality, country_id, state_id, district_id, city_id, pincode_id, zipcode, created_by, created_at, updated_at)
VALUES ('Y', @customer_id, @address1, @address2, @landmark, @locality, @country_id, @state_id, @district_id, @city_id, @pincode_id, @zipcode, @created_by, @now, @now)",
            cancellationToken, AddressParams(customerId, request).ToArray());
    }

    private IEnumerable<(string, object?)> AddressParams(ulong customerId, CustomerMutationForm request)
    {
        yield return ("@customer_id", customerId);
        yield return ("@address1", request.Address1 ?? request.Address ?? string.Empty);
        yield return ("@address2", request.Address2 ?? string.Empty);
        yield return ("@landmark", request.Landmark ?? string.Empty);
        yield return ("@locality", request.Locality ?? request.Landmark ?? string.Empty);
        yield return ("@country_id", request.CountryId);
        yield return ("@state_id", request.StateId);
        yield return ("@district_id", request.DistrictId);
        yield return ("@city_id", request.CityId);
        yield return ("@pincode_id", request.PincodeId);
        yield return ("@zipcode", request.Zipcode ?? string.Empty);
        yield return ("@created_by", CurrentUserId());
        yield return ("@now", IndiaNow());
    }

    private async Task UpsertCustomerDetails(ulong customerId, CustomerMutationForm request, CancellationToken cancellationToken)
    {
        var id = await ExistingId("customer_details", "customer_id", customerId, cancellationToken);
        var parameters = new (string, object?)[]
        {
            ("@customer_id", customerId),
            ("@gstin_no", Capitalize(request.GstinNo)),
            ("@pan_no", Capitalize(request.PanNo)),
            ("@aadhar_no", Capitalize(request.AadharNo)),
            ("@otherid_no", Capitalize(request.OtherIdNo)),
            ("@enrollment_date", request.EnrollmentDate),
            ("@approval_date", request.ApprovalDate),
            ("@grade", request.Grade ?? string.Empty),
            ("@visit_status", request.StatusType ?? string.Empty),
            ("@now", IndiaNow()),
            ("@id", id)
        };
        if (id.HasValue)
        {
            await Execute(@"UPDATE customer_details SET gstin_no=@gstin_no,pan_no=@pan_no,aadhar_no=@aadhar_no,otherid_no=@otherid_no,
enrollment_date=@enrollment_date,approval_date=@approval_date,grade=@grade,visit_status=@visit_status,updated_at=@now WHERE id=@id", cancellationToken, parameters);
            return;
        }

        await Execute(@"INSERT INTO customer_details (active, customer_id, gstin_no, pan_no, aadhar_no, otherid_no, enrollment_date, approval_date, grade, visit_status, created_at, updated_at)
VALUES ('Y', @customer_id, @gstin_no, @pan_no, @aadhar_no, @otherid_no, @enrollment_date, @approval_date, @grade, @visit_status, @now, @now)",
            cancellationToken, parameters);
    }

    private async Task UpsertBeatCustomer(ulong customerId, ulong? beatId, CancellationToken cancellationToken)
    {
        if (!beatId.HasValue) return;
        var id = await ExistingId("beat_customers", "customer_id", customerId, cancellationToken);
        if (id.HasValue)
        {
            await Execute("UPDATE beat_customers SET beat_id=@beat_id,updated_at=@now WHERE id=@id", cancellationToken,
                ("@beat_id", beatId.Value), ("@id", id.Value), ("@now", IndiaNow()));
            return;
        }
        await Execute("INSERT INTO beat_customers (active, beat_id, customer_id, created_at, updated_at) VALUES ('Y', @beat_id, @customer_id, @now, @now)", cancellationToken,
            ("@beat_id", beatId.Value), ("@customer_id", customerId), ("@now", IndiaNow()));
    }

    private async Task<ulong?> ExistingId(string table, string column, ulong value, CancellationToken cancellationToken)
    {
        var deletedFilter = table is "beat_customers" ? string.Empty : " AND deleted_at IS NULL";
        var result = await QueryScalar($"SELECT id FROM {table} WHERE {column} = @value{deletedFilter} ORDER BY id DESC LIMIT 1", cancellationToken, ("@value", value));
        return result is null or DBNull ? null : Convert.ToUInt64(result, CultureInfo.InvariantCulture);
    }

    private async Task SyncParentDetails(ulong customerId, string? parentIds, CancellationToken cancellationToken)
    {
        var ids = ParseIds(parentIds);
        if (ids.Count == 0) return;
        await Execute("UPDATE parent_details SET deleted_at = @now WHERE customer_id = @customer_id", cancellationToken, ("@customer_id", customerId), ("@now", IndiaNow()));
        foreach (var id in ids)
        {
            await Execute("INSERT INTO parent_details (active, customer_id, parent_id, created_by, created_at, updated_at) VALUES ('Y', @customer_id, @parent_id, @user_id, @now, @now)", cancellationToken,
                ("@customer_id", customerId), ("@parent_id", id), ("@user_id", CurrentUserId()), ("@now", IndiaNow()));
        }
    }

    private async Task<object?> QueryScalar(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task<long> QueryScalarLong(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var value = await QueryScalar(sql, cancellationToken, parameters);
        return value is null or DBNull ? 0 : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private async Task<decimal> QueryScalarDecimal(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var value = await QueryScalar(sql, cancellationToken, parameters);
        return value is null or DBNull ? 0 : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private async Task<int> Execute(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> QueryRows(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
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

    private static void AddParameters(IDbCommand command, IEnumerable<(string Name, object? Value)> parameters)
    {
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }
    }

    private static void AddIn(List<string> where, string column, IReadOnlyList<ulong>? values, string name)
    {
        if (values is null) return;
        if (values.Count == 0)
        {
            where.Add("1 = 0");
            return;
        }

        where.Add($"{column} IN ({string.Join(',', values.Distinct())})");
    }

    private static void AddAssignedCustomerVisibility(List<string> where, IReadOnlyList<ulong>? userIds)
    {
        if (userIds is null) return;
        if (userIds.Count == 0)
        {
            where.Add("1 = 0");
            return;
        }

        var ids = string.Join(',', userIds.Distinct());
        where.Add($"ed.user_id IN ({ids})");
    }

    private static IReadOnlyList<ulong> ParseIds(string? csv) =>
        (csv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => ulong.TryParse(x, out var id) ? id : (ulong?)null)
        .Where(x => x.HasValue).Select(x => x!.Value).ToArray();

    private static string NormalizeMobile(string? mobile)
    {
        var digits = new string((mobile ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 10 ? $"91{digits}" : digits;
    }

    private static (string First, string Last) SplitName(string? fullName, string? firstName, string? lastName)
    {
        if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName)) return (firstName ?? string.Empty, lastName ?? string.Empty);
        var parts = (fullName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return (string.Empty, string.Empty);
        return (string.Join(' ', parts.Take(parts.Length - 1)), parts.Last());
    }

    private static object? AddressObject(Dictionary<string, object?> row) => new
    {
        id = Obj(row, "address_id"),
        customer_id = Obj(row, "id"),
        address1 = Str(row, "address1"),
        address2 = Str(row, "address2"),
        landmark = Str(row, "landmark"),
        locality = Str(row, "locality"),
        country_id = Obj(row, "country_id"),
        state_id = Obj(row, "state_id"),
        district_id = Obj(row, "district_id"),
        city_id = Obj(row, "city_id"),
        pincode_id = Obj(row, "pincode_id"),
        zipcode = Str(row, "zipcode")
    };

    private static object? CustomerDetailsObject(Dictionary<string, object?> row) => new
    {
        customer_id = Obj(row, "id"),
        gstin_no = Str(row, "gstin_no"),
        pan_no = Str(row, "pan_no"),
        aadhar_no = Str(row, "aadhar_no"),
        otherid_no = Str(row, "otherid_no"),
        shop_image = Str(row, "shop_image"),
        visiting_card = Str(row, "visiting_card"),
        grade = Str(row, "grade"),
        visit_status = Str(row, "visit_status")
    };

    private static object VisitActivity(Dictionary<string, object?> row) => new
    {
        id = Obj(row, "id"),
        customer_id = Obj(row, "customer_id"),
        description = Str(row, "description"),
        report_title = FirstNonEmpty(Str(row, "report_title"), "-"),
        visit_image = Str(row, "visit_image"),
        user_id = Obj(row, "user_id"),
        user_name = string.Empty,
        created_at = FormatDate(Obj(row, "created_at"))
    };

    private static string CustomerNameWithSap(Dictionary<string, object?> row) =>
        $"{Str(row, "name")} ({Str(row, "sap_code")})";

    private static object? Obj(Dictionary<string, object?> row, string key) => row.TryGetValue(key, out var value) && value is not DBNull ? value : null;
    private static string Str(Dictionary<string, object?> row, string key) => Convert.ToString(Obj(row, key), CultureInfo.InvariantCulture) ?? string.Empty;
    private static ulong ULong(Dictionary<string, object?> row, string key) => Obj(row, key) is null ? 0 : Convert.ToUInt64(Obj(row, key), CultureInfo.InvariantCulture);
    private static decimal Dec(Dictionary<string, object?> row, string key) => Obj(row, key) is null ? 0 : Convert.ToDecimal(Obj(row, key), CultureInfo.InvariantCulture);

    private static DateTime? ParseDate(string? value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ? date : null;
    private static string FormatDate(object? value) => value is null or DBNull ? string.Empty : Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    private static string Capitalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : char.ToUpperInvariant(value.Trim()[0]) + value.Trim()[1..];
    private static DateTime IndiaNow() => DateTime.UtcNow.AddHours(5).AddMinutes(30);
    private ulong CurrentUserId() => ulong.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new InvalidOperationException("Unauthenticated.");

    private sealed class CustomerRowFilter
    {
        public string? Search { get; init; }
        public ulong? CustomerId { get; init; }
        public ulong? CustomerType { get; init; }
        public bool ActiveOnly { get; init; }
        public bool DistributorOnly { get; init; }
        public bool RetailerOnly { get; init; }
        public bool OrderByName { get; init; }
        public bool Latest { get; init; }
        public int? PageSize { get; init; }
        public IReadOnlyList<ulong>? VisibleUserIds { get; init; }
        public IReadOnlyList<ulong>? CityCustomerIds { get; init; }
        public IReadOnlyList<ulong>? BranchUserIds { get; init; }
        public IReadOnlyList<ulong>? ExecutiveUserIds { get; init; }
    }

    public sealed class CustomerListQuery
    {
        public int? PageSize { get; init; }
        public string? Search { get; init; }
        [FromQuery(Name = "city_id")]
        public string? CityId { get; init; }
        [FromQuery(Name = "branch_id")]
        public string? BranchId { get; init; }
        [FromQuery(Name = "customertype")]
        public ulong? Customertype { get; init; }
    }

    public sealed class CustomerMutationForm
    {
        public ulong? Id { get; init; }
        [FromForm(Name = "customer_id")] public ulong? CustomerId { get; init; }
        public string? Active { get; init; }
        public string? Name { get; init; }
        [FromForm(Name = "full_name")] public string? FullName { get; init; }
        [FromForm(Name = "first_name")] public string? FirstName { get; init; }
        [FromForm(Name = "last_name")] public string? LastName { get; init; }
        public string? Mobile { get; init; }
        public string? Email { get; init; }
        public string? Latitude { get; init; }
        public string? Longitude { get; init; }
        public string? Gender { get; init; }
        [FromForm(Name = "firmtype")] public ulong? FirmType { get; init; }
        [FromForm(Name = "contact_number")] public string? ContactNumber { get; init; }
        [FromForm(Name = "notification_id")] public string? NotificationId { get; init; }
        [FromForm(Name = "device_type")] public string? DeviceType { get; init; }
        [FromForm(Name = "customer_code")] public string? CustomerCode { get; init; }
        [FromForm(Name = "status_id")] public ulong? StatusId { get; init; }
        [FromForm(Name = "customertype")] public ulong? Customertype { get; init; }
        [FromForm(Name = "manager_name")] public string? ManagerName { get; init; }
        [FromForm(Name = "manager_phone")] public string? ManagerPhone { get; init; }
        [FromForm(Name = "parent_id")] public string? ParentId { get; init; }
        [FromForm(Name = "address_id")] public ulong? AddressId { get; init; }
        public string? Address { get; init; }
        [FromForm(Name = "address1")] public string? Address1 { get; init; }
        [FromForm(Name = "address2")] public string? Address2 { get; init; }
        public string? Landmark { get; init; }
        public string? Locality { get; init; }
        [FromForm(Name = "country_id")] public ulong? CountryId { get; init; }
        [FromForm(Name = "state_id")] public ulong? StateId { get; init; }
        [FromForm(Name = "district_id")] public ulong? DistrictId { get; init; }
        [FromForm(Name = "city_id")] public ulong? CityId { get; init; }
        [FromForm(Name = "pincode_id")] public ulong? PincodeId { get; init; }
        public string? Zipcode { get; init; }
        [FromForm(Name = "gstin_no")] public string? GstinNo { get; init; }
        [FromForm(Name = "pan_no")] public string? PanNo { get; init; }
        [FromForm(Name = "aadhar_no")] public string? AadharNo { get; init; }
        [FromForm(Name = "otherid_no")] public string? OtherIdNo { get; init; }
        [FromForm(Name = "enrollment_date")] public string? EnrollmentDate { get; init; }
        [FromForm(Name = "approval_date")] public string? ApprovalDate { get; init; }
        public string? Grade { get; init; }
        [FromForm(Name = "status_type")] public string? StatusType { get; init; }
        [FromForm(Name = "beat_id")] public ulong? BeatId { get; init; }
    }
}
