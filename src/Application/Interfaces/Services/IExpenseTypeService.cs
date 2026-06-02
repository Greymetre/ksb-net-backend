using Application.DTOs.Expenses;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IExpenseTypeService
{
    Task<LaravelApiResponse> GetExpenseTypesAsync(string? search, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetExpenseTypeAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetOptionsAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateExpenseTypeAsync(ExpenseTypeRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateExpenseTypeAsync(ulong id, ExpenseTypeRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetExpenseTypeActiveAsync(ulong id, string? active, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteExpenseTypeAsync(ulong id, CancellationToken cancellationToken);
}
