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
        await AttachAddressNamesAsync([dto], cancellationToken);
        await AttachLookupNamesAsync([dto], cancellationToken);
        await AttachPointSummaryAsync(dto, row.Customer, cancellationToken);
        return dto;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CustomerRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
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
            CustomFields = SerializeFields(request.CustomFields),
            CreatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _dbContext.Customers.AddAsync(customer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
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
        if (request.CustomFields is not null) customer.CustomFields = SerializeFields(request.CustomFields);

        var active = NormalizeActive(request.Active);
        if (active is not null) customer.Active = active;
        customer.UpdatedBy = actorUserId;
        customer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
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
                          && invoice.ApprovedHoBy == 1 
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
                    && x.Status == "Live"
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
