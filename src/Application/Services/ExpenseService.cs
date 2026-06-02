using Application.Common;
using Application.DTOs.Expenses;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;
using System.Globalization;

namespace Application.Services;

public sealed class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repository;

    public ExpenseService(IExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetExpensesAsync(ExpenseFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("expenses", await _repository.GetExpensesAsync(filter, cancellationToken));

    public async Task<LaravelApiResponse> GetExpenseAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("expense", await GetOrThrowAsync(_repository.GetExpenseDtoAsync(id, cancellationToken), "Expense not found"));

    public async Task<LaravelApiResponse> GetOptionsAsync(CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("options", await _repository.GetOptionsAsync(cancellationToken));

    public async Task<LaravelApiResponse> CreateExpenseAsync(ExpenseRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var expense = await BuildExpenseAsync(new Expense { CreatedAt = DateTime.Now }, request, actorUserId, cancellationToken);
        expense.UpdatedAt = DateTime.Now;
        await _repository.AddExpenseAsync(expense, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await AddLogAsync(expense.Id, actorUserId, "generated", cancellationToken);
        return LaravelApiResponse.Success("expense", await _repository.GetExpenseDtoAsync(expense.Id, cancellationToken), "expense added successfully");
    }

    public async Task<LaravelApiResponse> UpdateExpenseAsync(ulong id, ExpenseRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var expense = await GetOrThrowAsync(_repository.GetExpenseAsync(id, cancellationToken), "Expense not found");
        await BuildExpenseAsync(expense, request, actorUserId, cancellationToken);
        expense.UpdatedAt = DateTime.Now;
        await _repository.SaveChangesAsync(cancellationToken);
        await AddLogAsync(expense.Id, actorUserId, "updated", cancellationToken);
        return LaravelApiResponse.Success("expense", await _repository.GetExpenseDtoAsync(id, cancellationToken), "expense updated successfully");
    }

    public async Task<LaravelApiResponse> AddAttachmentsAsync(ulong id, IReadOnlyCollection<ExpenseUploadDto> uploads, CancellationToken cancellationToken)
    {
        if (uploads.Count == 0) return LaravelApiResponse.Success("expense", await _repository.GetExpenseDtoAsync(id, cancellationToken));
        _ = await GetOrThrowAsync(_repository.GetExpenseAsync(id, cancellationToken), "Expense not found");

        uint order = 1;
        foreach (var upload in uploads)
        {
            await _repository.AddMediaAsync(new Media
            {
                ModelType = "App\\Models\\Expenses",
                ModelId = id,
                Uuid = Guid.NewGuid().ToString(),
                CollectionName = "expense_file",
                Name = Path.GetFileNameWithoutExtension(upload.OriginalName),
                FileName = upload.FileName,
                MimeType = upload.MimeType,
                Disk = "public",
                ConversionsDisk = "public",
                Size = upload.Size,
                Manipulations = "[]",
                CustomProperties = "[]",
                GeneratedConversions = "[]",
                ResponsiveImages = "[]",
                OrderColumn = order++,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("expense", await _repository.GetExpenseDtoAsync(id, cancellationToken), "attachment uploaded successfully");
    }

    public async Task<LaravelApiResponse> DeleteExpenseAsync(ulong id, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteExpenseAsync(id, cancellationToken)) throw NotFound("Expense not found");
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Expense deleted successfully!");
    }

    public async Task<LaravelApiResponse> SetStatusAsync(ulong id, ExpenseStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var expense = await GetOrThrowAsync(_repository.GetExpenseAsync(id, cancellationToken), "Expense not found");
        var status = request.Status ?? throw BadRequest("Status is required.");
        if (!ExpenseStatusLookups.Statuses.ContainsKey(status)) throw BadRequest("Status is invalid.");

        if (status == 1)
        {
            var approveAmount = request.ApproveAmount ?? expense.ClaimAmount ?? 0;
            if (approveAmount > (expense.ClaimAmount ?? 0)) throw BadRequest("Approve amount greater than to claim amount");
            expense.ApproveAmount = approveAmount;
        }
        else if (status == 2)
        {
            RequireValue(request.Reason, "Please add reason if you want reject the expens.");
            expense.ApproveAmount = null;
        }
        else if (status == 0)
        {
            expense.ApproveAmount = null;
        }

        expense.CheckerStatus = status;
        expense.Reason = request.Reason;
        expense.ApproveRejectBy = actorUserId;
        expense.UpdatedAt = DateTime.Now;
        await _repository.SaveChangesAsync(cancellationToken);
        await AddLogAsync(expense.Id, actorUserId, StatusLogName(status), cancellationToken);
        return LaravelApiResponse.Success("expense", await _repository.GetExpenseDtoAsync(id, cancellationToken), StatusMessage(status));
    }

    private async Task<Expense> BuildExpenseAsync(Expense expense, ExpenseRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireId(request.ExpensesType, "Expense Type is required.");
        RequireId(request.UserId, "Employee is required.");
        RequireValue(request.Date, "Date is required.");

        var user = await _repository.GetUserAsync(request.UserId!.Value, cancellationToken) ?? throw NotFound("User not found");
        var expenseType = await _repository.GetExpenseTypeAsync(request.ExpensesType!.Value, cancellationToken) ?? throw NotFound("Expense Type not found");
        var claimAmount = ResolveClaimAmount(request, expenseType);

        expense.ExpensesType = request.ExpensesType;
        expense.UserId = request.UserId;
        expense.Date = request.Date;
        expense.ClaimAmount = claimAmount;
        expense.Note = request.Note;
        expense.Reason = request.Reason;
        expense.CreatedBy = actorUserId ?? expense.CreatedBy;

        if (expenseType.AllowanceTypeId == 1)
        {
            expense.StartKm = request.StartKm;
            expense.StopKm = request.StopKm;
            expense.TotalKm = CalculateTotalKm(request.StartKm, request.StopKm).ToString("0.##", CultureInfo.InvariantCulture);
        }
        else
        {
            expense.StartKm = null;
            expense.StopKm = null;
            expense.TotalKm = null;
        }

        if (request.ApproveAmount.HasValue) expense.ApproveAmount = request.ApproveAmount;
        _ = user;
        return expense;
    }

    private static decimal ResolveClaimAmount(ExpenseRequestDto request, ExpenseType expenseType)
    {
        if (expenseType.AllowanceTypeId == 1)
        {
            var totalKm = CalculateTotalKm(request.StartKm, request.StopKm);
            return Math.Round(totalKm * expenseType.Rate, 2, MidpointRounding.AwayFromZero);
        }

        return expenseType.Rate > 0
            ? expenseType.Rate
            : request.ClaimAmount ?? throw BadRequest("Claim Amount is required.");
    }

    private static decimal CalculateTotalKm(string? startKm, string? stopKm)
    {
        if (!decimal.TryParse(startKm, NumberStyles.Number, CultureInfo.InvariantCulture, out var start))
        {
            throw BadRequest("Start Km is required.");
        }

        if (!decimal.TryParse(stopKm, NumberStyles.Number, CultureInfo.InvariantCulture, out var stop))
        {
            throw BadRequest("End Km is required.");
        }

        if (stop < start) throw BadRequest("End Km must be greater than or equal to Start Km.");
        return stop - start;
    }

    private async Task AddLogAsync(ulong expenseId, ulong? actorUserId, string statusType, CancellationToken cancellationToken)
    {
        await _repository.AddLogAsync(new ExpenseLog
        {
            ExpenseId = expenseId,
            CreatedBy = actorUserId,
            LogDate = DateOnly.FromDateTime(DateTime.Today),
            StatusType = statusType,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        }, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static string StatusLogName(int status) => status switch
    {
        1 => "approved",
        2 => "rejected",
        3 => "checked",
        4 => "Checked By Reporting",
        5 => "Hold",
        _ => "unchecked"
    };

    private static string StatusMessage(int status) => status switch
    {
        1 => "Approve amount.",
        2 => "Expense reject successfully.",
        0 => "Status unchecked successfully",
        _ => "Status checked successfully"
    };

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
