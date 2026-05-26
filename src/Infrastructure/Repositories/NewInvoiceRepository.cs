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
    private const int MaxRows = 1000;
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
        return rows.Select(x => ToDto(x.Invoice, x.Customer, CityName(x.Customer, cities), x.Creator, x.Branch)).ToList();
    }

    public async Task<NewInvoiceDto?> GetInvoiceAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var distributorCustomerId = await GetDistributorCustomerIdAsync(actorUserId, cancellationToken);
        var row = await BaseQuery(distributorCustomerId).Where(x => x.Invoice.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var cities = await LoadCitiesAsync([CityId(row.Customer)], cancellationToken);
        var dto = ToDto(row.Invoice, row.Customer, CityName(row.Customer, cities), row.Creator, row.Branch);
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

    private static NewInvoiceDto ToDto(NewInvoice invoice, Customer customer, string? cityName, User? creator, Branch? branch) =>
        new()
        {
            Id = invoice.Id,
            SecondaryCustomerId = invoice.SecondaryCustomerId,
            RetailerCode = $"RET-{invoice.SecondaryCustomerId.ToString().PadLeft(4, '0')}",
            CustomerName = OwnerName(customer),
            ShopName = ShopName(customer),
            MobileNumber = MobileNumber(customer),
            CityName = cityName,
            ZoneName = branch?.BranchName,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            Amount = invoice.Amount,
            Points = invoice.Points,
            ApprovalStatus = invoice.ApprovalStatus,
            ApprovalStatusLabel = StatusLabel(invoice.ApprovalStatus),
            ApprovalRemark = invoice.ApprovalRemark,
            CreatedBy = invoice.CreatedBy,
            CreatedByName = creator?.Name,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };

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
}
