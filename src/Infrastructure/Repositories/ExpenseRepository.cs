using Application.DTOs.Expenses;
using Application.DTOs.Users;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _dbContext;

    public ExpenseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ExpenseDto>> GetExpensesAsync(ExpenseFilterDto filter, CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_dbContext.Expenses.AsNoTracking(), filter);

        var rows = await (
            from expense in query
            join typeRow in _dbContext.ExpenseTypes.AsNoTracking() on expense.ExpensesType equals typeRow.Id into types
            from type in types.DefaultIfEmpty()
            join userRow in _dbContext.Users.AsNoTracking() on expense.UserId equals userRow.Id into users
            from user in users.DefaultIfEmpty()
            join designationRow in _dbContext.Designations.AsNoTracking() on user.DesignationId equals designationRow.Id into designations
            from designation in designations.DefaultIfEmpty()
            join branchRow in _dbContext.Branches.AsNoTracking() on user.PrimaryBranchId equals branchRow.Id into branches
            from branch in branches.DefaultIfEmpty()
            join divisionRow in _dbContext.Divisions.AsNoTracking() on user.DivisionId equals divisionRow.Id into divisions
            from division in divisions.DefaultIfEmpty()
            join approverRow in _dbContext.Users.AsNoTracking() on expense.ApproveRejectBy equals approverRow.Id into approvers
            from approver in approvers.DefaultIfEmpty()
            orderby expense.Id descending
            select new ExpenseDto
            {
                Id = expense.Id,
                ExpensesType = expense.ExpensesType,
                ExpenseTypeName = type.Name,
                UserId = expense.UserId,
                UserName = user.Name,
                EmployeeCode = user.EmployeeCodes,
                DesignationName = designation.DesignationName,
                BranchId = user.PrimaryBranchId,
                BranchName = branch.BranchName,
                DivisionId = user.DivisionId,
                DivisionName = division.DivisionName,
                Payroll = user.Payroll,
                Date = expense.Date ?? string.Empty,
                ClaimAmount = expense.ClaimAmount ?? 0,
                ApproveAmount = expense.ApproveAmount,
                StartKm = expense.StartKm,
                StopKm = expense.StopKm,
                TotalKm = expense.TotalKm,
                Note = expense.Note,
                CheckerStatus = expense.CheckerStatus,
                Reason = expense.Reason,
                ApproveRejectBy = expense.ApproveRejectBy,
                ApproveRejectByName = approver.Name,
                CreatedAt = expense.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var expenseIds = rows.Select(x => x.Id).ToArray();
        var media = await _dbContext.Media.AsNoTracking()
            .Where(x => x.ModelType == "App\\Models\\Expenses" && expenseIds.Contains(x.ModelId) && x.CollectionName == "expense_file")
            .OrderBy(x => x.OrderColumn)
            .Select(x => new { x.Id, x.ModelId, x.FileName, x.MimeType, x.Size })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.CheckerStatusName = ExpenseStatusLookups.StatusName(row.CheckerStatus);
            row.Attachments = media
                .Where(x => x.ModelId == row.Id)
                .Select(x => new ExpenseAttachmentDto
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    MimeType = x.MimeType,
                    Size = x.Size,
                    Url = $"/uploads/expenses/{x.FileName}"
                })
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            rows = rows.Where(x =>
                x.Id.ToString().Contains(term)
                || (x.Date ?? string.Empty).ToLowerInvariant().Contains(term)
                || (x.UserName ?? string.Empty).ToLowerInvariant().Contains(term)
                || (x.EmployeeCode ?? string.Empty).ToLowerInvariant().Contains(term)
                || (x.DesignationName ?? string.Empty).ToLowerInvariant().Contains(term)
                || (x.ExpenseTypeName ?? string.Empty).ToLowerInvariant().Contains(term)
                || x.CheckerStatusName.ToLowerInvariant().Contains(term)
                || (x.Note ?? string.Empty).ToLowerInvariant().Contains(term)).ToList();
        }

        return rows;
    }

    public async Task<ExpenseDto?> GetExpenseDtoAsync(ulong id, CancellationToken cancellationToken) =>
        (await GetExpensesAsync(new ExpenseFilterDto { ExpenseId = id }, cancellationToken)).FirstOrDefault();

    public Task<Expense?> GetExpenseAsync(ulong id, CancellationToken cancellationToken) =>
        _dbContext.Expenses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken) =>
        _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<ExpenseType?> GetExpenseTypeAsync(ulong id, CancellationToken cancellationToken) =>
        _dbContext.ExpenseTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<ExpenseOptionsDto> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken)
    {
        var visibleUserIds = await ReportingVisibility.GetVisibleUserIdsAsync(_dbContext, actorUserId, cancellationToken);
        return new ExpenseOptionsDto
        {
            Users = await ReportingVisibility.InternalUsersQuery(_dbContext, _dbContext.Users.AsNoTracking())
                .Where(user => user.Active == "Y" && visibleUserIds.Contains(user.Id))
                .OrderBy(x => x.Name)
                .Select(x => new OptionDto { Id = x.Id, Name = x.Name })
                .ToListAsync(cancellationToken),
            ExpenseTypes = (await new ExpenseTypeRepository(_dbContext).GetExpenseTypesAsync(null, cancellationToken)).Where(x => x.Active == "Y").ToArray(),
            Branches = await _dbContext.Branches.AsNoTracking()
                .OrderBy(x => x.BranchName)
                .Select(x => new OptionDto { Id = x.Id, Name = x.BranchName })
                .ToListAsync(cancellationToken),
            Divisions = await _dbContext.Divisions.AsNoTracking()
                .OrderBy(x => x.DivisionName)
                .Select(x => new OptionDto { Id = x.Id, Name = x.DivisionName })
                .ToListAsync(cancellationToken),
            Payrolls = ExpenseTypeLookups.Payrolls.Select(x => new OptionDto { Id = x.Key, Name = x.Value }).ToArray(),
            Statuses = ExpenseStatusLookups.Statuses.Select(x => new OptionDto { Id = (ulong)x.Key, Name = x.Value }).ToArray()
        };
    }

    public async Task AddExpenseAsync(Expense expense, CancellationToken cancellationToken) =>
        await _dbContext.Expenses.AddAsync(expense, cancellationToken);

    public async Task AddLogAsync(ExpenseLog log, CancellationToken cancellationToken) =>
        await _dbContext.ExpenseLogs.AddAsync(log, cancellationToken);

    public async Task AddMediaAsync(Media media, CancellationToken cancellationToken) =>
        await _dbContext.Media.AddAsync(media, cancellationToken);

    public async Task<bool> DeleteExpenseAsync(ulong id, CancellationToken cancellationToken)
    {
        var logs = _dbContext.ExpenseLogs.Where(x => x.ExpenseId == id);
        _dbContext.ExpenseLogs.RemoveRange(logs);
        var expense = await _dbContext.Expenses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (expense is null) return false;
        _dbContext.Expenses.Remove(expense);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Expense> ApplyFilters(IQueryable<Expense> query, ExpenseFilterDto filter)
    {
        if (filter.ExpenseId.HasValue) query = query.Where(x => x.Id == filter.ExpenseId.Value);
        if (filter.ExecutiveId.HasValue) query = query.Where(x => x.UserId == filter.ExecutiveId.Value);
        if (filter.ExpensesType.HasValue) query = query.Where(x => x.ExpensesType == filter.ExpensesType.Value);
        if (filter.Status.HasValue) query = query.Where(x => x.CheckerStatus == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.StartDate) && !string.IsNullOrWhiteSpace(filter.EndDate))
        {
            query = query.Where(x => string.Compare(x.Date, filter.StartDate) >= 0 && string.Compare(x.Date, filter.EndDate) <= 0);
        }

        if (filter.BranchId.HasValue || filter.DivisionId.HasValue || !string.IsNullOrWhiteSpace(filter.Payroll))
        {
            var users = _dbContext.Users.AsNoTracking().AsQueryable();
            if (filter.BranchId.HasValue) users = users.Where(x => x.PrimaryBranchId == filter.BranchId.Value);
            if (filter.DivisionId.HasValue) users = users.Where(x => x.DivisionId == filter.DivisionId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Payroll)) users = users.Where(x => x.Payroll == filter.Payroll);
            var userIds = users.Select(x => x.Id);
            query = query.Where(x => x.UserId.HasValue && userIds.Contains(x.UserId.Value));
        }

        return query;
    }
}
