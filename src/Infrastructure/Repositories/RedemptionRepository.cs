using System.Text.Json;
using Application.DTOs.Redemptions;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class RedemptionRepository : IRedemptionRepository
{
    private const int MaxRows = 50000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;

    public RedemptionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<RedemptionDto>> GetRedemptionsAsync(RedemptionFilterDto filter, CancellationToken cancellationToken)
    {
        var query =
            from redemption in _dbContext.LoyaltyRedemptions.AsNoTracking()
            where redemption.DeletedAt == null
            join customerRow in _dbContext.Customers.AsNoTracking() on redemption.CustomerId equals customerRow.Id into customers
            from customer in customers.DefaultIfEmpty()
            join userRow in _dbContext.Users.AsNoTracking() on redemption.CreatedBy equals userRow.Id into users
            from user in users.DefaultIfEmpty()
            orderby redemption.CreatedAt descending, redemption.Id descending
            select new { Redemption = redemption, Customer = customer, CreatedByName = user != null ? user.Name : null };

        if (filter.Status.HasValue) query = query.Where(x => x.Redemption.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.RedeemMode))
        {
            var mode = filter.RedeemMode.Trim();
            query = query.Where(x => x.Redemption.RedeemMode == mode);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Redemption.TransactionNo.Contains(search)
                || x.Redemption.Points.ToString().Contains(search)
                || (x.Customer != null && (x.Customer.Name.Contains(search)
                    || x.Customer.CustomerCode.Contains(search)
                    || (x.Customer.Mobile != null && x.Customer.Mobile.Contains(search))
                    || (x.Customer.CustomFields != null && x.Customer.CustomFields.Contains(search)))));
        }

        var rows = await query.Take(MaxRows).ToListAsync(cancellationToken);
        var cities = await LoadCitiesAsync(rows.Select(x => CityId(x.Customer)), cancellationToken);
        var distributors = await LoadDistributorNamesAsync(rows.Select(x => x.Customer).Where(x => x is not null)!, cancellationToken);

        return rows.Select(x => ToDto(
            x.Redemption,
            x.Customer,
            CityName(x.Customer, cities),
            DistributorName(x.Customer, distributors),
            x.CreatedByName)).ToList();
    }

    public async Task<IReadOnlyCollection<LoyaltyRedemption>> GetCustomerRedemptionsAsync(ulong customerId, CancellationToken cancellationToken) =>
        await _dbContext.LoyaltyRedemptions.AsNoTracking()
            .Where(x => x.DeletedAt == null
                && x.CustomerId == customerId
                && (x.Status == LoyaltyRedemption.StatusPending || x.Status == LoyaltyRedemption.StatusApproved))
            .ToListAsync(cancellationToken);

    public async Task<LoyaltyRedemption> CreateAsync(LoyaltyRedemption redemption, CancellationToken cancellationToken)
    {
        await _dbContext.LoyaltyRedemptions.AddAsync(redemption, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return redemption;
    }

    public Task<bool> TransactionNoExistsAsync(string transactionNo, CancellationToken cancellationToken) =>
        _dbContext.LoyaltyRedemptions.AnyAsync(x => x.TransactionNo == transactionNo, cancellationToken);

    private async Task<Dictionary<ulong, string>> LoadCitiesAsync(IEnumerable<ulong?> cityIds, CancellationToken cancellationToken)
    {
        var ids = cityIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        return ids.Length == 0
            ? []
            : await _dbContext.Cities.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.CityName, cancellationToken);
    }

    private async Task<Dictionary<ulong, string>> LoadDistributorNamesAsync(IEnumerable<Customer> customers, CancellationToken cancellationToken)
    {
        var customerDistributorIds = customers
            .Select(customer => new { CustomerId = customer.Id, DistributorId = FirstULong(ReadField(customer, "distributor_name")) ?? FirstULong(ReadField(customer, "agri_distributor")) ?? customer.ParentId })
            .Where(x => x.DistributorId.HasValue)
            .GroupBy(x => x.CustomerId)
            .Select(x => x.First())
            .ToList();

        var distributorIds = customerDistributorIds.Select(x => x.DistributorId!.Value).Distinct().ToArray();
        if (distributorIds.Length == 0) return [];

        var names = await _dbContext.Customers.AsNoTracking()
            .Where(x => distributorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        return customerDistributorIds
            .Where(x => x.DistributorId.HasValue && names.ContainsKey(x.DistributorId.Value))
            .ToDictionary(x => x.CustomerId, x => names[x.DistributorId!.Value]);
    }

    private static RedemptionDto ToDto(LoyaltyRedemption redemption, Customer? customer, string? cityName, string? distributorName, string? createdByName) => new()
    {
        Id = redemption.Id,
        TransactionNo = redemption.TransactionNo,
        CustomerId = redemption.CustomerId,
        CustomerCode = CustomerCode(customer, redemption.CustomerId),
        CustomerName = customer is null ? string.Empty : OwnerName(customer),
        CustomerTypeName = CustomerTypeName(customer?.CustomerType),
        MobileNumber = customer is null ? null : MobileNumber(customer),
        CityName = cityName,
        DistributorName = distributorName,
        LoyaltySchemeId = redemption.LoyaltySchemeId,
        SchemeName = redemption.SchemeName,
        WalletType = redemption.WalletType,
        RedeemMode = redemption.RedeemMode,
        Points = redemption.Points,
        AccountHolder = redemption.AccountHolder,
        AccountNumber = redemption.AccountNumber,
        BankName = redemption.BankName,
        IfscCode = redemption.IfscCode,
        Status = redemption.Status,
        StatusLabel = StatusLabel(redemption.Status),
        Remark = redemption.Remark,
        CreatedBy = redemption.CreatedBy,
        CreatedByName = createdByName,
        CreatedAt = redemption.CreatedAt
    };

    private static string StatusLabel(int status) => status switch
    {
        LoyaltyRedemption.StatusApproved => "Approved",
        LoyaltyRedemption.StatusRejected => "Rejected",
        LoyaltyRedemption.StatusHold => "Hold",
        _ => "Pending"
    };

    private static string CustomerTypeName(ulong? type) => type switch
    {
        1 => "Distributor",
        2 => "Retailer",
        3 => "Influencers",
        null => string.Empty,
        _ => $"Type {type}"
    };

    private static string CustomerCode(Customer? customer, ulong customerId)
    {
        if (!string.IsNullOrWhiteSpace(customer?.CustomerCode)) return customer.CustomerCode;
        return $"{CustomerPrefix(customer?.CustomerType)}-{customerId.ToString().PadLeft(4, '0')}";
    }

    private static string CustomerPrefix(ulong? customerType) => customerType switch
    {
        1 => "DIS",
        2 => "RET",
        3 => "INF",
        _ => "CUS"
    };

    private static string OwnerName(Customer customer) => ReadField(customer, "owner_name") ?? customer.Name;

    private static string MobileNumber(Customer customer) =>
        ReadField(customer, "mobile_numbers")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()
        ?? customer.Mobile
        ?? string.Empty;

    private static ulong? CityId(Customer? customer) =>
        customer is not null && ulong.TryParse(ReadField(customer, "city_id"), out var cityId) ? cityId : null;

    private static string? CityName(Customer? customer, IReadOnlyDictionary<ulong, string> cities)
    {
        var cityId = CityId(customer);
        return cityId.HasValue && cities.TryGetValue(cityId.Value, out var cityName) ? cityName : null;
    }

    private static string? DistributorName(Customer? customer, IReadOnlyDictionary<ulong, string> distributors) =>
        customer is not null && distributors.TryGetValue(customer.Id, out var distributorName) ? distributorName : null;

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
}
