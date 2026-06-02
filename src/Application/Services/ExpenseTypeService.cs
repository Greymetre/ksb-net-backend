using Application.Common;
using Application.DTOs.Expenses;
using Application.DTOs.Users;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Services;

public sealed class ExpenseTypeService : IExpenseTypeService
{
    private readonly IExpenseTypeRepository _repository;

    public ExpenseTypeService(IExpenseTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetExpenseTypesAsync(string? search, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("expenses_types", await _repository.GetExpenseTypesAsync(search, cancellationToken));

    public async Task<LaravelApiResponse> GetExpenseTypeAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("expenses_type", await GetOrThrowAsync(_repository.GetExpenseTypeDtoAsync(id, cancellationToken), "Expense Type not found"));

    public Task<LaravelApiResponse> GetOptionsAsync(CancellationToken cancellationToken)
    {
        var options = new ExpenseTypeOptionsDto
        {
            AllowanceTypes = ExpenseTypeLookups.AllowanceTypes.Select(x => new OptionDto { Id = x.Key, Name = x.Value }).ToArray(),
            Payrolls = ExpenseTypeLookups.Payrolls.Select(x => new OptionDto { Id = x.Key, Name = x.Value }).ToArray()
        };
        return Task.FromResult(LaravelApiResponse.Success("options", options));
    }

    public async Task<LaravelApiResponse> CreateExpenseTypeAsync(ExpenseTypeRequestDto request, CancellationToken cancellationToken)
    {
        var expenseType = BuildExpenseType(new ExpenseType { CreatedAt = DateTime.Now }, request);
        expenseType.UpdatedAt = DateTime.Now;
        await _repository.AddExpenseTypeAsync(expenseType, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("expenses_type", await _repository.GetExpenseTypeDtoAsync(expenseType.Id, cancellationToken), "Data Store Successfully");
    }

    public async Task<LaravelApiResponse> UpdateExpenseTypeAsync(ulong id, ExpenseTypeRequestDto request, CancellationToken cancellationToken)
    {
        var expenseType = await GetOrThrowAsync(_repository.GetExpenseTypeAsync(id, cancellationToken), "Expense Type not found");
        BuildExpenseType(expenseType, request);
        expenseType.UpdatedAt = DateTime.Now;
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("expenses_type", await _repository.GetExpenseTypeDtoAsync(id, cancellationToken), "Data Updated Successfully");
    }

    public async Task<LaravelApiResponse> SetExpenseTypeActiveAsync(ulong id, string? active, CancellationToken cancellationToken)
    {
        var expenseType = await GetOrThrowAsync(_repository.GetExpenseTypeAsync(id, cancellationToken), "Expense Type not found");
        expenseType.IsActive = NormalizeActive(active) == "Y" ? 1 : 0;
        expenseType.UpdatedAt = DateTime.Now;
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("expenses_type", await _repository.GetExpenseTypeDtoAsync(id, cancellationToken), "Status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteExpenseTypeAsync(ulong id, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteExpenseTypeAsync(id, cancellationToken)) throw NotFound("Expense Type not found");
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Expense Type deleted successfully!");
    }

    private static ExpenseType BuildExpenseType(ExpenseType expenseType, ExpenseTypeRequestDto request)
    {
        RequireValue(request.Name, "Expense type name is required.");
        RequireId(request.AllowanceTypeId, "Allowance Type is required.");
        RequireId(request.PayrollId, "Grade is required.");
        if (!ExpenseTypeLookups.AllowanceTypes.ContainsKey(request.AllowanceTypeId!.Value)) throw BadRequest("Allowance Type is invalid.");
        if (!ExpenseTypeLookups.Payrolls.ContainsKey(request.PayrollId!.Value)) throw BadRequest("Grade is invalid.");
        if (request.Rate is < 0) throw BadRequest("Rate must be greater than or equal to 0.");

        expenseType.Name = request.Name!.Trim();
        expenseType.Rate = request.Rate ?? 0;
        expenseType.AllowanceTypeId = request.AllowanceTypeId.Value;
        expenseType.PayrollId = request.PayrollId;
        if (!string.IsNullOrWhiteSpace(request.Active)) expenseType.IsActive = NormalizeActive(request.Active) == "Y" ? 1 : 0;
        return expenseType;
    }

    private static string NormalizeActive(string? active)
    {
        var value = active?.Trim().ToUpperInvariant();
        if (value is "Y" or "1" or "ACTIVE") return "Y";
        if (value is "N" or "0" or "INACTIVE") return "N";
        throw BadRequest("Active value is invalid.");
    }

    private static async Task<T> GetOrThrowAsync<T>(Task<T?> task, string message)
    {
        var value = await task;
        return value ?? throw NotFound(message);
    }

    private static void RequireValue(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw BadRequest(message);
    }

    private static void RequireId(ulong? value, string message)
    {
        if (value is null or 0) throw BadRequest(message);
    }

    private static LaravelHttpException BadRequest(string message) =>
        new(LaravelStatusCodes.BadRequest, message);

    private static LaravelHttpException NotFound(string message) =>
        new(LaravelStatusCodes.NotFound, message);
}
