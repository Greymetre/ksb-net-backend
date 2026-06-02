using Application.DTOs.Expenses;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IExpenseService
{
    Task<LaravelApiResponse> GetExpensesAsync(ExpenseFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetExpenseAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetOptionsAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateExpenseAsync(ExpenseRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateExpenseAsync(ulong id, ExpenseRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> AddAttachmentsAsync(ulong id, IReadOnlyCollection<ExpenseUploadDto> uploads, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteExpenseAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetStatusAsync(ulong id, ExpenseStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
}
