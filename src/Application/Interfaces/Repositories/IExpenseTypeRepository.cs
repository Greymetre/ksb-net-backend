using Application.DTOs.Expenses;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IExpenseTypeRepository
{
    Task<IReadOnlyCollection<ExpenseTypeDto>> GetExpenseTypesAsync(string? search, CancellationToken cancellationToken);
    Task<ExpenseTypeDto?> GetExpenseTypeDtoAsync(ulong id, CancellationToken cancellationToken);
    Task<ExpenseType?> GetExpenseTypeAsync(ulong id, CancellationToken cancellationToken);
    Task AddExpenseTypeAsync(ExpenseType expenseType, CancellationToken cancellationToken);
    Task<bool> DeleteExpenseTypeAsync(ulong id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
