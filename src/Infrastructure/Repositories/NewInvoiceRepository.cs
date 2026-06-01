using System.Text.Json;
using Application.DTOs.NewInvoices;
using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class NewInvoiceRepository : INewInvoiceRepository
{
    private const int MaxRows = 50000;
    private const ulong RetailerCustomerType = 2;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;

    public NewInvoiceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<NewInvoiceDto>> GetInvoicesAsync(NewInvoiceFilterDto filter, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var distributorCustomerId = await GetDistributorCustomerIdAsync(actorUserId, cancellationToken);
        var rows = await ApplyFilters(BaseQuery(distributorCustomerId), filter)
            .OrderByDescending(x => x.Invoice.CreatedAt)
            .ThenByDescending(x => x.Invoice.Id)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var cities = await LoadCitiesAsync(rows.Select(x => CityId(x.Customer)), cancellationToken);
        var assignedZones = await LoadAssignedZoneNamesAsync(rows.Select(x => x.Customer), cancellationToken);
        var assignedDistributors = await LoadAssignedDistributorNamesAsync(rows.Select(x => x.Customer), cancellationToken);
        var assignedEmployees = await LoadAssignedEmployeeNamesAsync(rows.Select(x => x.Customer), cancellationToken);
        var schemes = await LoadSchemesAsync(rows.Select(x => x.Invoice.InvoiceDate), cancellationToken);
        var schemeInvoices = await LoadSchemeInvoicesAsync(rows.Select(x => x.Customer.Id), schemes, cancellationToken);
        return rows.SelectMany(x => ToSchemeDtos(x.Invoice, x.Customer, CityName(x.Customer, cities), AssignedZoneName(x.Customer, assignedZones) ?? x.Branch?.BranchName, AssignedDistributorName(x.Customer, assignedDistributors), AssignedEmployeeName(x.Customer, assignedEmployees), x.Creator, x.Branch, schemes, schemeInvoices)).ToList();
    }

    public async Task<NewInvoiceDto?> GetInvoiceAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var distributorCustomerId = await GetDistributorCustomerIdAsync(actorUserId, cancellationToken);
        var row = await BaseQuery(distributorCustomerId).Where(x => x.Invoice.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var cities = await LoadCitiesAsync([CityId(row.Customer)], cancellationToken);
        var assignedZones = await LoadAssignedZoneNamesAsync([row.Customer], cancellationToken);
        var assignedDistributors = await LoadAssignedDistributorNamesAsync([row.Customer], cancellationToken);
        var assignedEmployees = await LoadAssignedEmployeeNamesAsync([row.Customer], cancellationToken);
        var schemes = await LoadSchemesAsync([row.Invoice.InvoiceDate], cancellationToken);
        var schemeInvoices = await LoadSchemeInvoicesAsync([row.Customer.Id], schemes, cancellationToken);
        var dto = ToSchemeDtos(row.Invoice, row.Customer, CityName(row.Customer, cities), AssignedZoneName(row.Customer, assignedZones) ?? row.Branch?.BranchName, AssignedDistributorName(row.Customer, assignedDistributors), AssignedEmployeeName(row.Customer, assignedEmployees), row.Creator, row.Branch, schemes, schemeInvoices).First();
        dto.ApprovalLogs = await GetApprovalLogsAsync(id, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyCollection<RetailerOptionDto>> GetRetailerOptionsAsync(string? search, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var distributorCustomerId = await GetDistributorCustomerIdAsync(actorUserId, cancellationToken);
        var query = _dbContext.Customers.AsNoTracking()
            .Where(x => x.Active == "Y" && x.CustomerType == RetailerCustomerType);
        query = ApplyDistributorRetailerScope(query, distributorCustomerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var likeTerm = $"%{term}%";
            query = query.Where(x => x.Name.Contains(term)
                || (x.Mobile != null && x.Mobile.Contains(term))
                || x.CustomerCode.Contains(term)
                || (x.CustomFields != null && EF.Functions.Like(x.CustomFields, likeTerm)));
        }

        var retailers = await query
            .OrderBy(x => x.Name)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var cities = await LoadCitiesAsync(retailers.Select(CityId), cancellationToken);
        return retailers.Select(customer => new RetailerOptionDto
        {
            Id = customer.Id,
            OwnerName = OwnerName(customer),
            ShopName = ShopName(customer),
            MobileNumber = MobileNumber(customer),
            CityName = CityName(customer, cities)
        }).ToList();
    }

    public async Task<Customer?> GetRetailerAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var distributorCustomerId = await GetDistributorCustomerIdAsync(actorUserId, cancellationToken);
        var query = _dbContext.Customers.Where(x => x.Id == id && x.Active == "Y" && x.CustomerType == RetailerCustomerType);
        query = ApplyDistributorRetailerScope(query, distributorCustomerId);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, ulong? exceptId, CancellationToken cancellationToken) =>
        await _dbContext.NewInvoices.AnyAsync(x => x.InvoiceNumber == invoiceNumber && (!exceptId.HasValue || x.Id != exceptId), cancellationToken);

    public async Task<NewInvoiceDto> CreateInvoiceAsync(NewInvoice invoice, CancellationToken cancellationToken)
    {
        await _dbContext.NewInvoices.AddAsync(invoice, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetInvoiceAsync(invoice.Id, null, cancellationToken) ?? throw new InvalidOperationException("Created invoice could not be loaded.");
    }

    public async Task<NewInvoice?> FindInvoiceEntityAsync(ulong id, CancellationToken cancellationToken) =>
        await _dbContext.NewInvoices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<NewInvoiceDto> SaveInvoiceAsync(NewInvoice invoice, string statusType, int? fromStatus, int toStatus, ulong actorUserId, string? remark, CancellationToken cancellationToken)
    {
        invoice.UpdatedAt = DateTime.UtcNow;
        await _dbContext.NewInvoiceApprovalLogs.AddAsync(new NewInvoiceApprovalLog
        {
            LogDate = DateTime.UtcNow.Date,
            NewInvoiceId = invoice.Id,
            CreatedBy = actorUserId,
            StatusType = statusType,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Remark = remark,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetInvoiceAsync(invoice.Id, null, cancellationToken) ?? throw new InvalidOperationException("Invoice could not be loaded.");
    }

    public async Task<bool> DeleteInvoiceAsync(NewInvoice invoice, CancellationToken cancellationToken)
    {
        var logs = _dbContext.NewInvoiceApprovalLogs.Where(x => x.NewInvoiceId == invoice.Id);
        _dbContext.NewInvoiceApprovalLogs.RemoveRange(logs);
        _dbContext.NewInvoices.Remove(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<InvoiceRow> ApplyFilters(IQueryable<InvoiceRow> query, NewInvoiceFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.RetailerSearch))
        {
            var search = filter.RetailerSearch.Trim();
            var likeSearch = $"%{search}%";
            query = query.Where(x => x.Customer.Name.Contains(search)
                || (x.Customer.Mobile != null && x.Customer.Mobile.Contains(search))
                || x.Customer.CustomerCode.Contains(search)
                || (x.Customer.CustomFields != null && EF.Functions.Like(x.Customer.CustomFields, likeSearch)));
        }

        if (!string.IsNullOrWhiteSpace(filter.InvoiceNumber))
        {
            var invoiceNumber = filter.InvoiceNumber.Trim();
            query = query.Where(x => x.Invoice.InvoiceNumber.Contains(invoiceNumber));
        }

        if (filter.ApprovalStatus.HasValue) query = query.Where(x => x.Invoice.ApprovalStatus == filter.ApprovalStatus);
        if (filter.BranchId.HasValue) query = query.Where(x => x.Creator != null && x.Creator.PrimaryBranchId == filter.BranchId);
        if (filter.FromDate.HasValue) query = query.Where(x => x.Invoice.InvoiceDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) query = query.Where(x => x.Invoice.InvoiceDate.Date <= filter.ToDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            var likeSearch = $"%{search}%";
            query = query.Where(x => x.Invoice.InvoiceNumber.Contains(search)
                || x.Invoice.Amount.ToString().Contains(search)
                || x.Invoice.Points.ToString().Contains(search)
                || x.Customer.Name.Contains(search)
                || (x.Customer.Mobile != null && x.Customer.Mobile.Contains(search))
                || x.Customer.CustomerCode.Contains(search)
                || (x.Customer.CustomFields != null && EF.Functions.Like(x.Customer.CustomFields, likeSearch)));
        }

        return query;
    }

    private IQueryable<InvoiceRow> BaseQuery(ulong? distributorCustomerId)
    {
        var query =
            from invoice in _dbContext.NewInvoices.AsNoTracking()
            join customer in _dbContext.Customers.AsNoTracking().Where(x => x.CustomerType == RetailerCustomerType) on invoice.SecondaryCustomerId equals customer.Id
            join creatorRow in _dbContext.Users.AsNoTracking() on invoice.CreatedBy equals creatorRow.Id into creators
            from creator in creators.DefaultIfEmpty()
            join branchRow in _dbContext.Branches.AsNoTracking() on creator.PrimaryBranchId equals branchRow.Id into branches
            from branch in branches.DefaultIfEmpty()
            select new InvoiceRow
            {
                Invoice = invoice,
                Customer = customer,
                Creator = creator,
                Branch = branch
            };

        return ApplyDistributorInvoiceScope(query, distributorCustomerId);
    }

    private async Task<ulong?> GetDistributorCustomerIdAsync(ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) return null;

        var isDistributor = await _dbContext.ModelHasRoles.AsNoTracking()
            .Where(x => x.ModelId == actorUserId.Value && x.ModelType == LaravelModelTypes.User)
            .Join(_dbContext.Roles.AsNoTracking(), modelRole => modelRole.RoleId, role => role.Id, (_, role) => role.Name)
            .AnyAsync(roleName => roleName == "Distributor", cancellationToken);

        if (!isDistributor) return null;

        var customerId = await _dbContext.Users.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.Id == actorUserId.Value)
            .Select(x => x.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        return customerId ?? 0;
    }

    private static IQueryable<Customer> ApplyDistributorRetailerScope(IQueryable<Customer> query, ulong? distributorCustomerId)
    {
        if (!distributorCustomerId.HasValue) return query;

        var domestic = JsonFieldPattern("distributor_name", distributorCustomerId.Value);
        var domesticSpaced = JsonFieldSpacedPattern("distributor_name", distributorCustomerId.Value);
        var domesticNumber = JsonNumberFieldPattern("distributor_name", distributorCustomerId.Value);
        var agri = JsonFieldPattern("agri_distributor", distributorCustomerId.Value);
        var agriSpaced = JsonFieldSpacedPattern("agri_distributor", distributorCustomerId.Value);
        var agriNumber = JsonNumberFieldPattern("agri_distributor", distributorCustomerId.Value);

        return query.Where(x => x.CustomFields != null
            && (EF.Functions.Like(x.CustomFields, domestic)
                || EF.Functions.Like(x.CustomFields, domesticSpaced)
                || EF.Functions.Like(x.CustomFields, domesticNumber)
                || EF.Functions.Like(x.CustomFields, agri)
                || EF.Functions.Like(x.CustomFields, agriSpaced)
                || EF.Functions.Like(x.CustomFields, agriNumber)));
    }

    private static IQueryable<InvoiceRow> ApplyDistributorInvoiceScope(IQueryable<InvoiceRow> query, ulong? distributorCustomerId)
    {
        if (!distributorCustomerId.HasValue) return query;

        var domestic = JsonFieldPattern("distributor_name", distributorCustomerId.Value);
        var domesticSpaced = JsonFieldSpacedPattern("distributor_name", distributorCustomerId.Value);
        var domesticNumber = JsonNumberFieldPattern("distributor_name", distributorCustomerId.Value);
        var agri = JsonFieldPattern("agri_distributor", distributorCustomerId.Value);
        var agriSpaced = JsonFieldSpacedPattern("agri_distributor", distributorCustomerId.Value);
        var agriNumber = JsonNumberFieldPattern("agri_distributor", distributorCustomerId.Value);

        return query.Where(x => x.Customer.CustomFields != null
            && (EF.Functions.Like(x.Customer.CustomFields, domestic)
                || EF.Functions.Like(x.Customer.CustomFields, domesticSpaced)
                || EF.Functions.Like(x.Customer.CustomFields, domesticNumber)
                || EF.Functions.Like(x.Customer.CustomFields, agri)
                || EF.Functions.Like(x.Customer.CustomFields, agriSpaced)
                || EF.Functions.Like(x.Customer.CustomFields, agriNumber)));
    }

    private static string JsonFieldPattern(string key, ulong value) => $"%\"{key}\":\"{value}\"%";

    private static string JsonFieldSpacedPattern(string key, ulong value) => $"%\"{key}\": \"{value}\"%";

    private static string JsonNumberFieldPattern(string key, ulong value) => $"%\"{key}\":{value}%";

    private async Task<Dictionary<ulong, string>> LoadCitiesAsync(IEnumerable<ulong?> cityIds, CancellationToken cancellationToken)
    {
        var ids = cityIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        if (ids.Length == 0) return [];
        return await _dbContext.Cities.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.CityName, cancellationToken);
    }

    private async Task<Dictionary<ulong, string>> LoadAssignedZoneNamesAsync(IEnumerable<Customer> customers, CancellationToken cancellationToken)
    {
        var customerEmployeeIds = customers
            .Select(customer => new { CustomerId = customer.Id, EmployeeId = AssignedEmployeeId(customer) })
            .Where(x => x.EmployeeId.HasValue)
            .GroupBy(x => x.CustomerId)
            .Select(x => x.First())
            .ToList();

        var employeeIds = customerEmployeeIds.Select(x => x.EmployeeId!.Value).Distinct().ToArray();
        if (employeeIds.Length == 0) return [];

        var employeeZones = await (from user in _dbContext.Users.AsNoTracking()
                                   where employeeIds.Contains(user.Id)
                                   join divisionRow in _dbContext.Divisions.AsNoTracking() on user.DivisionId equals divisionRow.Id into divisions
                                   from division in divisions.DefaultIfEmpty()
                                   select new { user.Id, ZoneName = division != null ? division.DivisionName : null })
            .Where(x => x.ZoneName != null)
            .ToDictionaryAsync(x => x.Id, x => x.ZoneName!, cancellationToken);

        return customerEmployeeIds
            .Where(x => x.EmployeeId.HasValue && employeeZones.ContainsKey(x.EmployeeId.Value))
            .ToDictionary(x => x.CustomerId, x => employeeZones[x.EmployeeId!.Value]);
    }

    private async Task<Dictionary<ulong, string>> LoadAssignedEmployeeNamesAsync(IEnumerable<Customer> customers, CancellationToken cancellationToken)
    {
        var customerEmployeeIds = customers
            .Select(customer => new { CustomerId = customer.Id, EmployeeId = AssignedEmployeeId(customer) })
            .Where(x => x.EmployeeId.HasValue)
            .GroupBy(x => x.CustomerId)
            .Select(x => x.First())
            .ToList();

        var employeeIds = customerEmployeeIds.Select(x => x.EmployeeId!.Value).Distinct().ToArray();
        if (employeeIds.Length == 0) return [];

        var employeeNames = await _dbContext.Users.AsNoTracking()
            .Where(x => employeeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        return customerEmployeeIds
            .Where(x => x.EmployeeId.HasValue && employeeNames.ContainsKey(x.EmployeeId.Value))
            .ToDictionary(x => x.CustomerId, x => employeeNames[x.EmployeeId!.Value]);
    }

    private async Task<Dictionary<ulong, string>> LoadAssignedDistributorNamesAsync(IEnumerable<Customer> customers, CancellationToken cancellationToken)
    {
        var customerDistributorIds = customers
            .Select(customer => new { CustomerId = customer.Id, DistributorId = AssignedDistributorId(customer) })
            .Where(x => x.DistributorId.HasValue)
            .GroupBy(x => x.CustomerId)
            .Select(x => x.First())
            .ToList();

        var distributorIds = customerDistributorIds.Select(x => x.DistributorId!.Value).Distinct().ToArray();
        if (distributorIds.Length == 0) return [];

        var distributorNames = await _dbContext.Customers.AsNoTracking()
            .Where(x => distributorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        return customerDistributorIds
            .Where(x => x.DistributorId.HasValue && distributorNames.ContainsKey(x.DistributorId.Value))
            .ToDictionary(x => x.CustomerId, x => distributorNames[x.DistributorId!.Value]);
    }

    private async Task<IReadOnlyCollection<NewInvoiceApprovalLogDto>> GetApprovalLogsAsync(ulong invoiceId, CancellationToken cancellationToken) =>
        await (from log in _dbContext.NewInvoiceApprovalLogs.AsNoTracking()
              where log.NewInvoiceId == invoiceId
              join userRow in _dbContext.Users.AsNoTracking() on log.CreatedBy equals userRow.Id into users
              from user in users.DefaultIfEmpty()
              orderby log.CreatedAt descending, log.Id descending
              select new NewInvoiceApprovalLogDto
              {
                  Id = log.Id,
                  LogDate = log.LogDate,
                  CreatedBy = log.CreatedBy,
                  CreatedByName = user != null ? user.Name : null,
                  EmployeeCode = user != null ? user.EmployeeCodes : null,
                  StatusType = log.StatusType ?? string.Empty,
                  FromStatus = log.FromStatus,
                  ToStatus = log.ToStatus,
                  Remark = log.Remark,
                  CreatedAt = log.CreatedAt
              }).ToListAsync(cancellationToken);

    private async Task<IReadOnlyCollection<LoyaltyScheme>> LoadSchemesAsync(IEnumerable<DateTime> invoiceDates, CancellationToken cancellationToken)
    {
        var dates = invoiceDates.Select(x => DateOnly.FromDateTime(x.Date)).ToArray();
        if (dates.Length == 0) return [];
        var minDate = dates.Min();
        var maxDate = dates.Max();

        return await _dbContext.LoyaltySchemes.AsNoTracking()
            .Include(x => x.Slabs)
            .Where(x => x.DeletedAt == null
                && x.Active == "Y"
                && x.Status == "Live"
                && x.SchemeType == "Invoice"
                && x.StartDate <= maxDate
                && x.EndDate >= minDate)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<SchemeInvoiceAmount>> LoadSchemeInvoicesAsync(IEnumerable<ulong> customerIds, IReadOnlyCollection<LoyaltyScheme> schemes, CancellationToken cancellationToken)
    {
        var ids = customerIds.Distinct().ToArray();
        if (ids.Length == 0 || schemes.Count == 0) return [];

        var minDate = schemes.Min(x => x.StartDate).ToDateTime(TimeOnly.MinValue);
        var maxDate = schemes.Max(x => x.EndDate).ToDateTime(TimeOnly.MaxValue);

        return await _dbContext.NewInvoices.AsNoTracking()
            .Where(x => ids.Contains(x.SecondaryCustomerId)
                && x.InvoiceDate >= minDate
                && x.InvoiceDate <= maxDate)
            .Select(x => new SchemeInvoiceAmount(x.Id, x.SecondaryCustomerId, x.InvoiceDate, x.Amount))
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyCollection<NewInvoiceDto> ToSchemeDtos(NewInvoice invoice, Customer customer, string? cityName, string? zoneName, string? assignedDistributorName, string? assignedEmployeeName, User? creator, Branch? branch, IReadOnlyCollection<LoyaltyScheme> schemes, IReadOnlyCollection<SchemeInvoiceAmount> schemeInvoices)
    {
        var invoiceDate = DateOnly.FromDateTime(invoice.InvoiceDate.Date);
        var matchingSchemes = schemes
            .Where(scheme => SchemeMatches(scheme, invoiceDate, customer, branch, zoneName))
            .OrderBy(scheme => scheme.SchemeTag)
            .ThenBy(scheme => scheme.SchemeName)
            .ToList();

        if (matchingSchemes.Count == 0) return [ToDto(invoice, customer, cityName, zoneName, assignedDistributorName, assignedEmployeeName, creator, branch, null, null)];
        return matchingSchemes.Select(scheme =>
        {
            var periodAmount = PeriodAmount(invoice, scheme, schemeInvoices);
            return ToDto(invoice, customer, cityName, zoneName, assignedDistributorName, assignedEmployeeName, creator, branch, scheme, CalculateSchemeResult(invoice.Amount, periodAmount, scheme));
        }).ToList();
    }

    private static NewInvoiceDto ToDto(NewInvoice invoice, Customer customer, string? cityName, string? zoneName, string? assignedDistributorName, string? assignedEmployeeName, User? creator, Branch? branch, LoyaltyScheme? scheme, SchemeResult? schemeResult)
    {
        var schemePoints = schemeResult?.Points ?? 0;
        var isBooster = string.Equals(scheme?.SchemeTag, "Booster", StringComparison.OrdinalIgnoreCase);
        return new NewInvoiceDto
        {
            Id = invoice.Id,
            SecondaryCustomerId = invoice.SecondaryCustomerId,
            RetailerCode = $"RET-{invoice.SecondaryCustomerId.ToString().PadLeft(4, '0')}",
            CustomerName = OwnerName(customer),
            ShopName = ShopName(customer),
            MobileNumber = MobileNumber(customer),
            CityName = cityName,
            ZoneName = zoneName,
            AssignedDistributorName = assignedDistributorName,
            AssignedEmployeeName = assignedEmployeeName,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            Amount = invoice.Amount,
            Points = schemePoints,
            SchemeId = scheme?.Id,
            SchemeName = scheme?.SchemeName,
            SchemeCode = scheme?.SchemeCode,
            SchemeTag = scheme?.SchemeTag,
            SchemeBasedOn = scheme?.BasedOn,
            SchemeRewardValue = schemeResult?.RewardValue,
            SchemePoints = schemePoints,
            TierName = schemeResult?.TierName,
            SchemeHintMessage = schemeResult?.HintMessage,
            RegularWalletPoints = scheme is not null && !isBooster ? schemePoints : 0,
            BoosterWalletPoints = isBooster ? schemePoints : 0,
            Attachment = invoice.Attachment,
            ApprovalStatus = invoice.ApprovalStatus,
            ApprovalStatusLabel = StatusLabel(invoice.ApprovalStatus),
            ApprovalRemark = invoice.ApprovalRemark,
            CreatedBy = invoice.CreatedBy,
            CreatedByName = creator?.Name,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };
    }

    private static bool SchemeMatches(LoyaltyScheme scheme, DateOnly invoiceDate, Customer customer, Branch? branch, string? zoneName)
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
        || string.Equals(customerType, "Retailer + Plumber", StringComparison.OrdinalIgnoreCase);

    private static decimal PeriodAmount(NewInvoice invoice, LoyaltyScheme scheme, IReadOnlyCollection<SchemeInvoiceAmount> schemeInvoices)
    {
        var startDate = scheme.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = scheme.EndDate.ToDateTime(TimeOnly.MaxValue);
        return schemeInvoices
            .Where(x => x.CustomerId == invoice.SecondaryCustomerId
                && x.InvoiceDate >= startDate
                && x.InvoiceDate <= endDate)
            .Sum(x => x.Amount);
    }

    private static SchemeResult CalculateSchemeResult(decimal invoiceAmount, decimal cumulativeAmount, LoyaltyScheme scheme)
    {
        var slabs = scheme.Slabs
            .Where(x => x.DeletedAt == null)
            .OrderBy(x => x.ValueFrom)
            .ThenBy(x => x.SortOrder)
            .ToList();

        var achieved = slabs.LastOrDefault(slab => cumulativeAmount >= slab.ValueFrom && (!slab.ValueTo.HasValue || cumulativeAmount <= slab.ValueTo.Value));
        if (achieved is not null)
        {
            var points = string.Equals(scheme.BasedOn, "Percentage", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(invoiceAmount * achieved.RewardValue / 100, 2)
                : achieved.RewardValue;

            return new SchemeResult(points, achieved.RewardValue, achieved.TierName, null);
        }

        var next = slabs.FirstOrDefault(slab => cumulativeAmount < slab.ValueFrom);
        if (next is null) return new SchemeResult(0, null, null, null);

        var remaining = next.ValueFrom - cumulativeAmount;
        var rewardText = string.Equals(scheme.BasedOn, "Percentage", StringComparison.OrdinalIgnoreCase)
            ? $"{next.RewardValue:0.##}%"
            : $"Rs. {next.RewardValue:0.##}";
        return new SchemeResult(0, null, null, $"Add Rs. {remaining:0.##} more to get {rewardText}");
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

    private static string OwnerName(Customer customer) => ReadField(customer, "owner_name") ?? customer.Name;

    private static string ShopName(Customer customer) => ReadField(customer, "shop_name") ?? customer.Name;

    private static string MobileNumber(Customer customer)
    {
        var mobileNumbers = ReadField(customer, "mobile_numbers");
        var firstMobile = mobileNumbers?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return firstMobile ?? customer.Mobile ?? string.Empty;
    }

    private static ulong? CityId(Customer customer) =>
        ulong.TryParse(ReadField(customer, "city_id"), out var cityId) ? cityId : null;

    private static ulong? AssignedEmployeeId(Customer customer) =>
        FirstULong(ReadField(customer, "employee_id")) ?? FirstULong(ReadField(customer, "sales_executive_id")) ?? customer.ExecutiveId;

    private static ulong? AssignedDistributorId(Customer customer) =>
        FirstULong(ReadField(customer, "distributor_name")) ?? FirstULong(ReadField(customer, "agri_distributor")) ?? customer.ParentId;

    private static string? AssignedZoneName(Customer customer, IReadOnlyDictionary<ulong, string> zones) =>
        zones.TryGetValue(customer.Id, out var zoneName) ? zoneName : null;

    private static string? AssignedEmployeeName(Customer customer, IReadOnlyDictionary<ulong, string> employees) =>
        employees.TryGetValue(customer.Id, out var employeeName) ? employeeName : null;

    private static string? AssignedDistributorName(Customer customer, IReadOnlyDictionary<ulong, string> distributors) =>
        distributors.TryGetValue(customer.Id, out var distributorName) ? distributorName : null;

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

    private static string? CityName(Customer customer, IReadOnlyDictionary<ulong, string> cities)
    {
        var cityId = CityId(customer);
        return cityId.HasValue && cities.TryGetValue(cityId.Value, out var cityName) ? cityName : null;
    }

    private static string? ReadField(Customer customer, string key)
    {
        if (string.IsNullOrWhiteSpace(customer.CustomFields)) return null;
        try
        {
            var fields = JsonSerializer.Deserialize<Dictionary<string, string?>>(customer.CustomFields, JsonOptions);
            return fields is not null && fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
        }
        catch
        {
            return null;
        }
    }

    private static string StatusLabel(int status) => status switch
    {
        NewInvoice.StatusApprovedSs => "Approved By SS",
        NewInvoice.StatusApprovedSales => "Approved By Sales",
        NewInvoice.StatusApprovedHo => "Approved By HO",
        NewInvoice.StatusRejected => "Rejected",
        _ => "Pending"
    };

    private sealed class InvoiceRow
    {
        public NewInvoice Invoice { get; init; } = null!;
        public Customer Customer { get; init; } = null!;
        public User? Creator { get; init; }
        public Branch? Branch { get; init; }
    }

    private sealed record SchemeInvoiceAmount(ulong InvoiceId, ulong CustomerId, DateTime InvoiceDate, decimal Amount);

    private sealed record SchemeResult(decimal Points, decimal? RewardValue, string? TierName, string? HintMessage);
}
