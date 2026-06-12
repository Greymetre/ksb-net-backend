using Application.DTOs.UserTargets;
using Application.DTOs.Users;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UserTargetRepository : IUserTargetRepository
{
    private static readonly string[] MonthOrder = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    private readonly AppDbContext _dbContext;

    public UserTargetRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<UserTargetDto>> GetTargetsAsync(UserTargetFilterDto filter, CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_dbContext.SalesTargetUsers.AsNoTracking(), filter);

        var rows = await (
            from target in query
            join userRow in _dbContext.Users.AsNoTracking() on target.UserId equals userRow.Id into users
            from user in users.DefaultIfEmpty()
            join branchRow in _dbContext.Branches.AsNoTracking() on target.BranchId equals branchRow.Id into branches
            from branch in branches.DefaultIfEmpty()
            join designationRow in _dbContext.Designations.AsNoTracking() on user.DesignationId equals designationRow.Id into designations
            from designation in designations.DefaultIfEmpty()
            join divisionRow in _dbContext.Divisions.AsNoTracking() on user.DivisionId equals divisionRow.Id into divisions
            from division in divisions.DefaultIfEmpty()
            orderby target.Id
            select new TargetProjection(
                target.Id,
                target.UserId,
                target.BranchId,
                user.EmployeeCodes,
                user.Name,
                user.SalesType,
                designation.DesignationName,
                branch.BranchName,
                user.DivisionId,
                division.DivisionName,
                target.Type,
                target.Month,
                target.Year,
                target.Target,
                target.Achievement,
                target.AchievementPercent,
                target.QuantityTarget,
                target.QuantityAchievement,
                target.QuantityAchievementPercent))
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            rows = rows.Where(x =>
                (x.UserName ?? string.Empty).ToLowerInvariant().Contains(search)
                || (x.EmployeeCode ?? string.Empty).ToLowerInvariant().Contains(search)
                || (x.BranchName ?? string.Empty).ToLowerInvariant().Contains(search)
                || (x.DesignationName ?? string.Empty).ToLowerInvariant().Contains(search)
                || (x.Type ?? string.Empty).ToLowerInvariant().Contains(search)).ToList();
        }

        var result = new List<UserTargetDto>();
        foreach (var row in rows)
        {
            var achievement = await CalculateAchievementAsync(row, cancellationToken);
            result.Add(new UserTargetDto
            {
                Id = row.Id,
                UserId = row.UserId,
                BranchId = row.BranchId,
                EmployeeCode = row.EmployeeCode,
                UserName = row.UserName,
                DesignationName = row.DesignationName,
                BranchName = row.BranchName,
                DivisionId = row.DivisionId,
                DivisionName = row.DivisionName,
                Type = row.Type ?? string.Empty,
                Month = row.Month ?? string.Empty,
                Year = row.Year?.ToString() ?? string.Empty,
                Target = row.Target ?? 0,
                Achievement = achievement.Value,
                AchievementPercent = Percent(achievement.Value, row.Target),
                QuantityTarget = row.QuantityTarget,
                QuantityAchievement = achievement.Quantity,
                QuantityAchievementPercent = Percent(achievement.Quantity, row.QuantityTarget)
            });
        }

        return result;
    }

    public async Task<UserTargetDto?> GetTargetDtoAsync(ulong id, CancellationToken cancellationToken) =>
        (await GetTargetsAsync(new UserTargetFilterDto(), cancellationToken)).FirstOrDefault(x => x.Id == id);

    public Task<SalesTargetUser?> GetTargetAsync(ulong id, CancellationToken cancellationToken) =>
        _dbContext.SalesTargetUsers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<UserTargetOptionsDto> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.Now.Year;
        var visibleUserIds = await ReportingVisibility.GetVisibleUserIdsAsync(_dbContext, actorUserId, cancellationToken);
        return new UserTargetOptionsDto
        {
            Users = await ReportingVisibility.InternalUsersQuery(_dbContext, _dbContext.Users.AsNoTracking())
                .Where(user => user.Active == "Y" && visibleUserIds.Contains(user.Id))
                .OrderBy(x => x.Name)
                .Select(x => new OptionDto { Id = x.Id, Name = x.Name })
                .ToListAsync(cancellationToken),
            Branches = await _dbContext.Branches.AsNoTracking()
                .OrderBy(x => x.BranchName)
                .Select(x => new OptionDto { Id = x.Id, Name = x.BranchName })
                .ToListAsync(cancellationToken),
            Divisions = await _dbContext.Divisions.AsNoTracking()
                .OrderBy(x => x.DivisionName)
                .Select(x => new OptionDto { Id = x.Id, Name = x.DivisionName })
                .ToListAsync(cancellationToken),
            Years = Enumerable.Range(currentYear - 2, 5).ToArray()
        };
    }

    public Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken) =>
        _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<SalesTargetUser?> FindTargetAsync(ulong userId, ulong? branchId, string month, int year, CancellationToken cancellationToken) =>
        _dbContext.SalesTargetUsers.FirstOrDefaultAsync(x =>
            x.UserId == userId
            && x.BranchId == branchId
            && x.Month == month
            && x.Year == year,
            cancellationToken);

    public async Task AddTargetAsync(SalesTargetUser target, CancellationToken cancellationToken) =>
        await _dbContext.SalesTargetUsers.AddAsync(target, cancellationToken);

    public async Task<bool> DeleteTargetAsync(ulong id, CancellationToken cancellationToken)
    {
        var target = await _dbContext.SalesTargetUsers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (target is null) return false;
        _dbContext.SalesTargetUsers.Remove(target);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<SalesTargetUser> ApplyFilters(IQueryable<SalesTargetUser> query, UserTargetFilterDto filter)
    {
        if (filter.BranchId.HasValue) query = query.Where(x => x.BranchId == filter.BranchId.Value);
        if (filter.UserId.HasValue) query = query.Where(x => x.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Type)) query = query.Where(x => x.Type == filter.Type);
        if (!string.IsNullOrWhiteSpace(filter.Month)) query = query.Where(x => x.Month == filter.Month);

        if (filter.DivisionId.HasValue)
        {
            var userIds = _dbContext.Users.AsNoTracking()
                .Where(x => x.DivisionId == filter.DivisionId.Value)
                .Select(x => x.Id);
            query = query.Where(x => x.UserId.HasValue && userIds.Contains(x.UserId.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.FinancialYear))
        {
            var parts = filter.FinancialYear.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var startYear) && int.TryParse(parts[1], out var endYear))
            {
                query = query.Where(x =>
                    (x.Year == startYear && new[] { "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" }.Contains(x.Month))
                    || (x.Year == endYear && new[] { "Jan", "Feb", "Mar" }.Contains(x.Month)));
            }
        }

        return query;
    }

    private async Task<(decimal Value, decimal Quantity)> CalculateAchievementAsync(TargetProjection row, CancellationToken cancellationToken)
    {
        if (!DateRange(row.Month, row.Year, out var start, out var end))
        {
            return (row.Achievement ?? 0, row.QuantityAchievement ?? 0);
        }

        if (string.Equals(row.SalesType, "Primary", StringComparison.OrdinalIgnoreCase) || string.Equals(row.Type, "primary", StringComparison.OrdinalIgnoreCase))
        {
            var primary = await _dbContext.PrimarySales.AsNoTracking()
                .Where(x => x.InvoiceDate >= start && x.InvoiceDate <= end && x.EmpCode == row.EmployeeCode)
                .GroupBy(_ => 1)
                .Select(g => new { Value = g.Sum(x => x.NetAmount), Quantity = g.Sum(x => x.Quantity) })
                .FirstOrDefaultAsync(cancellationToken);

            return (Math.Round((primary?.Value ?? 0) / 100000m, 2), primary?.Quantity ?? row.QuantityAchievement ?? 0);
        }

        if (row.UserId.HasValue)
        {
            var secondary = await _dbContext.Orders.AsNoTracking()
                .Where(x => x.OrderDate >= start && x.OrderDate <= end && x.CreatedBy == row.UserId.Value)
                .GroupBy(_ => 1)
                .Select(g => new { Value = g.Sum(x => x.SubTotal), Quantity = g.Sum(x => x.TotalQty) })
                .FirstOrDefaultAsync(cancellationToken);

            if ((secondary?.Value ?? 0) > 1)
            {
                return (Math.Round(((secondary!.Value - (secondary.Value / 100m)) / 100000m), 2), secondary.Quantity);
            }
        }

        return (row.Achievement ?? 0, row.QuantityAchievement ?? 0);
    }

    private static bool DateRange(string? month, int? year, out DateTime start, out DateTime end)
    {
        start = DateTime.MinValue;
        end = DateTime.MinValue;
        var monthIndex = Array.IndexOf(MonthOrder, month) + 1;
        if (monthIndex <= 0 || !year.HasValue) return false;
        var yearNumber = year.Value;
        start = new DateTime(yearNumber, monthIndex, 1);
        end = start.AddMonths(1).AddDays(-1);
        return true;
    }

    private static string Percent(decimal? value, decimal? target) =>
        target.HasValue && target.Value != 0
            ? $"{Math.Round(((value ?? 0) * 100m) / target.Value, 2):0.00}%"
            : string.Empty;

    private sealed record TargetProjection(
        ulong Id,
        ulong? UserId,
        ulong? BranchId,
        string? EmployeeCode,
        string? UserName,
        string? SalesType,
        string? DesignationName,
        string? BranchName,
        ulong? DivisionId,
        string? DivisionName,
        string? Type,
        string? Month,
        int? Year,
        decimal? Target,
        decimal? Achievement,
        decimal? AchievementPercent,
        decimal? QuantityTarget,
        decimal? QuantityAchievement,
        decimal? QuantityAchievementPercent);
}
