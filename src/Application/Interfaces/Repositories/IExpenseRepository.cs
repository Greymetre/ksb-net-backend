using Application.DTOs.Expenses;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IExpenseRepository
{
    Task<IReadOnlyCollection<ExpenseDto>> GetExpensesAsync(ExpenseFilterDto filter, CancellationToken cancellationToken);
    Task<ExpenseDto?> GetExpenseDtoAsync(ulong id, CancellationToken cancellationToken);
    Task<Expense?> GetExpenseAsync(ulong id, CancellationToken cancellationToken);
    Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken);
    Task<ExpenseType?> GetExpenseTypeAsync(ulong id, CancellationToken cancellationToken);
    Task<ExpenseOptionsDto> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken);
    Task AddExpenseAsync(Expense expense, CancellationToken cancellationToken);
    Task AddLogAsync(ExpenseLog log, CancellationToken cancellationToken);
    Task AddMediaAsync(Media media, CancellationToken cancellationToken);
    Task<bool> DeleteExpenseAsync(ulong id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
