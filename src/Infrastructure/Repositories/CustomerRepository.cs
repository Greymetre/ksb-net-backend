using System.Data;
using System.Text.Json;
using Application.DTOs.Customers;
using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private const int MaxRows = 50000;
    private const ulong DistributorCustomerType = 1;
    private const string DistributorRoleName = "Distributor";
    private const string GuardName = "users";
    private static readonly string[] DistributorPermissions =
    [
        "dashboard_access",
        "scheme_access",
        "new_invoice_access",
        "new_invoice_create"
    ];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CustomerDto>> GetCustomersAsync(CustomerListFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _dbContext.Customers.AsNoTracking().Where(x => x.DeletedAt == null);

        if (filter.CustomerType.HasValue) query = query.Where(x => x.CustomerType == filter.CustomerType);
        if (!string.IsNullOrWhiteSpace(filter.Active)) query = query.Where(x => x.Active == NormalizeActive(filter.Active));
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x =>
                x.Name.Contains(search)
                || (x.Mobile != null && x.Mobile.Contains(search))
                || (x.Email != null && x.Email.Contains(search))
                || x.CustomerCode.Contains(search));
        }

        var rows = await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .Select(x => new
            {
                Customer = x,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                ParentName = _dbContext.Customers.Where(parent => parent.Id == x.ParentId).Select(parent => parent.Name).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var customers = rows.Select(row => ToCustomerDto(row.Customer, row.CreatedByName, row.ParentName)).ToList();
        await AttachAddressFallbackAsync(customers, cancellationToken);
        customers = ApplyJsonFilters(customers, filter).ToList();
        await AttachAddressNamesAsync(customers, cancellationToken);
        await AttachLookupNamesAsync(customers, cancellationToken);
        return customers;
    }

    public async Task<CustomerDto?> GetCustomerAsync(ulong id, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Customers.AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new
            {
                Customer = x,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                ParentName = _dbContext.Customers.Where(parent => parent.Id == x.ParentId).Select(parent => parent.Name).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) return null;
        var dto = ToCustomerDto(row.Customer, row.CreatedByName, row.ParentName);
        await AttachAddressFallbackAsync([dto], cancellationToken);
        await AttachAddressNamesAsync([dto], cancellationToken);
        await AttachLookupNamesAsync([dto], cancellationToken);
        await AttachPointSummaryAsync(dto, row.Customer, cancellationToken);
        return dto;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CustomerRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var assignedUserIds = AssignedUserIdsOrCreator(request.AssignedUserIds, actorUserId);
        var customer = new Customer
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            Name = request.Name!.Trim(),
            FirstName = NormalizeText(request.FirstName) ?? string.Empty,
            LastName = NormalizeText(request.LastName) ?? string.Empty,
            Mobile = NormalizeText(request.Mobile),
            ContactNumber = NormalizeText(request.ContactNumber),
            Email = NormalizeText(request.Email),
            ProfileImage = NormalizeText(request.ProfileImage) ?? string.Empty,
            ShopImage = NormalizeText(request.ShopImage),
            CustomerCode = NormalizeText(request.CustomerCode) ?? NormalizeText(ReadField(request.CustomFields, "distributor_code")) ?? string.Empty,
            CustomerType = request.CustomerType,
            FirmType = request.FirmType,
            ParentId = request.ParentId,
            SapCode = NormalizeText(request.SapCode),
            ManagerName = NormalizeText(request.ManagerName) ?? string.Empty,
            ManagerPhone = NormalizeText(request.ManagerPhone) ?? string.Empty,
            ExecutiveId = FirstAssignedUserId(assignedUserIds),
            CustomFields = SerializeFields(request.CustomFields),
            CreatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _dbContext.Customers.AddAsync(customer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncCustomerRelatedTablesAsync(customer.Id, request.CustomFields, actorUserId, cancellationToken);
        await SyncCustomerAssignmentsAsync(customer.Id, assignedUserIds, actorUserId, cancellationToken);
        return ToCustomerDto(customer, null, null);
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(ulong id, CustomerRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);
        if (customer is null) return null;

        if (!string.IsNullOrWhiteSpace(request.Name)) customer.Name = request.Name.Trim();
        if (request.FirstName is not null) customer.FirstName = NormalizeText(request.FirstName) ?? string.Empty;
        if (request.LastName is not null) customer.LastName = NormalizeText(request.LastName) ?? string.Empty;
        if (request.Mobile is not null) customer.Mobile = NormalizeText(request.Mobile);
        if (request.ContactNumber is not null) customer.ContactNumber = NormalizeText(request.ContactNumber);
        if (request.Email is not null) customer.Email = NormalizeText(request.Email);
        if (request.ProfileImage is not null) customer.ProfileImage = NormalizeText(request.ProfileImage) ?? string.Empty;
        if (request.ShopImage is not null) customer.ShopImage = NormalizeText(request.ShopImage);
        if (request.CustomerCode is not null) customer.CustomerCode = NormalizeText(request.CustomerCode) ?? string.Empty;
        if (request.CustomerType.HasValue) customer.CustomerType = request.CustomerType;
        if (request.FirmType.HasValue) customer.FirmType = request.FirmType;
        if (request.ParentId.HasValue) customer.ParentId = request.ParentId;
        if (request.SapCode is not null) customer.SapCode = NormalizeText(request.SapCode);
        if (request.ManagerName is not null) customer.ManagerName = NormalizeText(request.ManagerName) ?? string.Empty;
        if (request.ManagerPhone is not null) customer.ManagerPhone = NormalizeText(request.ManagerPhone) ?? string.Empty;
        if (request.AssignedUserIds is not null && request.AssignedUserIds.Count > 0) customer.ExecutiveId = FirstAssignedUserId(request.AssignedUserIds);
        if (request.CustomFields is not null) customer.CustomFields = SerializeFields(request.CustomFields);

        var active = NormalizeActive(request.Active);
        if (active is not null) customer.Active = active;
        customer.UpdatedBy = actorUserId;
        customer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncCustomerRelatedTablesAsync(customer.Id, request.CustomFields, actorUserId, cancellationToken);
        await SyncCustomerAssignmentsAsync(customer.Id, request.AssignedUserIds, actorUserId, cancellationToken);
        return ToCustomerDto(customer, null, null);
    }

    public async Task<CustomerDto?> UpdateKycStatusAsync(ulong id, string documentKey, string status, string? remark, ulong actorUserId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);
        if (customer is null) return null;

        var fields = DeserializeFields(customer.CustomFields);
        var attachmentKey = KycAttachmentKey(documentKey);
        if (!fields.TryGetValue(attachmentKey, out var attachment) || string.IsNullOrWhiteSpace(attachment))
        {
            throw new InvalidOperationException("KYC document is not uploaded.");
        }

        var approverName = await _dbContext.Users.AsNoTracking()
            .Where(x => x.Id == actorUserId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var prefix = $"{documentKey}_kyc";
        fields[$"{prefix}_status"] = status;
        fields[$"{prefix}_remark"] = NormalizeText(remark);
        fields[$"{prefix}_action_by"] = actorUserId.ToString();
        fields[$"{prefix}_action_by_name"] = NormalizeText(approverName);
        fields[$"{prefix}_action_at"] = DateTime.UtcNow.ToString("O");

        customer.CustomFields = SerializeFields(fields);
        customer.UpdatedBy = actorUserId;
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = ToCustomerDto(customer, null, null);
        await AttachAddressNamesAsync([dto], cancellationToken);
        await AttachPointSummaryAsync(dto, customer, cancellationToken);
        return dto;
    }

    public async Task<CustomerDto?> SetRetailerApprovalStatusAsync(ulong id, string status, string? remark, ulong actorUserId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);
        if (customer is null || !IsRetailerCustomer(customer)) return null;

        var fields = DeserializeFields(customer.CustomFields);
        fields["status"] = status;
        fields["remark"] = NormalizeText(remark);
        fields["approve_reject_by"] = actorUserId.ToString();
        fields["status_updated_at"] = DateTime.UtcNow.ToString("O");

        customer.CustomFields = SerializeFields(fields);
        customer.UpdatedBy = actorUserId;
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpsertRetailerApprovalStatusAsync(customer.Id, status, cancellationToken);

        var dto = ToCustomerDto(customer, null, null);
        await AttachAddressFallbackAsync([dto], cancellationToken);
        await AttachAddressNamesAsync([dto], cancellationToken);
        await AttachLookupNamesAsync([dto], cancellationToken);
        await AttachPointSummaryAsync(dto, customer, cancellationToken);
        return dto;
    }

    public async Task<CustomerDto?> SetCustomerActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);
        if (customer is null) return null;

        customer.Active = NormalizeActive(active) ?? ToggleActive(customer.Active);
        customer.UpdatedBy = actorUserId;
        customer.UpdatedAt = DateTime.UtcNow;
        await SyncLinkedUserActiveAsync(customer.Id, customer.Active, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToCustomerDto(customer, null, null);
    }

    public async Task<bool> DeleteCustomerAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);
        if (customer is null) return false;

        customer.Active = "N";
        customer.DeletedAt = DateTime.UtcNow;
        customer.UpdatedBy = actorUserId;
        customer.UpdatedAt = DateTime.UtcNow;
        await SyncLinkedUserActiveAsync(customer.Id, "N", cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task EnsureDistributorLoginUserAsync(ulong customerId, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == customerId && x.DeletedAt == null, cancellationToken);

        if (customer?.CustomerType != DistributorCustomerType) return;

        var mobile = NormalizeMobile(customer.Mobile ?? FirstMobile(ReadCustomerField(customer, "mobile_numbers")));
        if (string.IsNullOrWhiteSpace(mobile) || mobile.Length > 11) return;

        var email = NormalizeText(customer.Email) ?? $"customer{customer.Id}@gmail.com";
        var name = FirstNonBlank(
            ReadCustomerField(customer, "contact_person"),
            ReadCustomerField(customer, "trade_name"),
            ReadCustomerField(customer, "legal_name"),
            customer.Name) ?? $"Distributor {customer.Id}";
        var (firstName, lastName) = SplitName(name);
        var role = await EnsureDistributorRoleAsync(cancellationToken);

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.CustomerId == customer.Id, cancellationToken);

        if (user is null)
        {
            var emailUser = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

            if (emailUser is not null)
            {
                if (emailUser.CustomerId != customer.Id) return;
                user = emailUser;
            }
        }

        var mobileOwner = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Mobile == mobile && (user == null || x.Id != user.Id), cancellationToken);
        if (mobileOwner is not null) return;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(mobile);
        var now = DateTime.UtcNow;

        if (user is null)
        {
            user = new User
            {
                Active = NormalizeActive(customer.Active) ?? "Y",
                Name = name,
                FirstName = firstName,
                LastName = lastName,
                Mobile = mobile,
                Email = email,
                Password = passwordHash,
                PasswordString = mobile,
                ReportingId = actorUserId,
                CustomerId = customer.Id,
                CreatedBy = actorUserId,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            user.Active = NormalizeActive(customer.Active) ?? user.Active;
            user.Name = name;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Mobile = mobile;
            user.Email = email;
            user.Password = passwordHash;
            user.PasswordString = mobile;
            user.CustomerId = customer.Id;
            user.UpdatedAt = now;
            if (!user.ReportingId.HasValue) user.ReportingId = actorUserId;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var hasRole = await _dbContext.ModelHasRoles.AnyAsync(
            x => x.ModelId == user.Id && x.ModelType == LaravelModelTypes.User && x.RoleId == role.Id,
            cancellationToken);

        if (!hasRole)
        {
            await _dbContext.ModelHasRoles.AddAsync(new ModelHasRole
            {
                RoleId = role.Id,
                ModelId = user.Id,
                ModelType = LaravelModelTypes.User
            }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> MobileExistsAsync(string mobile, ulong? exceptId, CancellationToken cancellationToken) =>
        await _dbContext.Customers.AnyAsync(x => x.DeletedAt == null && x.Mobile == mobile && (!exceptId.HasValue || x.Id != exceptId), cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, ulong? exceptId, CancellationToken cancellationToken) =>
        await _dbContext.Customers.AnyAsync(x => x.DeletedAt == null && x.Email == email && (!exceptId.HasValue || x.Id != exceptId), cancellationToken);

    private static ulong? FirstAssignedUserId(IReadOnlyCollection<ulong>? userIds)
    {
        var userId = userIds?.FirstOrDefault(id => id > 0) ?? 0;
        return userId > 0 ? userId : null;
    }

    private static IReadOnlyCollection<ulong>? AssignedUserIdsOrCreator(IReadOnlyCollection<ulong>? assignedUserIds, ulong? actorUserId)
    {
        var ids = assignedUserIds?.Where(id => id > 0).Distinct().ToArray() ?? [];
        if (ids.Length > 0) return ids;
        return actorUserId.HasValue && actorUserId.Value > 0 ? [actorUserId.Value] : assignedUserIds;
    }

    private async Task SyncCustomerAssignmentsAsync(ulong customerId, IReadOnlyCollection<ulong>? assignedUserIds, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (assignedUserIds is null) return;

        var userIds = assignedUserIds.Where(id => id > 0).Distinct().ToArray();
        if (userIds.Length == 0) return;

        var idsCsv = string.Join(',', userIds);
        await _dbContext.Database.ExecuteSqlRawAsync(
            $@"UPDATE employee_details
SET deleted_at = UTC_TIMESTAMP(), updated_by = {{0}}, updated_at = UTC_TIMESTAMP()
WHERE customer_id = {{1}} AND deleted_at IS NULL AND (user_id IS NULL OR user_id NOT IN ({idsCsv}))",
            [actorUserId, customerId],
            cancellationToken);

        foreach (var userId in userIds)
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"UPDATE employee_details
SET active = 'Y', deleted_at = NULL, updated_by = {2}, updated_at = UTC_TIMESTAMP()
WHERE customer_id = {0} AND user_id = {1} AND deleted_at IS NOT NULL",
                [customerId, userId, actorUserId],
                cancellationToken);

            await _dbContext.Database.ExecuteSqlRawAsync(
                @"INSERT INTO employee_details (active, customer_id, user_id, created_by, created_at, updated_at)
SELECT 'Y', {0}, {1}, {2}, UTC_TIMESTAMP(), UTC_TIMESTAMP()
WHERE EXISTS (SELECT 1 FROM users WHERE id = {1} AND deleted_at IS NULL)
AND NOT EXISTS (
    SELECT 1 FROM employee_details
    WHERE customer_id = {0} AND user_id = {1} AND deleted_at IS NULL
)",
                [customerId, userId, actorUserId],
                cancellationToken);
        }
    }

    private async Task SyncCustomerRelatedTablesAsync(ulong customerId, IReadOnlyDictionary<string, string?>? fields, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (fields is null) return;
        await SyncCustomerAddressAsync(customerId, fields, actorUserId, cancellationToken);
        await SyncCustomerDetailsAsync(customerId, fields, cancellationToken);
        await SyncBeatCustomerAsync(customerId, fields, cancellationToken);
    }

    private async Task SyncCustomerAddressAsync(ulong customerId, IReadOnlyDictionary<string, string?> fields, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var address1 = FirstNonBlank(ReadField(fields, "address_line"), ReadField(fields, "address1"), ReadField(fields, "billing_address"));
        var address2 = FirstNonBlank(ReadField(fields, "shipping_address"), ReadField(fields, "address2"));
        var countryId = ReadULong(fields, "country_id") ?? ReadULong(fields, "billing_country");
        var stateId = ReadULong(fields, "state_id") ?? ReadULong(fields, "billing_state");
        var districtId = ReadULong(fields, "district_id") ?? ReadULong(fields, "billing_district");
        var cityId = ReadULong(fields, "city_id") ?? ReadULong(fields, "billing_city");
        var pincodeId = ReadULong(fields, "pincode_id") ?? ReadULong(fields, "billing_pincode");

        if (string.IsNullOrWhiteSpace(address1) && string.IsNullOrWhiteSpace(address2) && !countryId.HasValue && !stateId.HasValue && !districtId.HasValue && !cityId.HasValue && !pincodeId.HasValue) return;

        var existingId = await QueryScalarLongAsync("SELECT COALESCE(MAX(id), 0) FROM addresses WHERE customer_id = {0} AND deleted_at IS NULL", [customerId], cancellationToken);
        if (existingId > 0)
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"UPDATE addresses SET address1 = {0}, address2 = {1}, country_id = {2}, state_id = {3}, district_id = {4}, city_id = {5}, pincode_id = {6}, updated_at = UTC_TIMESTAMP()
WHERE id = {7}",
                [address1 ?? string.Empty, address2 ?? string.Empty, countryId, stateId, districtId, cityId, pincodeId, existingId],
                cancellationToken);
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            @"INSERT INTO addresses (active, customer_id, address1, address2, country_id, state_id, district_id, city_id, pincode_id, created_by, created_at, updated_at)
VALUES ('Y', {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            [customerId, address1 ?? string.Empty, address2 ?? string.Empty, countryId, stateId, districtId, cityId, pincodeId, actorUserId],
            cancellationToken);
    }

    private async Task SyncCustomerDetailsAsync(ulong customerId, IReadOnlyDictionary<string, string?> fields, CancellationToken cancellationToken)
    {
        var gst = FirstNonBlank(ReadField(fields, "gst_number"), ReadField(fields, "gstin_no"));
        var pan = FirstNonBlank(ReadField(fields, "pan_number"), ReadField(fields, "pan_no"));
        var aadhar = ReadField(fields, "aadhar_no");
        var accountHolder = FirstNonBlank(ReadField(fields, "account_holder_name"), ReadField(fields, "account_holder"));
        var accountNumber = FirstNonBlank(ReadField(fields, "bank_account_number"), ReadField(fields, "account_number"));
        var bankName = ReadField(fields, "bank_name");
        var ifscCode = FirstNonBlank(ReadField(fields, "ifsc_code"), ReadField(fields, "ifsc"));
        var shopImage = FirstNonBlank(ReadField(fields, "shop_photo"), ReadField(fields, "shop_image"));
        var visitStatus = FirstNonBlank(ReadField(fields, "business_status"), ReadField(fields, "status"));

        if (new[] { gst, pan, aadhar, accountHolder, accountNumber, bankName, ifscCode, shopImage, visitStatus }.All(string.IsNullOrWhiteSpace)) return;

        var existingId = await QueryScalarLongAsync("SELECT COALESCE(MAX(id), 0) FROM customer_details WHERE customer_id = {0} AND deleted_at IS NULL", [customerId], cancellationToken);
        if (existingId > 0)
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"UPDATE customer_details SET gstin_no = {0}, pan_no = {1}, aadhar_no = {2}, account_holder = {3}, account_number = {4}, bank_name = {5}, ifsc_code = {6}, shop_image = {7}, visit_status = {8}, updated_at = UTC_TIMESTAMP()
WHERE id = {9}",
                [gst, pan, aadhar, accountHolder, accountNumber, bankName, ifscCode, shopImage ?? string.Empty, visitStatus ?? string.Empty, existingId],
                cancellationToken);
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            @"INSERT INTO customer_details (active, customer_id, gstin_no, pan_no, aadhar_no, account_holder, account_number, bank_name, ifsc_code, shop_image, visit_status, created_at, updated_at)
VALUES ('Y', {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            [customerId, gst, pan, aadhar, accountHolder, accountNumber, bankName, ifscCode, shopImage ?? string.Empty, visitStatus ?? string.Empty],
            cancellationToken);
    }

    private async Task UpsertRetailerApprovalStatusAsync(ulong customerId, string status, CancellationToken cancellationToken)
    {
        var existingId = await QueryScalarLongAsync("SELECT COALESCE(MAX(id), 0) FROM customer_details WHERE customer_id = {0} AND deleted_at IS NULL", [customerId], cancellationToken);
        if (existingId > 0)
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "UPDATE customer_details SET visit_status = {0}, updated_at = UTC_TIMESTAMP() WHERE id = {1}",
                [status, existingId],
                cancellationToken);
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO customer_details (active, customer_id, visit_status, created_at, updated_at) VALUES ('Y', {0}, {1}, UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            [customerId, status],
            cancellationToken);
    }

    private async Task SyncBeatCustomerAsync(ulong customerId, IReadOnlyDictionary<string, string?> fields, CancellationToken cancellationToken)
    {
        var beatId = ReadULong(fields, "beat_id");
        if (!beatId.HasValue) return;

        var existingId = await QueryScalarLongAsync("SELECT COALESCE(MAX(id), 0) FROM beat_customers WHERE customer_id = {0}", [customerId], cancellationToken);
        if (existingId > 0)
        {
            await _dbContext.Database.ExecuteSqlRawAsync("UPDATE beat_customers SET active = 'Y', beat_id = {0}, updated_at = UTC_TIMESTAMP() WHERE id = {1}", [beatId, existingId], cancellationToken);
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO beat_customers (active, beat_id, customer_id, created_at, updated_at) VALUES ('Y', {0}, {1}, UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            [beatId, customerId],
            cancellationToken);
    }

    private async Task<long> QueryScalarLongAsync(string sql, object?[] parameters, CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "p" + index;
            parameter.Value = parameters[index] ?? DBNull.Value;
            command.Parameters.Add(parameter);
            command.CommandText = command.CommandText.Replace("{" + index + "}", "@" + parameter.ParameterName, StringComparison.Ordinal);
        }

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }

    private static IEnumerable<CustomerDto> ApplyJsonFilters(IEnumerable<CustomerDto> customers, CustomerListFilterDto filter)
    {
        if (filter.StateId.HasValue) customers = customers.Where(x => x.StateId == filter.StateId);
        if (filter.CityId.HasValue) customers = customers.Where(x => x.CityId == filter.CityId);
        if (filter.PincodeId.HasValue) customers = customers.Where(x => x.PincodeId == filter.PincodeId);
        return customers;
    }

    private async Task AttachAddressNamesAsync(IReadOnlyCollection<CustomerDto> customers, CancellationToken cancellationToken)
    {
        var countryIds = customers.Select(x => x.CountryId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var stateIds = customers.Select(x => x.StateId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var districtIds = customers.Select(x => x.DistrictId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var cityIds = customers.Select(x => x.CityId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var pincodeIds = customers.Select(x => x.PincodeId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();

        var countries = await _dbContext.Countries.AsNoTracking().Where(x => countryIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.CountryName, cancellationToken);
        var states = await _dbContext.States.AsNoTracking().Where(x => stateIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.StateName, cancellationToken);
        var districts = await _dbContext.Districts.AsNoTracking().Where(x => districtIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.DistrictName, cancellationToken);
        var cities = await _dbContext.Cities.AsNoTracking().Where(x => cityIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.CityName, cancellationToken);
        var pincodes = await _dbContext.Pincodes.AsNoTracking().Where(x => pincodeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.PinCode, cancellationToken);

        foreach (var customer in customers)
        {
            if (customer.CountryId.HasValue && countries.TryGetValue(customer.CountryId.Value, out var country)) customer.CountryName = country;
            if (customer.StateId.HasValue && states.TryGetValue(customer.StateId.Value, out var state)) customer.StateName = state;
            if (customer.DistrictId.HasValue && districts.TryGetValue(customer.DistrictId.Value, out var district)) customer.DistrictName = district;
            if (customer.CityId.HasValue && cities.TryGetValue(customer.CityId.Value, out var city)) customer.CityName = city;
            if (customer.PincodeId.HasValue && pincodes.TryGetValue(customer.PincodeId.Value, out var pincode)) customer.Pincode = pincode;
        }
    }

    private async Task AttachAddressFallbackAsync(IReadOnlyCollection<CustomerDto> customers, CancellationToken cancellationToken)
    {
        var customerIds = customers
            .Where(customer => !customer.CountryId.HasValue || !customer.StateId.HasValue || !customer.DistrictId.HasValue || !customer.CityId.HasValue || !customer.PincodeId.HasValue)
            .Select(customer => customer.Id)
            .Distinct()
            .ToArray();
        if (customerIds.Length == 0) return;

        var idCsv = string.Join(',', customerIds);
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $@"SELECT a.customer_id, a.address1, a.country_id, a.state_id, a.district_id, a.city_id, a.pincode_id
FROM addresses a
INNER JOIN (
    SELECT customer_id, MAX(id) AS id
    FROM addresses
    WHERE deleted_at IS NULL AND customer_id IN ({idCsv})
    GROUP BY customer_id
) latest ON latest.id = a.id";

        var addressRows = new Dictionary<ulong, Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++) row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
            var customerId = ToULong(row, "customer_id");
            if (customerId > 0) addressRows[customerId] = row;
        }

        foreach (var customer in customers)
        {
            if (!addressRows.TryGetValue(customer.Id, out var row)) continue;
            customer.CountryId ??= ToNullableULong(row, "country_id");
            customer.StateId ??= ToNullableULong(row, "state_id");
            customer.DistrictId ??= ToNullableULong(row, "district_id");
            customer.CityId ??= ToNullableULong(row, "city_id");
            customer.PincodeId ??= ToNullableULong(row, "pincode_id");
            SetFieldIfPresent(customer.CustomFields, "address_line", ToStringValue(row, "address1"));
            SetFieldIfPresent(customer.CustomFields, "address1", ToStringValue(row, "address1"));
            SetFieldIfPresent(customer.CustomFields, "country_id", customer.CountryId?.ToString());
            SetFieldIfPresent(customer.CustomFields, "state_id", customer.StateId?.ToString());
            SetFieldIfPresent(customer.CustomFields, "district_id", customer.DistrictId?.ToString());
            SetFieldIfPresent(customer.CustomFields, "city_id", customer.CityId?.ToString());
            SetFieldIfPresent(customer.CustomFields, "pincode_id", customer.PincodeId?.ToString());
        }
    }

    private async Task AttachLookupNamesAsync(IReadOnlyCollection<CustomerDto> customers, CancellationToken cancellationToken)
    {
        var distributorIds = customers
            .SelectMany(customer => new[] { ReadULong(customer.CustomFields, "distributor_name"), ReadULong(customer.CustomFields, "agri_distributor") })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        var userIds = customers
            .SelectMany(customer => new[] { "employee_id", "sales_executive_id", "supervisor_id" }
                .SelectMany(key => ReadULongs(ReadField(customer.CustomFields, key))))
            .Distinct()
            .ToArray();

        var distributors = await _dbContext.Customers.AsNoTracking()
            .Where(customer => distributorIds.Contains(customer.Id))
            .Select(customer => new
            {
                customer.Id,
                customer.Name,
                customer.CustomerCode,
                customer.CustomFields
            })
            .ToDictionaryAsync(customer => customer.Id, customer => DistributorDisplayName(customer.CustomerCode, customer.Name, DeserializeFields(customer.CustomFields)), cancellationToken);

        var users = await _dbContext.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.Name, cancellationToken);

        foreach (var customer in customers)
        {
            SetCustomerLookupName(customer.CustomFields, "distributor_name", distributors);
            SetCustomerLookupName(customer.CustomFields, "agri_distributor", distributors);
            SetUserLookupName(customer.CustomFields, "employee_id", users);
            SetUserLookupName(customer.CustomFields, "sales_executive_id", users);
            SetUserLookupName(customer.CustomFields, "supervisor_id", users);
        }
    }

    private async Task AttachPointSummaryAsync(CustomerDto customerDto, Customer customer, CancellationToken cancellationToken)
    {
        var rows = await (from invoice in _dbContext.NewInvoices.AsNoTracking()
                          where invoice.SecondaryCustomerId == customer.Id
                          && invoice.ApprovalStatus == NewInvoice.StatusApprovedHo
                          join creatorRow in _dbContext.Users.AsNoTracking() on invoice.CreatedBy equals creatorRow.Id into creators
                          from creator in creators.DefaultIfEmpty()
                          join branchRow in _dbContext.Branches.AsNoTracking() on creator.PrimaryBranchId equals branchRow.Id into branches
                          from branch in branches.DefaultIfEmpty()
                          select new CustomerInvoiceRow(invoice, branch))
            .ToListAsync(cancellationToken);

        if (rows.Count > 0)
        {
            var dates = rows.Select(x => DateOnly.FromDateTime(x.Invoice.InvoiceDate.Date)).ToArray();
            var minDate = dates.Min();
            var maxDate = dates.Max();
            var schemes = await _dbContext.LoyaltySchemes.AsNoTracking()
                .Include(x => x.Slabs)
                .Where(x => x.DeletedAt == null
                    && x.Active == "Y"
                    && (x.Status == "Published" || x.Status == "Live")
                    && x.SchemeType == "Invoice"
                    && x.StartDate <= maxDate
                    && x.EndDate >= minDate)
                .ToListAsync(cancellationToken);

            var zoneName = await LoadAssignedZoneNameAsync(customer, cancellationToken);

            foreach (var row in rows)
            {
                var invoiceDate = DateOnly.FromDateTime(row.Invoice.InvoiceDate.Date);
                var matchingSchemes = schemes.Where(scheme => SchemeMatchesCustomer(scheme, invoiceDate, customer, row.Branch, zoneName));
                foreach (var scheme in matchingSchemes)
                {
                    var periodAmount = PeriodAmount(customer.Id, scheme, rows.Select(x => x.Invoice));
                    var points = CalculateSchemePoints(row.Invoice.Amount, periodAmount, scheme);
                    if (points <= 0) continue;

                    customerDto.TotalPoints += points;
                    if (string.Equals(scheme.SchemeTag, "Booster", StringComparison.OrdinalIgnoreCase)) customerDto.TotalBoosterPoints += points;
                    else customerDto.TotalRegularPoints += points;
                }
            }
        }

        var redemptionRows = await _dbContext.LoyaltyRedemptions.AsNoTracking()
            .Where(x => x.DeletedAt == null && x.CustomerId == customer.Id)
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Points = x.Sum(row => row.Points) })
            .ToListAsync(cancellationToken);

        customerDto.TotalRedeemPoints = redemptionRows
            .Where(x => x.Status is LoyaltyRedemption.StatusPending or LoyaltyRedemption.StatusApproved)
            .Sum(x => x.Points);
        customerDto.TotalRejectedPoints = redemptionRows
            .Where(x => x.Status == LoyaltyRedemption.StatusRejected)
            .Sum(x => x.Points);
        customerDto.TotalBalancePoints = Math.Max(0, customerDto.TotalPoints - customerDto.TotalRedeemPoints);
    }

    private async Task<string?> LoadAssignedZoneNameAsync(Customer customer, CancellationToken cancellationToken)
    {
        var employeeId = FirstULong(ReadCustomerField(customer, "employee_id"))
            ?? FirstULong(ReadCustomerField(customer, "sales_executive_id"))
            ?? customer.ExecutiveId;

        if (!employeeId.HasValue) return null;

        return await (from user in _dbContext.Users.AsNoTracking()
                      where user.Id == employeeId.Value
                      join divisionRow in _dbContext.Divisions.AsNoTracking() on user.DivisionId equals divisionRow.Id into divisions
                      from division in divisions.DefaultIfEmpty()
                      select division != null ? division.DivisionName : null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool SchemeMatchesCustomer(LoyaltyScheme scheme, DateOnly invoiceDate, Customer customer, Branch? branch, string? zoneName)
    {
        if (invoiceDate < scheme.StartDate || invoiceDate > scheme.EndDate) return false;
        if (!CustomerTypeMatches(scheme.CustomerType)) return false;

        if (string.Equals(scheme.AreaScope, "All", StringComparison.OrdinalIgnoreCase)) return true;
        var values = ReadSchemeAreaValues(scheme.AreaValues);
        if (values.Count == 0) return true;

        return scheme.AreaScope switch
        {
            "Customer" => values.Any(value => string.Equals(value, customer.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, customer.CustomerCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, $"{customer.CustomerCode} - {customer.Name}", StringComparison.OrdinalIgnoreCase)),
            "Branch" => branch is not null && values.Any(value => string.Equals(value, branch.BranchName, StringComparison.OrdinalIgnoreCase)),
            "Zone" => !string.IsNullOrWhiteSpace(zoneName) && values.Any(value => string.Equals(value, zoneName, StringComparison.OrdinalIgnoreCase)),
            _ => true
        };
    }

    private static bool CustomerTypeMatches(string customerType) =>
        string.Equals(customerType, "Retailer", StringComparison.OrdinalIgnoreCase)
        || string.Equals(customerType, "Influencers", StringComparison.OrdinalIgnoreCase)
        || string.Equals(customerType, "Influencer", StringComparison.OrdinalIgnoreCase)
        || string.Equals(customerType, "Retailer + Plumber", StringComparison.OrdinalIgnoreCase);

    private static decimal PeriodAmount(ulong customerId, LoyaltyScheme scheme, IEnumerable<NewInvoice> invoices)
    {
        var startDate = scheme.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = scheme.EndDate.ToDateTime(TimeOnly.MaxValue);
        return invoices
            .Where(x => x.SecondaryCustomerId == customerId
                && x.InvoiceDate >= startDate
                && x.InvoiceDate <= endDate)
            .Sum(x => x.Amount);
    }

    private static decimal CalculateSchemePoints(decimal invoiceAmount, decimal periodAmount, LoyaltyScheme scheme)
    {
        var achieved = scheme.Slabs
            .Where(x => x.DeletedAt == null)
            .OrderBy(x => x.ValueFrom)
            .ThenBy(x => x.SortOrder)
            .LastOrDefault(slab => periodAmount >= slab.ValueFrom && (!slab.ValueTo.HasValue || periodAmount <= slab.ValueTo.Value));

        if (achieved is null) return 0;

        return string.Equals(scheme.BasedOn, "Percentage", StringComparison.OrdinalIgnoreCase)
            ? Math.Round(invoiceAmount * achieved.RewardValue / 100, 2)
            : achieved.RewardValue;
    }

    private static IReadOnlyCollection<string> ReadSchemeAreaValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static CustomerDto ToCustomerDto(Customer customer, string? createdByName, string? parentName)
    {
        var fields = DeserializeFields(customer.CustomFields);
        return new CustomerDto
        {
            Id = customer.Id,
            Active = customer.Active,
            Name = customer.Name,
            Mobile = customer.Mobile,
            ContactNumber = customer.ContactNumber,
            Email = customer.Email,
            CustomerCode = customer.CustomerCode,
            ProfileImage = customer.ProfileImage,
            ShopImage = customer.ShopImage,
            CustomerType = customer.CustomerType,
            CustomerTypeName = CustomerTypeName(customer.CustomerType),
            SapCode = customer.SapCode,
            ParentId = customer.ParentId,
            ParentName = parentName,
            CountryId = ReadULong(fields, "country_id"),
            StateId = ReadULong(fields, "state_id"),
            DistrictId = ReadULong(fields, "district_id"),
            CityId = ReadULong(fields, "city_id"),
            PincodeId = ReadULong(fields, "pincode_id"),
            CreatedBy = customer.CreatedBy,
            CreatedByName = createdByName,
            CreatedAt = customer.CreatedAt,
            CustomFields = fields
        };
    }

    private static Dictionary<string, string?> DeserializeFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? SerializeFields(Dictionary<string, string?>? fields) =>
        fields is null ? null : JsonSerializer.Serialize(fields.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value), JsonOptions);

    private static string CustomerTypeName(ulong? type) => type switch
    {
        1 => "Distributor",
        2 => "Retailer",
        3 => "Influencers",
        null => string.Empty,
        _ => $"Type {type}"
    };

    private static bool IsRetailerCustomer(Customer customer)
    {
        if (customer.CustomerType == 2) return true;
        var fields = DeserializeFields(customer.CustomFields);
        var type = ReadField(fields, "customer_type");
        return string.Equals(type, "2", StringComparison.OrdinalIgnoreCase)
            || ContainsRetailer(ReadField(fields, "customer_type_name"))
            || ContainsRetailer(ReadField(fields, "type"))
            || ContainsRetailer(ReadField(fields, "type_name"));
    }

    private static bool ContainsRetailer(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains("retailer", StringComparison.OrdinalIgnoreCase);

    private static string? ReadField(IReadOnlyDictionary<string, string?>? fields, string key) =>
        fields is not null && fields.TryGetValue(key, out var value) ? value : null;

    private static string? ReadCustomerField(Customer customer, string key) =>
        ReadField(DeserializeFields(customer.CustomFields), key);

    private static string KycAttachmentKey(string documentKey) => documentKey switch
    {
        "gst" => "gst_attachment",
        "pan" => "pan_attachment",
        "aadhar" => "aadhar_attachment",
        "bank" => "bank_proof",
        _ => documentKey
    };

    private static ulong? ReadULong(IReadOnlyDictionary<string, string?> fields, string key) =>
        ulong.TryParse(ReadField(fields, key), out var parsed) ? parsed : null;

    private static void SetFieldIfPresent(IDictionary<string, string?> fields, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) fields[key] = value.Trim();
    }

    private static string? ToStringValue(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null and not DBNull ? Convert.ToString(value) : null;

    private static ulong ToULong(IReadOnlyDictionary<string, object?> row, string key) =>
        ulong.TryParse(ToStringValue(row, key), out var parsed) ? parsed : 0;

    private static ulong? ToNullableULong(IReadOnlyDictionary<string, object?> row, string key)
    {
        var value = ToULong(row, key);
        return value > 0 ? value : null;
    }

    private static ulong? FirstULong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var first = value.Trim();
        if (first.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                var values = JsonSerializer.Deserialize<List<ulong>>(first, JsonOptions);
                return values?.FirstOrDefault(x => x > 0);
            }
            catch
            {
                return null;
            }
        }

        first = first.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? first;
        return ulong.TryParse(first, out var parsed) ? parsed : null;
    }

    private static ulong[] ReadULongs(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        var trimmed = value.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                return JsonSerializer.Deserialize<List<ulong>>(trimmed, JsonOptions)?.Where(id => id > 0).ToArray() ?? [];
            }
            catch
            {
                return [];
            }
        }

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => ulong.TryParse(item, out var parsed) ? parsed : 0)
            .Where(id => id > 0)
            .ToArray();
    }

    private static void SetCustomerLookupName(Dictionary<string, string?> fields, string key, IReadOnlyDictionary<ulong, string> names)
    {
        var id = ReadULong(fields, key);
        if (id.HasValue && names.TryGetValue(id.Value, out var name)) fields[$"{key}_name"] = name;
    }

    private static void SetUserLookupName(Dictionary<string, string?> fields, string key, IReadOnlyDictionary<ulong, string> names)
    {
        var values = ReadULongs(ReadField(fields, key));
        if (values.Length == 0) return;

        var labels = values
            .Select(id => names.TryGetValue(id, out var name) ? name : null)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        if (labels.Length > 0) fields[$"{key}_name"] = string.Join(", ", labels);
    }

    private static string DistributorDisplayName(string? customerCode, string name, IReadOnlyDictionary<string, string?> fields)
    {
        var legalName = FirstNonBlank(ReadField(fields, "legal_name"), ReadField(fields, "shop_name"), name) ?? name;
        return string.Join(" - ", new[] { customerCode, legalName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string? NormalizeText(string? value)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? NormalizeActive(string? active)
    {
        if (string.IsNullOrWhiteSpace(active)) return null;
        return active.Trim().Equals("N", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";
    }

    private static string ToggleActive(string active) =>
        active.Equals("Y", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";

    private async Task<Role> EnsureDistributorRoleAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var role = await _dbContext.Roles.FirstOrDefaultAsync(
            x => x.Name == DistributorRoleName && x.GuardName == GuardName,
            cancellationToken);

        if (role is null)
        {
            role = new Role
            {
                Name = DistributorRoleName,
                GuardName = GuardName,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _dbContext.Roles.AddAsync(role, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var permissionIds = await _dbContext.Permissions
            .Where(x => x.GuardName == GuardName && DistributorPermissions.Contains(x.Name))
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        var assignedPermissionIds = await _dbContext.RoleHasPermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToArrayAsync(cancellationToken);

        var missingPermissions = permissionIds
            .Except(assignedPermissionIds)
            .Select(permissionId => new RoleHasPermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            })
            .ToArray();

        if (missingPermissions.Length > 0)
        {
            await _dbContext.RoleHasPermissions.AddRangeAsync(missingPermissions, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return role;
    }

    private async Task SyncLinkedUserActiveAsync(ulong customerId, string active, CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users.IgnoreQueryFilters()
            .Where(x => x.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            user.Active = active;
            user.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string? FirstMobile(string? mobileNumbers) =>
        mobileNumbers?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

    private static string? NormalizeMobile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length > 10 && digits.StartsWith("91", StringComparison.Ordinal)) digits = digits[2..];
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static (string FirstName, string LastName) SplitName(string name)
    {
        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (parts.FirstOrDefault() ?? name, parts.Length > 1 ? parts[1] : string.Empty);
    }

    private sealed record CustomerInvoiceRow(NewInvoice Invoice, Branch? Branch);
}
