using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class FieldKonnectCustomerRestController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public FieldKonnectCustomerRestController(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    [HttpGet("master-distributors")]
    public async Task<IActionResult> MasterDistributors(CancellationToken cancellationToken)
    {
        try
        {
            var page = Page();
            var perPage = PerPage();
            var (rows, total) = await FallbackMasterDistributors(page, perPage, cancellationToken);

            return Ok(new
            {
                status = "success",
                message = "Master distributors retrieved successfully",
                data = Paginator(rows.Select(CleanRow).ToList(), page, perPage, total)
            });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = "Failed to fetch master distributors", error = ExceptionMessage(exception) });
        }
    }

    [HttpGet("master-distributors/{id}")]
    public async Task<IActionResult> MasterDistributor(ulong id, CancellationToken cancellationToken)
    {
        try
        {
            var row = await FallbackCustomerDistributor(id, cancellationToken);
            if (row is null) return NotFound(new { status = false, message = "Distributor not found" });
            var data = DistributorDetails(row);
            return Ok(new
            {
                status = true,
                data,
                check_status = await CheckStatus("distributor", id, cancellationToken),
                hierarchy_level = await HierarchyLevel(cancellationToken)
            });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = false, message = ExceptionMessage(exception) });
        }
    }

    [HttpPost("master-distributors")]
    public async Task<IActionResult> StoreMasterDistributor(CancellationToken cancellationToken) => await UpsertMasterDistributor(null, cancellationToken);

    [HttpPost("master-distributors/{id}")]
    public async Task<IActionResult> UpdateMasterDistributor(ulong id, CancellationToken cancellationToken) => await UpsertMasterDistributor(id, cancellationToken);

    [HttpGet("secondary-customers")]
    public async Task<IActionResult> SecondaryCustomers(CancellationToken cancellationToken)
    {
        try
        {
            var type = Request.Query["type"].ToString();
            if (string.IsNullOrWhiteSpace(type) || !new[] { "RETAILER", "WORKSHOP", "MECHANIC", "GARAGE" }.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { status = "error", message = "Validation failed", errors = new { type = new[] { "Invalid or missing type parameter." } } });
            }

            var page = Page();
            var perPage = PerPage(10);
            var (rows, total) = await FallbackSecondaryCustomers(type.ToUpperInvariant(), page, perPage, cancellationToken);

            return Ok(new
            {
                status = "success",
                message = "Secondary customers retrieved successfully",
                data = Paginator(rows.Select(CleanRow).ToList(), page, perPage, total)
            });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = "error", message = "Failed to fetch secondary customers", error = ExceptionMessage(exception) });
        }
    }

    [HttpGet("secondary-customers/{id}")]
    public async Task<IActionResult> SecondaryCustomer(ulong id, CancellationToken cancellationToken)
    {
        try
        {
            var row = await FallbackCustomerSecondary(id, Request.Query["type"].ToString().ToUpperInvariant(), cancellationToken);
            if (row is null) return NotFound(new { status = false, message = "Customer not found" });
            return Ok(new
            {
                status = true,
                data = RetailerDetails(row),
                distributors = await LinkedDistributors(row, cancellationToken),
                check_status = await CheckStatus("secondary_customer", id, cancellationToken),
                hierarchy_level = await HierarchyLevel(cancellationToken)
            });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { status = false, message = ExceptionMessage(exception) });
        }
    }

    [HttpPost("secondary-customers")]
    public async Task<IActionResult> StoreSecondaryCustomer(CancellationToken cancellationToken) => await UpsertSecondaryCustomer(null, cancellationToken);

    [HttpPost("secondary-customers/{id}")]
    public async Task<IActionResult> UpdateSecondaryCustomer(ulong id, CancellationToken cancellationToken) => await UpsertSecondaryCustomer(id, cancellationToken);

    [HttpGet("getBeatCustomers")]
    public async Task<IActionResult> GetBeatCustomers(CancellationToken cancellationToken)
    {
        var beatId = ULongQuery("beat_id");
        if (!beatId.HasValue) return BadRequest(new { status = "error", message = "beat_id is required" });
        var page = Page();
        var perPage = PerPage(10);
        var offset = (page - 1) * perPage;
        var where = new List<string> { "bc.beat_id = @beat_id" };
        var parameters = BaseParameters();
        parameters.Add(("@beat_id", beatId.Value));
        var search = Request.Query["search"].ToString();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(c.name LIKE @search OR c.mobile LIKE @search OR c.email LIKE @search OR c.customer_code LIKE @search)");
            parameters.Add(("@search", "%" + search.Trim() + "%"));
        }
        var status = Request.Query["status"].ToString();
        if (!string.IsNullOrWhiteSpace(status))
        {
            where.Add("cd.visit_status = @status");
            parameters.Add(("@status", status.Trim()));
        }
        var cityName = Request.Query["city_name"].ToString();
        if (!string.IsNullOrWhiteSpace(cityName))
        {
            where.Add("customer_city.city_name LIKE @city_name");
            parameters.Add(("@city_name", "%" + cityName.Trim() + "%"));
        }

        var total = await QueryScalarLong($@"SELECT COUNT(*) FROM beat_customers bc
LEFT JOIN customers c ON c.id = bc.customer_id
LEFT JOIN addresses ca ON ca.customer_id = c.id AND ca.deleted_at IS NULL
LEFT JOIN cities customer_city ON customer_city.id = ca.city_id
LEFT JOIN customer_details cd ON cd.customer_id = c.id AND cd.deleted_at IS NULL
WHERE {string.Join(" AND ", where)}", cancellationToken, parameters.ToArray());
        var rows = await QueryRows($@"SELECT bc.*, b.beat_name,
c.name AS owner_name, c.name AS shop_name, c.mobile AS mobile_number, ctype.customertype_name AS type, cd.visit_status AS status, ca.city_id, customer_city.city_name,
c.name AS customer_name, c.mobile AS customer_mobile, cd.visit_status,
(SELECT IF(COUNT(*) > 0, 1, 0) FROM check_in ci WHERE ci.user_id = @auth_user AND ci.checkin_date = @today AND (ci.customer_id = bc.customer_id OR ci.entity_id = bc.customer_id)) AS isvisited
FROM beat_customers bc
LEFT JOIN beats b ON b.id = bc.beat_id
LEFT JOIN customers c ON c.id = bc.customer_id
LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL
LEFT JOIN addresses ca ON ca.customer_id = c.id AND ca.deleted_at IS NULL
LEFT JOIN cities customer_city ON customer_city.id = ca.city_id
LEFT JOIN customer_details cd ON cd.customer_id = c.id AND cd.deleted_at IS NULL
WHERE {string.Join(" AND ", where)}
ORDER BY bc.id DESC
LIMIT {perPage} OFFSET {offset}", cancellationToken, parameters.ToArray());
        return Ok(new { status = "success", message = rows.Count > 0 ? "Data retrieved successfully." : "No Record Found.", data = rows.Select(CleanRow).ToList(), page_count = total == 0 ? 1 : (long)Math.Ceiling(total / (double)perPage) });
    }

    [HttpGet("getBeatDropdownList")]
    public async Task<IActionResult> GetBeatDropdownList(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var cityIds = ParseIds(Request.Query["city_id"].ToString());
        var where = new List<string> { "bu.user_id = @user_id" };
        if (cityIds.Count > 0) where.Add("(" + string.Join(" OR ", cityIds.Select(id => $"FIND_IN_SET({id}, b.city_id)")) + ")");
        var rows = await QueryRows($@"SELECT b.id AS beat_id, b.beat_name, b.city_id
FROM beats b
INNER JOIN beat_users bu ON bu.beat_id = b.id
WHERE {string.Join(" AND ", where)}
ORDER BY b.city_id ASC", cancellationToken, ("@user_id", userId));
        if (rows.Count == 0) return Ok(new { status = "error", message = "No Record Found.", data = rows });
        return Ok(new { status = "success", message = "Data retrieved successfully.", data = rows.Select(CleanRow).ToList() });
    }

    [HttpGet("getStateList")]
    public async Task<IActionResult> GetStateList(CancellationToken cancellationToken)
    {
        var countryId = ULongQuery("country_id");
        var where = countryId.HasValue ? "WHERE country_id = @country_id AND deleted_at IS NULL" : "WHERE deleted_at IS NULL";
        var rows = await QueryRows($"SELECT id AS state_id, state_name FROM states {where} ORDER BY state_name ASC", cancellationToken, ("@country_id", countryId));
        return ListResponse(rows.Select(CleanRow).ToList());
    }

    [HttpGet("getDistrictList")]
    public async Task<IActionResult> GetDistrictList(CancellationToken cancellationToken)
    {
        var stateId = ULongQuery("state_id");
        var where = stateId.HasValue ? "WHERE state_id = @state_id AND deleted_at IS NULL" : "WHERE deleted_at IS NULL";
        var rows = await QueryRows($"SELECT id AS district_id, district_name FROM districts {where} ORDER BY district_name ASC", cancellationToken, ("@state_id", stateId));
        var disData = await QueryRows(@"SELECT c.id, c.name FROM customers c LEFT JOIN addresses a ON a.customer_id = c.id WHERE c.active = 'Y' AND c.customertype IN (1,3) AND (@state_id IS NULL OR a.state_id = @state_id)", cancellationToken, ("@state_id", stateId));
        if (rows.Count == 0) return Ok(new { status = "error", message = "No Record Found.", data = rows.Select(CleanRow).ToList() });
        return Ok(new { status = "success", message = "Data retrieved successfully.", data = rows.Select(CleanRow).ToList(), dis_data = disData.Select(CleanRow).ToList() });
    }

    [HttpGet("getCityList")]
    public async Task<IActionResult> GetCityList(CancellationToken cancellationToken)
    {
        var districtId = ULongQuery("district_id");
        var where = districtId.HasValue ? "WHERE district_id = @district_id AND deleted_at IS NULL" : "WHERE deleted_at IS NULL";
        var rows = await QueryRows($"SELECT id AS city_id, city_name FROM cities {where} ORDER BY city_name ASC", cancellationToken, ("@district_id", districtId));
        return ListResponse(rows.Select(CleanRow).ToList());
    }

    [HttpGet("getPincodeList")]
    public async Task<IActionResult> GetPincodeList(CancellationToken cancellationToken)
    {
        var cityId = ULongQuery("city_id");
        var where = cityId.HasValue ? "WHERE city_id = @city_id AND deleted_at IS NULL" : "WHERE deleted_at IS NULL";
        var rows = await QueryRows($"SELECT id AS pincode_id, pincode FROM pincodes {where} ORDER BY pincode ASC", cancellationToken, ("@city_id", cityId));
        return ListResponse(rows.Select(CleanRow).ToList());
    }

    [HttpGet("get-location-by-pincode")]
    [HttpPost("get-location-by-pincode")]
    public async Task<IActionResult> GetLocationByPincode(CancellationToken cancellationToken)
    {
        var body = await RequestValues(cancellationToken);
        var pincode = Value(body, "pincode") ?? Request.Query["pincode"].ToString();
        var pincodeId = ULongValue(body, "pincode_id") ?? ULongQuery("pincode_id");
        var row = (await QueryRows(@"SELECT p.id AS pincode_id, p.pincode, p.city_id, c.city_name, c.district_id, d.district_name, d.state_id, s.state_name, s.country_id, co.country_name
FROM pincodes p
LEFT JOIN cities c ON c.id = p.city_id
LEFT JOIN districts d ON d.id = c.district_id
LEFT JOIN states s ON s.id = d.state_id
LEFT JOIN countries co ON co.id = s.country_id
WHERE p.deleted_at IS NULL
AND ((@pincode_id IS NOT NULL AND p.id = @pincode_id) OR (@pincode_id IS NULL AND p.pincode = @pincode))
LIMIT 1", cancellationToken, ("@pincode", pincode), ("@pincode_id", pincodeId))).FirstOrDefault();
        if (row is null) return Ok(new { status = "error", message = "No Record Found.", data = (object?)null });
        var disData = await QueryRows("SELECT id, name FROM customers WHERE active = 'Y' AND customertype IN (1,3)", cancellationToken);
        return Ok(new { status = "success", message = "Record Found.", data = CleanRow(row), dis_data = disData.Select(CleanRow).ToList() });
    }

    [HttpGet("master-distributors/supervisors")]
    public async Task<IActionResult> MasterDistributorSupervisors(CancellationToken cancellationToken)
    {
        var rows = await QueryRows("SELECT id, name, employee_codes, employee_codes AS employee_code FROM users WHERE deleted_at IS NULL AND active = 'Y' ORDER BY name ASC", cancellationToken);
        return Ok(new { status = "success", message = "Supervisors retrieved successfully", data = rows.Select(CleanRow).ToList(), count = rows.Count });
    }

    [HttpGet("getMyHierarchyUsers")]
    public async Task<IActionResult> GetMyHierarchyUsers(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var ids = await VisibleHierarchyUserIds(userId, cancellationToken);
        var rows = ids.Count == 0
            ? []
            : await QueryRows($@"SELECT u.id, u.name, u.mobile, u.employee_codes, u.employee_codes AS employee_code, u.designation_id, u.reportingid
FROM users u
WHERE u.id IN ({string.Join(',', ids)})
AND u.deleted_at IS NULL
AND NOT EXISTS (
    SELECT 1 FROM model_has_roles m
    INNER JOIN roles r ON r.id = m.role_id
    WHERE m.model_id = u.id
    AND (m.role_id = 61 OR r.name = 'Distributor')
)
ORDER BY FIELD(u.id, {string.Join(',', ids)})", cancellationToken);

        var data = rows.Select(CleanRow).ToList();
        var myself = data.FirstOrDefault(row => Convert.ToString(row["id"], CultureInfo.InvariantCulture) == userId.ToString(CultureInfo.InvariantCulture));
        var reportingUsers = data
            .Where(row => Convert.ToString(row["id"], CultureInfo.InvariantCulture) != userId.ToString(CultureInfo.InvariantCulture))
            .ToList();
        return Ok(new
        {
            status = true,
            message = "Hierarchy users retrieved",
            total_users = data.Count,
            myself,
            reporting_users = reportingUsers,
            users = data,
            data
        });
    }

    [HttpGet("secondary-customer/cities")]
    public async Task<IActionResult> SecondaryCustomerCities(CancellationToken cancellationToken)
    {
        var access = await SecondaryCustomerAccess(CurrentUserId(), cancellationToken);
        var fallbackScope = access.AllAccess
            ? string.Empty
            : $" AND ed.user_id IN ({string.Join(',', access.UserIds)})";
        var rows = access.UserIds.Count == 0 && !access.AllAccess ? [] : await QueryRows($@"SELECT DISTINCT city.id, city.city_name
FROM cities city
INNER JOIN addresses a ON a.city_id = city.id AND a.deleted_at IS NULL
INNER JOIN customers c ON c.id = a.customer_id AND c.deleted_at IS NULL AND c.active = 'Y'
LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL
LEFT JOIN employee_details ed ON ed.customer_id = c.id AND ed.deleted_at IS NULL
WHERE city.deleted_at IS NULL
AND (ctype.customertype_name LIKE '%Retailer%' OR ctype.type_name LIKE '%Retailer%' OR (c.customertype NOT IN (1,3) AND COALESCE(ctype.customertype_name, '') NOT LIKE '%Distributor%' AND COALESCE(ctype.type_name, '') NOT LIKE '%Distributor%'))
{fallbackScope}
ORDER BY city.city_name ASC", cancellationToken);
        return Ok(new { status = "success", message = "Cities retrieved successfully", data = rows.Select(CleanRow).ToList() });
    }

    private async Task<IActionResult> UpsertMasterDistributor(ulong? id, CancellationToken cancellationToken)
    {
        try
        {
            var body = await RequestValues(cancellationToken);
            var customerId = await UpsertUnifiedCustomer(id, body, CustomerEndpointKind.MasterDistributor, cancellationToken);
            return Ok(new
            {
                status = "success",
                message = id.HasValue ? "Master distributor updated successfully" : "Master distributor created successfully",
                data = new { id = customerId, customer_id = customerId }
            });
        }
        catch (Exception exception)
        {
            if (exception is ArgumentException) return BadRequest(ValidationError(ExceptionMessage(exception)));
            return StatusCode(500, new { status = "error", message = ExceptionMessage(exception) });
        }
    }

    private async Task<IActionResult> UpsertSecondaryCustomer(ulong? id, CancellationToken cancellationToken)
    {
        try
        {
            var body = await RequestValues(cancellationToken);
            var customerId = await UpsertUnifiedCustomer(id, body, CustomerEndpointKind.SecondaryCustomer, cancellationToken);
            var row = await FallbackCustomerSecondary(customerId, Value(body, "type") ?? Request.Query["type"].ToString().ToUpperInvariant(), cancellationToken);
            return Ok(new
            {
                status = true,
                message = id.HasValue ? "Customer updated successfully" : "Customer created successfully",
                data = row is null ? new Dictionary<string, object?> { ["id"] = customerId, ["customer_id"] = customerId } : RetailerDetails(row),
                distributors = row is null ? Array.Empty<object>() : await LinkedDistributors(row, cancellationToken)
            });
        }
        catch (Exception exception)
        {
            if (exception is ArgumentException) return BadRequest(ValidationError(ExceptionMessage(exception)));
            return StatusCode(500, new { status = "error", message = ExceptionMessage(exception) });
        }
    }

    private async Task<ulong> UpsertUnifiedCustomer(ulong? id, IReadOnlyDictionary<string, string> body, CustomerEndpointKind kind, CancellationToken cancellationToken)
    {
        var now = IndiaNow();
        var isDistributor = kind == CustomerEndpointKind.MasterDistributor;
        if (!isDistributor) body = await NormalizeSecondaryDistributorLinks(body, cancellationToken);
        var typeName = (Value(body, "type") ?? "RETAILER").ToUpperInvariant();
        var mobile = NormalizeMobile(isDistributor ? Value(body, "mobile") : FirstNonEmpty(Value(body, "mobile_number"), Value(body, "mobile")));
        if (string.IsNullOrWhiteSpace(mobile)) throw new ArgumentException(isDistributor ? "The mobile field is required." : "The mobile_number field is required.");

        var duplicateSql = id.HasValue
            ? "SELECT COUNT(*) FROM customers WHERE mobile = @mobile AND id <> @id AND deleted_at IS NULL"
            : "SELECT COUNT(*) FROM customers WHERE mobile = @mobile AND deleted_at IS NULL";
        if (await QueryScalarLong(duplicateSql, cancellationToken, ("@mobile", mobile), ("@id", id)) > 0)
        {
            throw new ArgumentException("Mobile Number Already Exist");
        }

        if (id.HasValue && await QueryScalarLong("SELECT COUNT(*) FROM customers WHERE id = @id AND deleted_at IS NULL", cancellationToken, ("@id", id.Value)) == 0)
        {
            throw new ArgumentException("Customer not found");
        }
        var deletedCustomerId = id.HasValue
            ? null
            : (ulong?)(await QueryScalarLong("SELECT COALESCE(MAX(id), 0) FROM customers WHERE mobile = @mobile AND deleted_at IS NOT NULL", cancellationToken, ("@mobile", mobile)) is var deletedId && deletedId > 0 ? (ulong)deletedId : null);

        var customerStorageFolder = isDistributor ? "distributors/shop_images" : "secondary_customers";
        var profileImage = await SaveFormFile(FirstNonEmpty("profile_image"), "profile-images", cancellationToken, isDistributor ? "distributors/profile_images" : "secondary_customers");
        var shopImage = await SaveFormFile(FirstNonEmpty("shop_image", "shop_photo"), "shop-photos", cancellationToken, customerStorageFolder);
        var cancelledCheque = await SaveFormFile(FirstNonEmpty("cancelled_cheque", "bank_proof"), "bank-proofs", cancellationToken, isDistributor ? "distributors/cheques" : "secondary_customers");
        var gstAttachment = await SaveFormFile("gst_attachment", "gst-attachments", cancellationToken, "secondary_customers");
        var panAttachment = await SaveFormFile("pan_attachment", "pan-attachments", cancellationToken, "secondary_customers");
        var mouFile = await SaveFormFile("mou_file", "documents", cancellationToken, isDistributor ? "distributors/mou" : "secondary_customers");
        var documentFiles = await SaveFormFiles("documents", "documents", cancellationToken, isDistributor ? "distributors/documents" : "secondary_customers");

        var name = isDistributor
            ? FirstNonEmpty(Value(body, "legal_name"), Value(body, "trade_name"), Value(body, "contact_person"), Value(body, "name"))
            : FirstNonEmpty(Value(body, "shop_name"), Value(body, "owner_name"), Value(body, "name"));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(isDistributor ? "The legal_name field is required." : "The shop_name field is required.");

        var contactName = FirstNonEmpty(Value(body, "contact_person"), Value(body, "owner_name"), name) ?? name;
        var (firstName, lastName) = SplitName(contactName);
        var (latitude, longitude) = SplitGps(Value(body, "gps_location"));
        var customerType = ULongValue(body, "customertype") ?? ULongValue(body, "customer_type_id") ?? await CustomerTypeId(isDistributor ? "DISTRIBUTOR" : typeName, isDistributor, cancellationToken);
        var assignedUserIds = AssignedUserIdsFromBody(body, id.HasValue ? null : CurrentUserId());
        var executiveId = assignedUserIds.FirstOrDefault();
        if (executiveId == 0) executiveId = ULongValue(body, "sales_executive_id[0]") ?? ULongValue(body, "sales_executive_id") ?? ULongValue(body, "supervisor_id") ?? ULongValue(body, "employee_id") ?? 0;
        if (executiveId == 0 && id.HasValue) executiveId = (ulong)await QueryScalarLong("SELECT COALESCE(executive_id, 0) FROM customers WHERE id = @id", cancellationToken, ("@id", id.Value));
        if (executiveId == 0) executiveId = CurrentUserId();
        var active = string.Equals(Value(body, "business_status"), "Inactive", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";
        var existingCustomFields = id.HasValue ? await ExistingCustomFields(id.Value, cancellationToken) : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var customFields = BuildCustomFields(existingCustomFields, body, new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["endpoint_type"] = isDistributor ? "MASTER_DISTRIBUTOR" : "SECONDARY_CUSTOMER",
            ["secondary_type"] = isDistributor ? null : typeName,
            [isDistributor ? "sales_executive_id" : "employee_id"] = assignedUserIds.Count > 0 ? string.Join(',', assignedUserIds) : null,
            ["shop_photo"] = shopImage,
            ["shop_image"] = shopImage,
            ["cancelled_cheque"] = cancelledCheque,
            ["bank_proof"] = cancelledCheque,
            ["gst_attachment"] = gstAttachment,
            ["pan_attachment"] = panAttachment,
            ["mou_file"] = mouFile,
            ["documents"] = documentFiles.Count > 0 ? documentFiles : null
        });

        var parameters = new List<(string, object?)>
        {
            ("@active", active),
            ("@name", name.Trim()),
            ("@first_name", firstName),
            ("@last_name", lastName),
            ("@mobile", mobile),
            ("@contact_number", FirstNonEmpty(Value(body, "alternate_mobile"), Value(body, "whatsapp_number"), Value(body, "contact_number"))),
            ("@email", NullIfEmpty(Value(body, "email"))),
            ("@latitude", latitude),
            ("@longitude", longitude),
            ("@profile_image", profileImage),
            ("@shop_image", shopImage),
            ("@customer_code", FirstNonEmpty(Value(body, "distributor_code"), Value(body, "customer_code")) ?? string.Empty),
            ("@status_id", ULongValue(body, "status_id") ?? 2),
            ("@customertype", customerType),
            ("@firmtype", ULongValue(body, "firmtype")),
            ("@created_by", CurrentUserId()),
            ("@updated_by", CurrentUserId()),
            ("@executive_id", executiveId),
            ("@manager_name", FirstNonEmpty(Value(body, "supervisor_name"), Value(body, "manager_name")) ?? string.Empty),
            ("@manager_phone", FirstNonEmpty(Value(body, "manager_phone"), Value(body, "alternate_mobile")) ?? string.Empty),
            ("@sap_code", Value(body, "sap_code")),
            ("@custom_fields", customFields),
            ("@now", now)
        };

        ulong customerId;
        if (id.HasValue)
        {
            customerId = id.Value;
            await Execute(@"UPDATE customers SET active = @active, name = @name, first_name = @first_name, last_name = @last_name,
mobile = @mobile, contact_number = @contact_number, email = @email, latitude = @latitude, longitude = @longitude,
profile_image = COALESCE(@profile_image, profile_image), shop_image = COALESCE(@shop_image, shop_image),
customer_code = @customer_code, status_id = @status_id, customertype = @customertype, firmtype = @firmtype,
updated_by = @updated_by, executive_id = @executive_id, manager_name = @manager_name, manager_phone = @manager_phone,
sap_code = @sap_code, custom_fields = @custom_fields, updated_at = @now WHERE id = @id", cancellationToken,
                parameters.Append(("@id", customerId)).ToArray());
        }
        else if (deletedCustomerId.HasValue)
        {
            customerId = deletedCustomerId.Value;
            await Execute(@"UPDATE customers SET deleted_at = NULL, active = @active, name = @name, first_name = @first_name, last_name = @last_name,
mobile = @mobile, contact_number = @contact_number, email = @email, latitude = @latitude, longitude = @longitude,
profile_image = COALESCE(@profile_image, profile_image), shop_image = COALESCE(@shop_image, shop_image),
customer_code = @customer_code, status_id = @status_id, customertype = @customertype, firmtype = @firmtype,
updated_by = @updated_by, executive_id = @executive_id, manager_name = @manager_name, manager_phone = @manager_phone,
sap_code = @sap_code, custom_fields = @custom_fields, updated_at = @now WHERE id = @id", cancellationToken,
                parameters.Append(("@id", customerId)).ToArray());
        }
        else
        {
            await Execute(@"INSERT INTO customers (active, name, first_name, last_name, mobile, contact_number, email, password, notification_id,
latitude, longitude, device_type, gender, profile_image, shop_image, customer_code, status_id, customertype, firmtype,
created_by, updated_by, executive_id, manager_name, manager_phone, sap_code, custom_fields, created_at, updated_at)
VALUES (@active, @name, @first_name, @last_name, @mobile, @contact_number, @email, '', '', @latitude, @longitude, '', '',
COALESCE(@profile_image, ''), @shop_image, @customer_code, @status_id, @customertype, @firmtype, @created_by, @updated_by,
@executive_id, @manager_name, @manager_phone, @sap_code, @custom_fields, @now, @now)", cancellationToken, parameters.ToArray());
            customerId = (ulong)await QueryScalarLong("SELECT LAST_INSERT_ID()", cancellationToken);
        }

        var addressId = await UpsertUnifiedAddress(customerId, body, isDistributor, cancellationToken);
        await UpsertUnifiedCustomerDetails(customerId, body, shopImage, isDistributor, cancellationToken);
        await UpsertUnifiedBeat(customerId, ULongValue(body, "beat_id"), cancellationToken);
        if (assignedUserIds.Count > 0) await SyncUnifiedEmployeeDetails(customerId, assignedUserIds, cancellationToken);
        else await UpsertEmployeeDetail(customerId, executiveId, cancellationToken);
        if (!addressId.HasValue)
        {
            throw new InvalidOperationException("Customer saved, but address could not be saved.");
        }
        return customerId;
    }

    private async Task<ulong?> UpsertUnifiedAddress(ulong customerId, IReadOnlyDictionary<string, string> body, bool isDistributor, CancellationToken cancellationToken)
    {
        var addressId = await ExistingId("addresses", "customer_id", customerId, cancellationToken);
        var countryId = await ExistingForeignId("countries", isDistributor ? ULongValue(body, "billing_country") : ULongValue(body, "country_id"), cancellationToken);
        var stateId = await ExistingForeignId("states", isDistributor ? ULongValue(body, "billing_state") : ULongValue(body, "state_id"), cancellationToken);
        var districtId = await ExistingForeignId("districts", isDistributor ? ULongValue(body, "billing_district") : ULongValue(body, "district_id"), cancellationToken);
        var cityId = await ExistingForeignId("cities", isDistributor ? ULongValue(body, "billing_city") : ULongValue(body, "city_id"), cancellationToken);
        var pincodeId = await ExistingForeignId("pincodes", isDistributor ? ULongValue(body, "billing_pincode") : ULongValue(body, "pincode_id"), cancellationToken);
        var parameters = new (string, object?)[]
        {
            ("@customer_id", customerId),
            ("@address1", isDistributor ? Value(body, "billing_address") ?? string.Empty : Value(body, "address_line") ?? string.Empty),
            ("@address2", isDistributor ? Value(body, "shipping_address") ?? string.Empty : string.Empty),
            ("@country_id", countryId),
            ("@state_id", stateId),
            ("@district_id", districtId),
            ("@city_id", cityId),
            ("@pincode_id", pincodeId),
            ("@created_by", CurrentUserId()),
            ("@now", IndiaNow()),
            ("@id", addressId)
        };
        if (addressId.HasValue)
        {
            await Execute(@"UPDATE addresses SET address1 = @address1, address2 = @address2, country_id = @country_id,
state_id = @state_id, district_id = @district_id, city_id = @city_id, pincode_id = @pincode_id, updated_at = @now
WHERE id = @id", cancellationToken, parameters);
            return addressId;
        }

        await Execute(@"INSERT INTO addresses (active, customer_id, address1, address2, country_id, state_id, district_id, city_id,
pincode_id, created_by, created_at, updated_at) VALUES ('Y', @customer_id, @address1, @address2, @country_id, @state_id,
@district_id, @city_id, @pincode_id, @created_by, @now, @now)", cancellationToken, parameters);
        return (ulong)await QueryScalarLong("SELECT LAST_INSERT_ID()", cancellationToken);
    }

    private async Task<ulong?> ExistingForeignId(string table, ulong? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id.Value == 0) return null;
        var deletedFilter = table is "countries" or "states" or "districts" or "cities" or "pincodes" ? " AND deleted_at IS NULL" : string.Empty;
        return await QueryScalarLong($"SELECT COUNT(*) FROM {table} WHERE id = @id{deletedFilter}", cancellationToken, ("@id", id.Value)) > 0 ? id : null;
    }

    private async Task UpsertUnifiedCustomerDetails(ulong customerId, IReadOnlyDictionary<string, string> body, string? shopImage, bool isDistributor, CancellationToken cancellationToken)
    {
        var id = await ExistingId("customer_details", "customer_id", customerId, cancellationToken);
        var existingVisitStatusRow = id.HasValue
            ? (await QueryRows("SELECT visit_status FROM customer_details WHERE id = @id LIMIT 1", cancellationToken, ("@id", id.Value))).FirstOrDefault()
            : null;
        var existingVisitStatus = existingVisitStatusRow is null ? null : Str(existingVisitStatusRow, "visit_status");
        var visitStatus = isDistributor
            ? Value(body, "business_status") ?? existingVisitStatus ?? "Active"
            : Value(body, "status") ?? existingVisitStatus ?? "PENDING";
        var parameters = new (string, object?)[]
        {
            ("@customer_id", customerId),
            ("@gstin_no", FirstNonEmpty(Value(body, "gst_number"), Value(body, "gstin_no"))),
            ("@pan_no", FirstNonEmpty(Value(body, "pan_number"), Value(body, "pan_no"))),
            ("@account_holder", FirstNonEmpty(Value(body, "account_holder"), Value(body, "account_holder_name"))),
            ("@account_number", FirstNonEmpty(Value(body, "account_number"), Value(body, "bank_account_number"))),
            ("@bank_name", Value(body, "bank_name")),
            ("@ifsc_code", FirstNonEmpty(Value(body, "ifsc"), Value(body, "ifsc_code"))),
            ("@shop_image", shopImage),
            ("@visit_status", visitStatus),
            ("@now", IndiaNow()),
            ("@id", id)
        };
        if (id.HasValue)
        {
            await Execute(@"UPDATE customer_details SET gstin_no = @gstin_no, pan_no = @pan_no, account_holder = @account_holder,
account_number = @account_number, bank_name = @bank_name, ifsc_code = @ifsc_code, shop_image = COALESCE(@shop_image, shop_image),
visit_status = @visit_status, updated_at = @now WHERE id = @id", cancellationToken, parameters);
            return;
        }

        await Execute(@"INSERT INTO customer_details (active, customer_id, gstin_no, pan_no, account_holder, account_number, bank_name,
ifsc_code, shop_image, visit_status, created_at, updated_at) VALUES ('Y', @customer_id, @gstin_no, @pan_no, @account_holder,
@account_number, @bank_name, @ifsc_code, COALESCE(@shop_image, ''), @visit_status, @now, @now)", cancellationToken, parameters);
    }

    private async Task UpsertUnifiedBeat(ulong customerId, ulong? beatId, CancellationToken cancellationToken)
    {
        if (!beatId.HasValue) return;
        var id = await ExistingId("beat_customers", "customer_id", customerId, cancellationToken);
        if (id.HasValue)
        {
            await Execute("UPDATE beat_customers SET beat_id = @beat_id, updated_at = @now WHERE id = @id", cancellationToken, ("@beat_id", beatId.Value), ("@id", id.Value), ("@now", IndiaNow()));
            return;
        }

        await Execute("INSERT INTO beat_customers (active, beat_id, customer_id, created_at, updated_at) VALUES ('Y', @beat_id, @customer_id, @now, @now)", cancellationToken,
            ("@beat_id", beatId.Value), ("@customer_id", customerId), ("@now", IndiaNow()));
    }

    private async Task UpsertEmployeeDetail(ulong customerId, ulong userId, CancellationToken cancellationToken)
    {
        if (await QueryScalarLong("SELECT COUNT(*) FROM employee_details WHERE customer_id = @customer_id AND user_id = @user_id AND deleted_at IS NULL", cancellationToken,
                ("@customer_id", customerId), ("@user_id", userId)) > 0) return;
        await Execute("INSERT INTO employee_details (active, customer_id, user_id, created_by, created_at, updated_at) VALUES ('Y', @customer_id, @user_id, @created_by, @now, @now)", cancellationToken,
            ("@customer_id", customerId), ("@user_id", userId), ("@created_by", CurrentUserId()), ("@now", IndiaNow()));
    }

    private async Task SyncUnifiedEmployeeDetails(ulong customerId, IReadOnlyCollection<ulong> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0) return;

        await Execute($@"UPDATE employee_details SET deleted_at = @now, updated_by = @updated_by, updated_at = @now
WHERE customer_id = @customer_id AND deleted_at IS NULL AND (user_id IS NULL OR user_id NOT IN ({string.Join(',', ids)}))", cancellationToken,
            ("@customer_id", customerId), ("@updated_by", CurrentUserId()), ("@now", IndiaNow()));

        foreach (var userId in ids)
        {
            await Execute(@"UPDATE employee_details SET active = 'Y', deleted_at = NULL, updated_by = @updated_by, updated_at = @now
WHERE customer_id = @customer_id AND user_id = @user_id AND deleted_at IS NOT NULL", cancellationToken,
                ("@customer_id", customerId), ("@user_id", userId), ("@updated_by", CurrentUserId()), ("@now", IndiaNow()));
            await UpsertEmployeeDetail(customerId, userId, cancellationToken);
        }
    }

    private static List<ulong> AssignedUserIdsFromBody(IReadOnlyDictionary<string, string> body, ulong? fallbackUserId)
    {
        var ids = new[] { "employee_id", "sales_executive_id", "sales_executive_id[0]", "supervisor_id" }
            .SelectMany(key => ParseIds(Value(body, key)))
            .Distinct()
            .ToList();
        if (ids.Count == 0 && fallbackUserId.HasValue) ids.Add(fallbackUserId.Value);
        return ids;
    }

    private async Task<ulong?> ExistingId(string table, string column, ulong value, CancellationToken cancellationToken)
    {
        var deletedFilter = table is "beat_customers" ? string.Empty : " AND deleted_at IS NULL";
        var rows = await QueryRows($"SELECT id FROM {table} WHERE {column} = @value{deletedFilter} ORDER BY id DESC LIMIT 1", cancellationToken, ("@value", value));
        var id = rows.FirstOrDefault();
        return id is null ? null : ULong(id, "id");
    }

    private async Task<ulong> CustomerTypeId(string type, bool distributor, CancellationToken cancellationToken)
    {
        var like = distributor ? "%Distributor%" : "%" + type + "%";
        var row = (await QueryRows(@"SELECT id FROM customer_types
WHERE deleted_at IS NULL AND (customertype_name LIKE @type OR type_name LIKE @type)
ORDER BY id ASC LIMIT 1", cancellationToken, ("@type", like))).FirstOrDefault();
        if (row is not null) return ULong(row, "id");
        return distributor ? 1UL : 2UL;
    }

    private async Task<(IReadOnlyList<Dictionary<string, object?>> Rows, long Total)> FallbackMasterDistributors(int page, int perPage, CancellationToken cancellationToken)
    {
        var offset = (page - 1) * perPage;
        var (where, parameters) = await CustomerDistributorWhere(cancellationToken);
        var total = await QueryScalarLong($"SELECT COUNT(DISTINCT c.id) FROM customers c LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL WHERE {where}", cancellationToken, parameters.ToArray());
        var rows = await QueryRows($@"SELECT DISTINCT c.id,
c.id AS customer_id,
c.name AS legal_name,
c.name AS trade_name,
c.customer_code AS distributor_code,
COALESCE(c.contact_number, c.mobile) AS contact_person,
c.mobile,
c.email,
c.active,
c.sap_code,
c.customertype,
ctype.customertype_name,
ctype.type_name,
COALESCE(c.shop_image, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_image')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_photo'))) AS shop_image,
CONCAT_WS(',', NULLIF(c.latitude, ''), NULLIF(c.longitude, '')) AS gps_location,
COALESCE(cd.gstin_no, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gst_number')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gstin_no'))) AS gst_number,
COALESCE(cd.pan_no, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_number')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_no'))) AS pan_number,
JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_account_type')) AS bank_account_type,
COALESCE(cd.account_number, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_account_number')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_number'))) AS bank_account_number,
cd.bank_name,
COALESCE(cd.ifsc_code, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.ifsc_code')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.ifsc'))) AS ifsc_code,
COALESCE(cd.account_holder, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_holder_name')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_holder'))) AS account_holder_name,
JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.registration_type')) AS registration_type,
a.address1 AS billing_address,
a.city_id AS billing_city,
city.city_name AS billing_city_name,
a.district_id AS billing_district,
d.district_name AS billing_district_name,
a.state_id AS billing_state,
s.state_name AS billing_state_name,
a.pincode_id AS billing_pincode,
p.pincode AS billing_pincode_value,
c.created_by,
c.executive_id AS supervisor_id,
u.name AS supervisor_name,
c.created_at,
c.updated_at,
'customers' AS source_table,
(SELECT ci.checkin_date FROM check_in ci WHERE (ci.entity_type = 'distributor' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user ORDER BY ci.checkin_date DESC, ci.checkin_time DESC LIMIT 1) AS last_checkin_date,
(SELECT ci.checkin_time FROM check_in ci WHERE (ci.entity_type = 'distributor' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user ORDER BY ci.checkin_date DESC, ci.checkin_time DESC LIMIT 1) AS last_checkin_time,
(SELECT IF(COUNT(*) > 0, 1, 0) FROM check_in ci WHERE (ci.entity_type = 'distributor' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user AND ci.checkin_date = @today) AS has_checked_in_today,
(SELECT IF(COUNT(*) > 0, 1, 0) FROM check_in ci WHERE (ci.entity_type = 'distributor' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user AND ci.checkout_date IS NULL AND ci.checkin_date = @today) AS current_visit_is_open,
(SELECT ci.id FROM check_in ci WHERE (ci.entity_type = 'distributor' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user ORDER BY ci.checkin_date DESC, ci.checkin_time DESC LIMIT 1) AS last_checkin_id
FROM customers c
LEFT JOIN addresses a ON a.customer_id = c.id AND a.deleted_at IS NULL
LEFT JOIN customer_details cd ON cd.customer_id = c.id AND cd.deleted_at IS NULL
LEFT JOIN cities city ON city.id = a.city_id
LEFT JOIN districts d ON d.id = a.district_id
LEFT JOIN states s ON s.id = a.state_id
LEFT JOIN pincodes p ON p.id = a.pincode_id
LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL
LEFT JOIN users u ON u.id = c.executive_id
WHERE {where}
ORDER BY c.created_at DESC, c.id DESC
LIMIT {perPage} OFFSET {offset}", cancellationToken, parameters.ToArray());
        return (rows, total);
    }

    private async Task<Dictionary<string, object?>?> FallbackCustomerDistributor(ulong id, CancellationToken cancellationToken)
    {
        var (where, parameters) = await CustomerDistributorWhere(cancellationToken, id);
        return (await QueryRows($@"SELECT DISTINCT c.id,
c.id AS customer_id,
c.name AS legal_name,
c.name AS trade_name,
c.customer_code AS distributor_code,
COALESCE(c.contact_number, c.mobile) AS contact_person,
c.mobile,
c.email,
c.active,
c.sap_code,
c.customertype,
ctype.customertype_name,
ctype.type_name,
COALESCE(c.shop_image, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_image')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_photo'))) AS shop_image,
CONCAT_WS(',', NULLIF(c.latitude, ''), NULLIF(c.longitude, '')) AS gps_location,
COALESCE(cd.gstin_no, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gst_number')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gstin_no'))) AS gst_number,
COALESCE(cd.pan_no, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_number')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_no'))) AS pan_number,
JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_account_type')) AS bank_account_type,
COALESCE(cd.account_number, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_account_number')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_number'))) AS bank_account_number,
cd.bank_name,
COALESCE(cd.ifsc_code, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.ifsc_code')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.ifsc'))) AS ifsc_code,
COALESCE(cd.account_holder, JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_holder_name')), JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_holder'))) AS account_holder_name,
JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.registration_type')) AS registration_type,
a.address1 AS billing_address,
a.city_id AS billing_city,
city.city_name AS billing_city_name,
a.district_id AS billing_district,
d.district_name AS billing_district_name,
a.state_id AS billing_state,
s.state_name AS billing_state_name,
a.pincode_id AS billing_pincode,
p.pincode AS billing_pincode_value,
c.created_by,
c.executive_id AS supervisor_id,
u.name AS supervisor_name,
c.created_at,
c.updated_at,
'customers' AS source_table
FROM customers c
LEFT JOIN addresses a ON a.customer_id = c.id AND a.deleted_at IS NULL
LEFT JOIN customer_details cd ON cd.customer_id = c.id AND cd.deleted_at IS NULL
LEFT JOIN cities city ON city.id = a.city_id
LEFT JOIN districts d ON d.id = a.district_id
LEFT JOIN states s ON s.id = a.state_id
LEFT JOIN pincodes p ON p.id = a.pincode_id
LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL
LEFT JOIN users u ON u.id = c.executive_id
WHERE {where}
LIMIT 1", cancellationToken, parameters.ToArray())).FirstOrDefault();
    }

    private async Task<(IReadOnlyList<Dictionary<string, object?>> Rows, long Total)> FallbackSecondaryCustomers(string type, int page, int perPage, CancellationToken cancellationToken)
    {
        var offset = (page - 1) * perPage;
        var (where, parameters) = await CustomerSecondaryWhere(type, cancellationToken);
        var total = await QueryScalarLong($@"SELECT COUNT(DISTINCT c.id)
FROM customers c
LEFT JOIN addresses a ON a.customer_id = c.id AND a.deleted_at IS NULL
LEFT JOIN cities city ON city.id = COALESCE(a.city_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.city_id')), '') AS UNSIGNED))
LEFT JOIN customer_details cd ON cd.customer_id = c.id AND cd.deleted_at IS NULL
LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL
WHERE {where}", cancellationToken, parameters.ToArray());
        var rows = await QueryRows($@"SELECT DISTINCT c.id,
c.id AS customer_id,
@fallback_type AS type,
	COALESCE(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.owner_name')), ''), NULLIF(c.name, '')) AS owner_name,
	COALESCE(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_name')), ''), NULLIF(c.name, '')) AS shop_name,
	COALESCE(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.mobile_number')), ''), NULLIF(c.mobile, '')) AS mobile_number,
c.contact_number AS whatsapp_number,
c.email,
c.active,
c.customer_code,
c.sap_code,
c.customertype,
ctype.customertype_name,
ctype.type_name,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.sub_type')), '') AS sub_type,
	COALESCE(NULLIF(c.shop_image, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_photo')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_image')), '')) AS shop_photo,
	CONCAT_WS(',', NULLIF(c.latitude, ''), NULLIF(c.longitude, '')) AS gps_location,
	COALESCE(NULLIF(cd.gstin_no, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gst_number')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gstin_no')), '')) AS gst_number,
	COALESCE(NULLIF(cd.pan_no, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_number')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_no')), '')) AS pan_number,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gst_attachment')), '') AS gst_attachment,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_attachment')), '') AS pan_attachment,
	COALESCE(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_proof')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.cancelled_cheque')), '')) AS bank_proof,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_account_type')), '') AS bank_account_type,
	COALESCE(NULLIF(cd.account_number, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_account_number')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_number')), '')) AS bank_account_number,
	COALESCE(NULLIF(cd.bank_name, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_name')), '')) AS bank_name,
	COALESCE(NULLIF(cd.ifsc_code, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.ifsc_code')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.ifsc')), '')) AS ifsc_code,
	COALESCE(NULLIF(cd.account_holder, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_holder_name')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_holder')), '')) AS account_holder_name,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.remark')), '') AS remark,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.distributor_name')), '') AS distributor_name,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.agri_distributor')), '') AS agri_distributor,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.belt_area_market_name')), '') AS belt_area_market_name,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.saathi_awareness_status')), '') AS saathi_awareness_status,
	COALESCE(NULLIF(a.address1, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.address_line')), '')) AS address_line,
COALESCE(a.country_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.country_id')), '') AS UNSIGNED)) AS country_id,
co.country_name,
COALESCE(a.state_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.state_id')), '') AS UNSIGNED)) AS state_id,
s.state_name,
COALESCE(a.district_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.district_id')), '') AS UNSIGNED)) AS district_id,
d.district_name,
COALESCE(a.city_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.city_id')), '') AS UNSIGNED)) AS city_id,
city.city_name,
COALESCE(a.pincode_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pincode_id')), '') AS UNSIGNED)) AS pincode_id,
p.pincode AS pincode_value,
bc.beat_id,
b.beat_name,
cd.visit_status AS status,
c.created_by,
creator.name AS creator_name,
c.executive_id AS employee_id,
c.created_at,
c.updated_at,
'customers' AS source_table,
(SELECT ci.checkin_date FROM check_in ci WHERE (ci.entity_type = 'secondary_customer' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user ORDER BY ci.checkin_date DESC, ci.checkin_time DESC LIMIT 1) AS last_checkin_date,
(SELECT ci.checkin_time FROM check_in ci WHERE (ci.entity_type = 'secondary_customer' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user ORDER BY ci.checkin_date DESC, ci.checkin_time DESC LIMIT 1) AS last_checkin_time,
(SELECT IF(COUNT(*) > 0, 1, 0) FROM check_in ci WHERE (ci.entity_type = 'secondary_customer' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user AND ci.checkin_date = @today) AS has_checked_in_today,
(SELECT IF(COUNT(*) > 0, 1, 0) FROM check_in ci WHERE (ci.entity_type = 'secondary_customer' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user AND ci.checkout_date IS NULL AND ci.checkin_date = @today) AS current_visit_is_open,
(SELECT ci.id FROM check_in ci WHERE (ci.entity_type = 'secondary_customer' OR ci.customer_id = c.id) AND ci.entity_id = c.id AND ci.user_id = @auth_user ORDER BY ci.checkin_date DESC, ci.checkin_time DESC LIMIT 1) AS last_checkin_id
FROM customers c
LEFT JOIN addresses a ON a.customer_id = c.id AND a.deleted_at IS NULL
LEFT JOIN countries co ON co.id = COALESCE(a.country_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.country_id')), '') AS UNSIGNED))
LEFT JOIN states s ON s.id = COALESCE(a.state_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.state_id')), '') AS UNSIGNED))
LEFT JOIN districts d ON d.id = COALESCE(a.district_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.district_id')), '') AS UNSIGNED))
LEFT JOIN cities city ON city.id = COALESCE(a.city_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.city_id')), '') AS UNSIGNED))
LEFT JOIN pincodes p ON p.id = COALESCE(a.pincode_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pincode_id')), '') AS UNSIGNED))
LEFT JOIN customer_details cd ON cd.customer_id = c.id AND cd.deleted_at IS NULL
LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL
LEFT JOIN beat_customers bc ON bc.customer_id = c.id
LEFT JOIN beats b ON b.id = bc.beat_id
LEFT JOIN users creator ON creator.id = c.created_by
WHERE {where}
ORDER BY c.created_at DESC, c.id DESC
LIMIT {perPage} OFFSET {offset}", cancellationToken, parameters.ToArray());
        return (rows, total);
    }

    private async Task<Dictionary<string, object?>?> FallbackCustomerSecondary(ulong id, string type, CancellationToken cancellationToken)
    {
        var fallbackType = string.IsNullOrWhiteSpace(type) ? "RETAILER" : type;
        var (where, parameters) = await CustomerSecondaryWhere(fallbackType, cancellationToken, id);
        return (await QueryRows($@"SELECT DISTINCT c.id,
c.id AS customer_id,
@fallback_type AS type,
	COALESCE(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.owner_name')), ''), NULLIF(c.name, '')) AS owner_name,
	COALESCE(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_name')), ''), NULLIF(c.name, '')) AS shop_name,
	COALESCE(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.mobile_number')), ''), NULLIF(c.mobile, '')) AS mobile_number,
c.contact_number AS whatsapp_number,
c.email,
c.active,
c.customer_code,
c.sap_code,
c.customertype,
ctype.customertype_name,
ctype.type_name,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.sub_type')), '') AS sub_type,
	COALESCE(NULLIF(c.shop_image, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_photo')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.shop_image')), '')) AS shop_photo,
	CONCAT_WS(',', NULLIF(c.latitude, ''), NULLIF(c.longitude, '')) AS gps_location,
	COALESCE(NULLIF(cd.gstin_no, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gst_number')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gstin_no')), '')) AS gst_number,
	COALESCE(NULLIF(cd.pan_no, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_number')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_no')), '')) AS pan_number,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.gst_attachment')), '') AS gst_attachment,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pan_attachment')), '') AS pan_attachment,
	COALESCE(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_proof')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.cancelled_cheque')), '')) AS bank_proof,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_account_type')), '') AS bank_account_type,
	COALESCE(NULLIF(cd.account_number, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_account_number')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_number')), '')) AS bank_account_number,
	COALESCE(NULLIF(cd.bank_name, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.bank_name')), '')) AS bank_name,
	COALESCE(NULLIF(cd.ifsc_code, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.ifsc_code')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.ifsc')), '')) AS ifsc_code,
	COALESCE(NULLIF(cd.account_holder, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_holder_name')), ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.account_holder')), '')) AS account_holder_name,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.remark')), '') AS remark,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.distributor_name')), '') AS distributor_name,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.agri_distributor')), '') AS agri_distributor,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.belt_area_market_name')), '') AS belt_area_market_name,
	NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.saathi_awareness_status')), '') AS saathi_awareness_status,
	COALESCE(NULLIF(a.address1, ''), NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.address_line')), '')) AS address_line,
COALESCE(a.country_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.country_id')), '') AS UNSIGNED)) AS country_id,
co.country_name,
COALESCE(a.state_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.state_id')), '') AS UNSIGNED)) AS state_id,
s.state_name,
COALESCE(a.district_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.district_id')), '') AS UNSIGNED)) AS district_id,
d.district_name,
COALESCE(a.city_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.city_id')), '') AS UNSIGNED)) AS city_id,
city.city_name,
COALESCE(a.pincode_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pincode_id')), '') AS UNSIGNED)) AS pincode_id,
p.pincode AS pincode_value,
bc.beat_id,
b.beat_name,
cd.visit_status AS status,
c.created_by,
creator.name AS creator_name,
c.executive_id AS employee_id,
c.created_at,
c.updated_at,
'customers' AS source_table
FROM customers c
LEFT JOIN addresses a ON a.customer_id = c.id AND a.deleted_at IS NULL
LEFT JOIN countries co ON co.id = COALESCE(a.country_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.country_id')), '') AS UNSIGNED))
LEFT JOIN states s ON s.id = COALESCE(a.state_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.state_id')), '') AS UNSIGNED))
LEFT JOIN districts d ON d.id = COALESCE(a.district_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.district_id')), '') AS UNSIGNED))
LEFT JOIN cities city ON city.id = COALESCE(a.city_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.city_id')), '') AS UNSIGNED))
LEFT JOIN pincodes p ON p.id = COALESCE(a.pincode_id, CAST(NULLIF(JSON_UNQUOTE(JSON_EXTRACT(c.custom_fields, '$.pincode_id')), '') AS UNSIGNED))
LEFT JOIN customer_details cd ON cd.customer_id = c.id AND cd.deleted_at IS NULL
LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL
LEFT JOIN beat_customers bc ON bc.customer_id = c.id
LEFT JOIN beats b ON b.id = bc.beat_id
LEFT JOIN users creator ON creator.id = c.created_by
WHERE {where}
LIMIT 1", cancellationToken, parameters.ToArray())).FirstOrDefault();
    }

    private async Task<(string Where, List<(string, object?)> Parameters)> CustomerDistributorWhere(CancellationToken cancellationToken, ulong? id = null)
    {
        var parameters = BaseParameters();
        var where = new List<string>
        {
            "c.deleted_at IS NULL",
            "c.active = 'Y'",
            "(c.customertype IN (1,3) OR ctype.customertype_name LIKE '%Distributor%' OR ctype.type_name LIKE '%Distributor%')"
        };
        if (id.HasValue)
        {
            where.Add("c.id = @customer_id");
            parameters.Add(("@customer_id", id.Value));
        }

        var search = Request.Query["global_search"].ToString();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(c.name LIKE @search OR c.mobile LIKE @search OR c.email LIKE @search OR c.customer_code LIKE @search OR c.sap_code LIKE @search)");
            parameters.Add(("@search", "%" + search.Trim() + "%"));
        }

        if (ULongQuery("for_user_id") is { } forUserId)
        {
            where.Add("c.id IN (SELECT ed.customer_id FROM employee_details ed WHERE ed.deleted_at IS NULL AND ed.user_id = @for_user_id)");
            parameters.Add(("@for_user_id", forUserId));
        }
        else
        {
            var access = await AssignedCustomerAccess(CurrentUserId(), cancellationToken, includeHrAndHo: false);
            if (!access.AllAccess)
            {
                where.Add(access.UserIds.Count == 0
                    ? "1 = 0"
                    : $"c.id IN (SELECT ed.customer_id FROM employee_details ed WHERE ed.deleted_at IS NULL AND ed.user_id IN ({string.Join(',', access.UserIds)}))");
            }
        }

        return (string.Join(" AND ", where), parameters);
    }

    private async Task<(string Where, List<(string, object?)> Parameters)> CustomerSecondaryWhere(string type, CancellationToken cancellationToken, ulong? id = null)
    {
        var parameters = BaseParameters();
        parameters.Add(("@fallback_type", type));
        var where = new List<string> { "c.deleted_at IS NULL", "c.active = 'Y'" };
        if (id.HasValue)
        {
            where.Add("c.id = @customer_id");
            parameters.Add(("@customer_id", id.Value));
        }

        if (string.Equals(type, "RETAILER", StringComparison.OrdinalIgnoreCase))
        {
            where.Add("(ctype.customertype_name LIKE '%Retailer%' OR ctype.type_name LIKE '%Retailer%' OR ((c.customertype IS NULL OR c.customertype NOT IN (1,3)) AND COALESCE(ctype.customertype_name, '') NOT LIKE '%Distributor%' AND COALESCE(ctype.type_name, '') NOT LIKE '%Distributor%'))");
        }
        else
        {
            where.Add("(ctype.customertype_name LIKE @type_search OR ctype.type_name LIKE @type_search)");
            parameters.Add(("@type_search", "%" + type + "%"));
        }

        var search = Request.Query["global_search"].ToString();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(c.name LIKE @search OR c.mobile LIKE @search OR c.email LIKE @search OR c.customer_code LIKE @search OR c.sap_code LIKE @search)");
            parameters.Add(("@search", "%" + search.Trim() + "%"));
        }

        var cityName = Request.Query["city_name"].ToString();
        if (!string.IsNullOrWhiteSpace(cityName))
        {
            where.Add("city.city_name LIKE @city_name");
            parameters.Add(("@city_name", "%" + cityName.Trim() + "%"));
        }

        var status = Request.Query["status"].ToString();
        if (!string.IsNullOrWhiteSpace(status))
        {
            where.Add("cd.visit_status = @status");
            parameters.Add(("@status", status.Trim()));
        }

        if (ULongQuery("for_user_id") is { } forUserId)
        {
            where.Add("c.id IN (SELECT ed.customer_id FROM employee_details ed WHERE ed.deleted_at IS NULL AND ed.user_id = @for_user_id)");
            parameters.Add(("@for_user_id", forUserId));
        }
        else
        {
            var access = await AssignedCustomerAccess(CurrentUserId(), cancellationToken, includeHrAndHo: true);
            if (!access.AllAccess)
            {
                where.Add(access.UserIds.Count == 0
                    ? "1 = 0"
                    : $"c.id IN (SELECT ed.customer_id FROM employee_details ed WHERE ed.deleted_at IS NULL AND ed.user_id IN ({string.Join(',', access.UserIds)}))");
            }
        }

        return (string.Join(" AND ", where), parameters);
    }

    private async Task<List<ulong>> VisibleUserIds(ulong userId, CancellationToken cancellationToken)
    {
        var rows = await QueryRows("SELECT id, reportingid FROM users WHERE deleted_at IS NULL", cancellationToken);
        var visible = new HashSet<ulong> { userId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var row in rows)
            {
                var id = ULong(row, "id");
                var reportingId = ULong(row, "reportingid");
                if (reportingId > 0 && visible.Contains(reportingId) && visible.Add(id)) changed = true;
            }
        }
        return visible.ToList();
    }

    private async Task<List<ulong>> VisibleHierarchyUserIds(ulong userId, CancellationToken cancellationToken)
    {
        if (await HasAnyRole(userId, cancellationToken, "superadmin", "subAdmin", "Sub_Admin"))
        {
            return await NonCustomerUserIds(null, cancellationToken);
        }

        if (await HasAnyRole(userId, cancellationToken, "BM."))
        {
            var user = (await QueryRows("SELECT branch_id FROM users WHERE id = @user_id AND deleted_at IS NULL", cancellationToken, ("@user_id", userId))).FirstOrDefault();
            var branchId = user is null ? null : Obj(user, "branch_id");
            return await NonCustomerUserIds(branchId, cancellationToken);
        }

        var rows = await QueryRows(@"SELECT u.id, u.reportingid
FROM users u
WHERE u.deleted_at IS NULL
AND NOT EXISTS (
    SELECT 1 FROM model_has_roles m
    INNER JOIN roles r ON r.id = m.role_id
    WHERE m.model_id = u.id
    AND (m.role_id = 61 OR r.name = 'Distributor')
)", cancellationToken);
        var visible = new HashSet<ulong> { userId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var row in rows)
            {
                var id = ULong(row, "id");
                var reportingId = ULong(row, "reportingid");
                if (reportingId > 0 && visible.Contains(reportingId) && visible.Add(id)) changed = true;
            }
        }

        return visible.ToList();
    }

    private async Task<(bool AllAccess, List<ulong> UserIds)> SecondaryCustomerAccess(ulong userId, CancellationToken cancellationToken)
    {
        if (await HasAnyRole(userId, cancellationToken, "superadmin", "subAdmin", "Sub_Admin"))
        {
            return (true, await NonCustomerUserIds(null, cancellationToken));
        }

        return (false, await VisibleHierarchyUserIds(userId, cancellationToken));
    }

    private async Task<(bool AllAccess, List<ulong> UserIds)> AssignedCustomerAccess(ulong userId, CancellationToken cancellationToken, bool includeHrAndHo)
    {
        var allAccessRoles = includeHrAndHo
            ? new[] { "superadmin", "Admin", "Sub_Admin", "subAdmin", "HR_Admin", "HO_Account" }
            : new[] { "superadmin", "Admin", "Sub_Admin", "subAdmin" };

        if (await HasAnyRole(userId, cancellationToken, allAccessRoles))
        {
            return (true, await NonCustomerUserIds(null, cancellationToken));
        }

        return (false, await VisibleHierarchyUserIds(userId, cancellationToken));
    }

    private async Task<List<ulong>> NonCustomerUserIds(object? branchId, CancellationToken cancellationToken)
    {
        var hasBranch = branchId is not null and not DBNull;
        var where = hasBranch ? "AND u.branch_id = @branch_id" : string.Empty;
        var parameters = hasBranch
            ? new (string, object?)[] { ("@branch_id", branchId) }
            : Array.Empty<(string, object?)>();
        var rows = await QueryRows($@"SELECT u.id
FROM users u
WHERE u.deleted_at IS NULL
{where}
AND NOT EXISTS (
    SELECT 1 FROM model_has_roles m
    INNER JOIN roles r ON r.id = m.role_id
    WHERE m.model_id = u.id
    AND (m.role_id = 61 OR r.name = 'Distributor')
)
ORDER BY u.name ASC", cancellationToken, parameters);
        return rows.Select(row => ULong(row, "id")).Where(id => id > 0).ToList();
    }

    private async Task<bool> HasAnyRole(ulong userId, CancellationToken cancellationToken, params string[] roles)
    {
        if (roles.Length == 0) return false;
        var quoted = string.Join(',', roles.Select(role => $"'{role.Replace("'", "''")}'"));
        return await QueryScalarLong($@"SELECT COUNT(*)
FROM model_has_roles m
INNER JOIN roles r ON r.id = m.role_id
WHERE m.model_id = @user_id
AND r.name IN ({quoted})", cancellationToken, ("@user_id", userId)) > 0;
    }

    private async Task<List<ulong>> FilterUsersByAssignedCustomerType(List<ulong> userIds, string? type, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0 || string.IsNullOrWhiteSpace(type)) return userIds;

        var ids = string.Join(',', userIds.Distinct());
        var normalizedType = type.Trim();
        var customerTypePredicate = normalizedType.Equals("Distributor", StringComparison.OrdinalIgnoreCase)
            ? "(c.customertype IN (1,3) OR ctype.customertype_name LIKE '%Distributor%' OR ctype.type_name LIKE '%Distributor%' OR ctype.type_name = 'Dealer')"
            : "(ctype.customertype_name LIKE @type_search OR ctype.type_name LIKE @type_search OR (c.customertype NOT IN (1,3) AND COALESCE(ctype.customertype_name, '') NOT LIKE '%Distributor%' AND COALESCE(ctype.type_name, '') NOT LIKE '%Distributor%'))";

        var rows = await QueryRows($@"SELECT DISTINCT ed.user_id
FROM employee_details ed
INNER JOIN customers c ON c.id = ed.customer_id AND c.deleted_at IS NULL AND c.active = 'Y'
LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL
WHERE ed.deleted_at IS NULL
AND ed.user_id IN ({ids})
AND {customerTypePredicate}", cancellationToken,
            ("@type_search", "%" + normalizedType + "%"));

        var filtered = rows.Select(row => ULong(row, "user_id")).Where(id => id > 0).Distinct().ToHashSet();
        return userIds.Where(filtered.Contains).ToList();
    }

    private List<(string, object?)> BaseParameters() => [("@auth_user", CurrentUserId()), ("@today", IndiaNow().Date)];

    private Dictionary<string, object?> DistributorDetails(Dictionary<string, object?> row) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = ULong(row, "id"),
        ["legal_name"] = FirstNonEmpty(Str(row, "legal_name"), Str(row, "trade_name")),
        ["shop_image"] = FirstNonEmpty(Str(row, "shop_image"), Str(row, "profile_image")),
        ["billing_address"] = FirstNonEmpty(Str(row, "billing_address"), Str(row, "address_line")),
        ["billing_city"] = FirstNonEmpty(Str(row, "billing_city_name"), Str(row, "billing_city")),
        ["contact_person"] = Str(row, "contact_person"),
        ["mobile"] = Str(row, "mobile"),
        ["billing_pincode"] = FirstNonEmpty(Str(row, "billing_pincode_value"), Str(row, "billing_pincode")),
        ["registration_type"] = FirstNonEmpty(Str(row, "registration_type"), "Distributor"),
        ["gps_location"] = Str(row, "gps_location"),
        ["gst_number"] = Str(row, "gst_number"),
        ["pan_number"] = Str(row, "pan_number"),
        ["bank_account_type"] = Str(row, "bank_account_type"),
        ["bank_account_number"] = FirstNonEmpty(Str(row, "bank_account_number"), Str(row, "account_number")),
        ["bank_name"] = Str(row, "bank_name"),
        ["ifsc_code"] = FirstNonEmpty(Str(row, "ifsc_code"), Str(row, "ifsc")),
        ["account_holder_name"] = FirstNonEmpty(Str(row, "account_holder_name"), Str(row, "account_holder"))
    };

    private Dictionary<string, object?> RetailerDetails(Dictionary<string, object?> row) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = ULong(row, "id"),
        ["shop_photo"] = MobileStoragePath(FirstNonEmpty(Str(row, "shop_photo"), Str(row, "shop_image"))),
        ["shop_name"] = Str(row, "shop_name"),
        ["address_line"] = Str(row, "address_line"),
        ["owner_name"] = Str(row, "owner_name"),
        ["mobile_number"] = FirstNonEmpty(Str(row, "mobile_number"), Str(row, "mobile")),
        ["type"] = Str(row, "type"),
        ["sub_type"] = Str(row, "sub_type"),
        ["status"] = FirstNonEmpty(Str(row, "status"), "PENDING"),
        ["gps_location"] = Str(row, "gps_location"),
        ["gst_number"] = Str(row, "gst_number"),
        ["gst_attachment"] = MobileStoragePath(Str(row, "gst_attachment")),
        ["pan_number"] = Str(row, "pan_number"),
        ["pan_attachment"] = MobileStoragePath(Str(row, "pan_attachment")),
        ["remark"] = Str(row, "remark"),
        ["country_id"] = Str(row, "country_id"),
        ["state_id"] = Str(row, "state_id"),
        ["district_id"] = Str(row, "district_id"),
        ["city_id"] = Str(row, "city_id"),
        ["pincode_id"] = Str(row, "pincode_id"),
        ["distributor_name"] = Str(row, "distributor_name"),
        ["agri_distributor"] = Str(row, "agri_distributor"),
        ["beat_id"] = Str(row, "beat_id"),
        ["belt_area_market_name"] = Str(row, "belt_area_market_name"),
        ["bank_account_type"] = Str(row, "bank_account_type"),
        ["bank_account_number"] = Str(row, "bank_account_number"),
        ["bank_name"] = Str(row, "bank_name"),
        ["ifsc_code"] = Str(row, "ifsc_code"),
        ["account_holder_name"] = Str(row, "account_holder_name"),
        ["bank_proof"] = MobileStoragePath(Str(row, "bank_proof")),
        ["saathi_awareness_status"] = Str(row, "saathi_awareness_status"),
        ["state"] = new { state_name = Str(row, "state_name") },
        ["district"] = new { district_name = Str(row, "district_name") },
        ["city"] = new { city_name = Str(row, "city_name") },
        ["pincode"] = new { pincode = FirstNonEmpty(Str(row, "pincode_value"), Str(row, "pincode")) },
        ["beat"] = new { beat_name = Str(row, "beat_name") },
        ["creator"] = new { name = Str(row, "creator_name") }
    };

    private async Task<IReadOnlyList<object>> LinkedDistributors(Dictionary<string, object?> row, CancellationToken cancellationToken)
    {
        var rawValues = new[] { Str(row, "distributor_name"), Str(row, "agri_distributor") }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var ids = ParseIds(string.Join(',', rawValues));
        var named = rawValues
            .Where(value => ParseIds(value).Count == 0)
            .Select(value => new { shop_name = value })
            .Cast<object>()
            .ToList();
        if (ids.Count == 0) return named;

        var idList = string.Join(',', ids);
        var customerRows = await QueryRows($@"SELECT id, name AS shop_name FROM customers WHERE id IN ({idList}) AND deleted_at IS NULL", cancellationToken);
        return named.Concat(customerRows
            .GroupBy(x => ULong(x, "id"))
            .Select(group => new { id = group.Key, shop_name = Str(group.First(), "shop_name") })
            .Cast<object>())
            .ToList();
    }

    private async Task<IReadOnlyDictionary<string, string>> NormalizeSecondaryDistributorLinks(IReadOnlyDictionary<string, string> body, CancellationToken cancellationToken)
    {
        var normalized = new Dictionary<string, string>(body, StringComparer.OrdinalIgnoreCase);
        foreach (var key in new[] { "distributor_name", "agri_distributor" })
        {
            if (!body.TryGetValue(key, out var rawValue)) continue;
            var value = rawValue?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                normalized[key] = string.Empty;
                continue;
            }

            var ids = ParseIds(value);
            if (ids.Count == 0)
            {
                throw new ArgumentException($"{DistributorFieldLabel(key)} must be a valid distributor id.");
            }

            var idList = string.Join(',', ids);
            var rows = await QueryRows($@"SELECT c.id
FROM customers c
LEFT JOIN customer_types ctype ON ctype.id = c.customertype AND ctype.deleted_at IS NULL
WHERE c.deleted_at IS NULL
AND c.id IN ({idList})
AND (c.customertype IN (1,3) OR ctype.customertype_name LIKE '%Distributor%' OR ctype.type_name LIKE '%Distributor%')", cancellationToken);
            var found = rows.Select(row => ULong(row, "id")).Where(id => id > 0).ToHashSet();
            var missing = ids.Where(id => !found.Contains(id)).ToArray();
            if (missing.Length > 0)
            {
                throw new ArgumentException($"{DistributorFieldLabel(key)} is invalid.");
            }

            normalized[key] = string.Join(',', ids);
        }

        return normalized;
    }

    private async Task<object> CheckStatus(string entityType, ulong entityId, CancellationToken cancellationToken)
    {
        var row = (await QueryRows(@"SELECT id, checkin_date, checkin_time, checkout_date, checkout_time
FROM check_in
WHERE user_id = @user_id
AND deleted_at IS NULL
AND ((entity_type = @entity_type AND entity_id = @entity_id) OR customer_id = @entity_id)
ORDER BY checkin_date DESC, checkin_time DESC, id DESC
LIMIT 1", cancellationToken,
            ("@user_id", CurrentUserId()), ("@entity_type", entityType), ("@entity_id", entityId))).FirstOrDefault();

        return new
        {
            last_checkin = new
            {
                checkin_id = row is null ? (ulong?)null : ULong(row, "id"),
                checkin_datetime = row is null ? null : DateTimeString(row, "checkin_date", "checkin_time")
            },
            last_checkout = new
            {
                checkout_datetime = row is null ? null : DateTimeString(row, "checkout_date", "checkout_time")
            }
        };
    }

    private Task<int> HierarchyLevel(CancellationToken cancellationToken) => Task.FromResult(4);

    private async Task<Dictionary<string, string>> RequestValues(CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var query in Request.Query) values[query.Key] = query.Value.ToString();
        if (Request.HasFormContentType)
        {
            foreach (var form in Request.Form) values[form.Key] = form.Value.ToString();
            return values;
        }
        if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                values[property.Name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString();
            }
        }
        return values;
    }

    private async Task<string?> SaveFormFile(string? fieldName, string folder, CancellationToken cancellationToken, string? storageFolder = null)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || !Request.HasFormContentType) return null;
        var file = Request.Form.Files[fieldName];
        if (file is null || file.Length == 0) return null;
        return await SaveFile(file, folder, cancellationToken, storageFolder);
    }

    private async Task<List<string>> SaveFormFiles(string fieldName, string folder, CancellationToken cancellationToken, string? storageFolder = null)
    {
        if (!Request.HasFormContentType) return [];
        var files = Request.Form.Files.GetFiles(fieldName);
        var saved = new List<string>();
        foreach (var file in files)
        {
            if (file.Length > 0) saved.Add(await SaveFile(file, folder, cancellationToken, storageFolder));
        }
        return saved;
    }

    private async Task<string> SaveFile(IFormFile file, string folder, CancellationToken cancellationToken, string? storageFolder = null)
    {
        if (folder is "profile-images" or "shop-photos" && !IsImageFile(file))
        {
            throw new ArgumentException("Only image files are allowed.");
        }
        if (folder is "gst-attachments" or "pan-attachments" or "bank-proofs" or "documents" && !IsPdfOrImageFile(file))
        {
            throw new ArgumentException("Only PDF and image files are allowed.");
        }

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        storageFolder = FirstNonEmpty(storageFolder) ?? "secondary_customers";
        var uploadRoot = Path.Combine(webRoot, "public", "storage", storageFolder);
        Directory.CreateDirectory(uploadRoot);
        var fullPath = Path.Combine(uploadRoot, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }
        var storageRoot = Path.Combine(webRoot, "storage", storageFolder);
        Directory.CreateDirectory(storageRoot);
        System.IO.File.Copy(fullPath, Path.Combine(storageRoot, fileName), true);
        return $"storage/{storageFolder}/{fileName}";
    }

    private static bool IsImageFile(IFormFile file)
    {
        if (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return true;
        return Path.GetExtension(file.FileName).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp";
    }

    private static bool IsPdfOrImageFile(IFormFile file)
    {
        if (IsImageFile(file)) return true;
        if (string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return true;
        return Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult ListResponse(List<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return Ok(new { status = "error", message = "No Record Found.", data = rows });
        return Ok(new { status = "success", message = "Data retrieved successfully.", data = rows });
    }

    private static object ValidationError(string message) => new
    {
        status = "error",
        message = "Validation failed",
        errors = new { general = new[] { message } }
    };

    private static object Paginator(List<Dictionary<string, object?>> data, int page, int perPage, long total) => new
    {
        current_page = page,
        data,
        from = data.Count == 0 ? null : (int?)((page - 1) * perPage + 1),
        to = data.Count == 0 ? null : (int?)((page - 1) * perPage + data.Count),
        per_page = perPage,
        total,
        last_page = total == 0 ? 1 : (long)Math.Ceiling(total / (double)perPage)
    };

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

    private async Task<Dictionary<string, object?>> ExistingCustomFields(ulong customerId, CancellationToken cancellationToken)
    {
        var row = (await QueryRows("SELECT custom_fields FROM customers WHERE id = @id LIMIT 1", cancellationToken, ("@id", customerId))).FirstOrDefault();
        var json = row is null ? null : Str(row, "custom_fields");
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var document = JsonDocument.Parse(json);
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                values[property.Name] = JsonValue(property.Value);
            }
            return values;
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task<long> QueryScalarLong(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? 0 : Convert.ToInt64(value, CultureInfo.InvariantCulture);
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

    private int Page() => Math.Max(1, (int)(ULongQuery("page") ?? 1));
    private int PerPage(int fallback = 20) => Math.Clamp((int)(ULongQuery("per_page") ?? ULongQuery("pageSize") ?? (ulong)fallback), 1, 500);
    private ulong? ULongQuery(string key) => ulong.TryParse(Request.Query[key].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    private static ulong? ULongValue(IReadOnlyDictionary<string, string> body, string key) => ulong.TryParse(Value(body, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    private static int IntValue(IReadOnlyDictionary<string, string> body, string key) => int.TryParse(Value(body, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    private static decimal DecimalValue(IReadOnlyDictionary<string, string> body, string key) => decimal.TryParse(Value(body, key), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0m;
    private static bool BoolValue(IReadOnlyDictionary<string, string> body, string key) => Value(body, key) is "1" or "true" or "True" or "Y";
    private static string? Value(IReadOnlyDictionary<string, string> body, string key) => body.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string DistributorFieldLabel(string key) => key.Equals("agri_distributor", StringComparison.OrdinalIgnoreCase) ? "agri_distributor" : "distributor_name";
    private static string NormalizeMobile(string? value) => FirstNonEmpty(value)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;
    private static (string FirstName, string LastName) SplitName(string? value)
    {
        var text = FirstNonEmpty(value) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return (string.Empty, string.Empty);
        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
    }

    private static (string? Latitude, string? Longitude) SplitGps(string? value)
    {
        var parts = (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? (parts[0], parts[1]) : (null, null);
    }

    private static string BuildCustomFields(IReadOnlyDictionary<string, object?> existing, IReadOnlyDictionary<string, string> body, IReadOnlyDictionary<string, object?> extras)
    {
        var values = new Dictionary<string, object?>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var item in body) values[item.Key] = item.Value;
        foreach (var item in extras.Where(item => item.Value is not null)) values[item.Key] = item.Value;
        return JsonSerializer.Serialize(values);
    }

    private static object? JsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => value.EnumerateArray().Select(JsonValue).ToArray(),
        JsonValueKind.Object => value.EnumerateObject().ToDictionary(property => property.Name, property => JsonValue(property.Value), StringComparer.OrdinalIgnoreCase),
        _ => null
    };

    private static string MobileStoragePath(string? path)
    {
        var value = FirstNonEmpty(path);
        if (value is null) return string.Empty;
        if (Uri.TryCreate(value, UriKind.Absolute, out _)) return value;
        value = value.Replace("\\", "/", StringComparison.Ordinal).TrimStart('/');
        if (value.StartsWith("public/storage/", StringComparison.OrdinalIgnoreCase)) return value["public/".Length..];
        if (value.StartsWith("storage/", StringComparison.OrdinalIgnoreCase)) return value;
        return $"storage/{value}";
    }

    private static DateTime? ParseDate(string? value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ? date.Date : null;
    private static List<ulong> ParseIds(string? csv) => (csv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => ulong.TryParse(x, out var id) ? id : 0).Where(id => id > 0).Distinct().ToList();
    private static Dictionary<string, object?> CleanRow(Dictionary<string, object?> row) => row.ToDictionary(pair => pair.Key, pair => pair.Value is DateTime date ? date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : pair.Value, StringComparer.OrdinalIgnoreCase);
    private static object? Obj(Dictionary<string, object?> row, string key) => row.TryGetValue(key, out var value) && value is not DBNull ? value : null;
    private static string Str(Dictionary<string, object?> row, string key) => Convert.ToString(Obj(row, key), CultureInfo.InvariantCulture) ?? string.Empty;
    private static ulong ULong(Dictionary<string, object?> row, string key) => Obj(row, key) is null ? 0 : Convert.ToUInt64(Obj(row, key), CultureInfo.InvariantCulture);
    private static string? DateTimeString(Dictionary<string, object?> row, string dateKey, string timeKey)
    {
        var date = Obj(row, dateKey);
        var time = Obj(row, timeKey);
        if (date is null || time is null) return null;
        var datePart = Convert.ToDateTime(date, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var timePart = time is TimeSpan span
            ? span.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
            : Convert.ToDateTime(time, CultureInfo.InvariantCulture).ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return $"{datePart} {timePart}";
    }
    private static DateTime IndiaNow() => DateTime.UtcNow.AddHours(5).AddMinutes(30);
    private ulong CurrentUserId() => ulong.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new InvalidOperationException("Unauthenticated.");
    private static string ExceptionMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException is not null) current = current.InnerException;
        return current.Message;
    }

    private enum CustomerEndpointKind
    {
        MasterDistributor,
        SecondaryCustomer
    }
}
