using Application.DTOs.Expenses;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ExpenseTypeRepository : IExpenseTypeRepository
{
    private readonly AppDbContext _dbContext;

    public ExpenseTypeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ExpenseTypeDto>> GetExpenseTypesAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.ExpenseTypes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        var rows = await query
            .OrderByDescending(x => x.Id)
            .Select(x => new ExpenseTypeDto
            {
                Id = x.Id,
                Name = x.Name,
                Rate = x.Rate,
                IsActive = x.IsActive,
                Active = x.IsActive == 1 ? "Y" : "N",
                AllowanceTypeId = x.AllowanceTypeId,
                PayrollId = x.PayrollId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.AllowanceTypeName = ExpenseTypeLookups.AllowanceTypeName(row.AllowanceTypeId);
            row.PayrollName = ExpenseTypeLookups.PayrollName(row.PayrollId);
        }

        return rows;
    }

    public async Task<ExpenseTypeDto?> GetExpenseTypeDtoAsync(ulong id, CancellationToken cancellationToken) =>
        (await GetExpenseTypesAsync(null, cancellationToken)).FirstOrDefault(x => x.Id == id);

    public Task<ExpenseType?> GetExpenseTypeAsync(ulong id, CancellationToken cancellationToken) =>
        _dbContext.ExpenseTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddExpenseTypeAsync(ExpenseType expenseType, CancellationToken cancellationToken) =>
        await _dbContext.ExpenseTypes.AddAsync(expenseType, cancellationToken);

    public async Task<bool> DeleteExpenseTypeAsync(ulong id, CancellationToken cancellationToken)
    {
        var expenseType = await _dbContext.ExpenseTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (expenseType is null) return false;
        _dbContext.ExpenseTypes.Remove(expenseType);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
