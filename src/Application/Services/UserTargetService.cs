using Application.Common;
using Application.DTOs.MasterData;
using Application.DTOs.UserTargets;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;
using System.Globalization;

namespace Application.Services;

public sealed class UserTargetService : IUserTargetService
{
    private static readonly string[] Months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    private readonly IUserTargetRepository _repository;

    public UserTargetService(IUserTargetRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetTargetsAsync(UserTargetFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("user_targets", await _repository.GetTargetsAsync(filter, cancellationToken));

    public async Task<LaravelApiResponse> GetTargetAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("user_target", await GetOrThrowAsync(_repository.GetTargetDtoAsync(id, cancellationToken), "User Target not found"));

    public async Task<LaravelApiResponse> GetOptionsAsync(CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("options", await _repository.GetOptionsAsync(cancellationToken));

    public async Task<LaravelApiResponse> CreateTargetAsync(UserTargetRequestDto request, CancellationToken cancellationToken)
    {
        var target = await BuildTargetAsync(new SalesTargetUser(), request, cancellationToken);
        target.CreatedAt = DateTime.Now;
        target.UpdatedAt = DateTime.Now;
        await _repository.AddTargetAsync(target, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("user_target", await _repository.GetTargetDtoAsync(target.Id, cancellationToken), "User Target Store Successfully");
    }

    public async Task<LaravelApiResponse> UpdateTargetAsync(ulong id, UserTargetRequestDto request, CancellationToken cancellationToken)
    {
        var target = await GetOrThrowAsync(_repository.GetTargetAsync(id, cancellationToken), "User Target not found");
        await BuildTargetAsync(target, request, cancellationToken);
        target.UpdatedAt = DateTime.Now;
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("user_target", await _repository.GetTargetDtoAsync(id, cancellationToken), "User Target Updated Successfully");
    }

    public async Task<LaravelApiResponse> DeleteTargetAsync(ulong id, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteTargetAsync(id, cancellationToken)) throw NotFound("User Target not found");
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "User Target Deleted Successfully");
    }

    public async Task<MasterDataFileDto> ExportTargetsAsync(UserTargetFilterDto filter, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetTargetsAsync(filter, cancellationToken);
        return CreateWorkbook(
            "sales_target_users.xlsx",
            ["employee_code", "user_name", "designation", "branch_name", "sales_type", "month", "year", "target", "achievement", "achievement_percent"],
            rows.Select(x => new object?[] { x.EmployeeCode, x.UserName, x.DesignationName, x.BranchName, x.Type, x.Month, x.Year, x.Target, x.Achievement, x.AchievementPercent }));
    }

    public Task<MasterDataFileDto> GetTemplateAsync(CancellationToken cancellationToken)
    {
        var currentYear = DateTime.Now.Year;
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");
        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 9;

        var headings = new[] { "User Id", "Branch Id", "User Name", "Type" }
            .Concat(Enumerable.Range(4, 12).Select(index =>
            {
                var month = index <= 12 ? index : index - 12;
                var year = index <= 12 ? currentYear : currentYear + 1;
                return $"{month:00}/{year % 100:00}";
            }))
            .ToArray();

        for (var column = 0; column < headings.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = headings[column];
            worksheet.Cell(1, column + 1).Style.Font.Bold = true;
        }

        worksheet.Cell(2, 4).Value = "Add primary or secondary value only. Please remove this row before upload.";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(new MasterDataFileDto { FileName = "sales_target_users.xlsx", Content = stream.ToArray() });
    }

    public async Task<LaravelApiResponse> UploadTargetsAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        var result = await ImportRowsAsync(fileStream, async row =>
        {
            var userId = row.ULong("user_id") ?? throw BadRequest("The user id is required.");
            var user = await _repository.GetUserAsync(userId, cancellationToken) ?? throw NotFound("User not found.");
            var branchId = row.ULong("branch_id") ?? FirstBranchId(user.BranchId);
            var type = NormalizeType(row.Value("type") ?? user.SalesType);

            var updatedAny = false;
            foreach (var monthValue in row.MonthValues())
            {
                var target = await UpsertAsync(userId, branchId, type, monthValue.Month, monthValue.Year, monthValue.Target, monthValue.QuantityTarget, cancellationToken);
                updatedAny = updatedAny || target;
            }

            return updatedAny;
        }, cancellationToken);

        return LaravelApiResponse.Success("import", result, "User Target Import Completed");
    }

    private async Task<bool> UpsertAsync(ulong userId, ulong? branchId, string type, string month, string year, decimal targetValue, decimal? quantityTarget, CancellationToken cancellationToken)
    {
        var parsedYear = ParseYear(year);
        var target = await _repository.FindTargetAsync(userId, branchId, month, parsedYear, cancellationToken);
        var updated = target is not null;
        target ??= new SalesTargetUser { CreatedAt = DateTime.Now };

        target.UserId = userId;
        target.BranchId = branchId ?? throw BadRequest("Selected user does not have branch assigned.");
        target.Type = type;
        target.Month = month;
        target.Year = parsedYear;
        target.Target = targetValue;
        target.QuantityTarget = quantityTarget;
        target.UpdatedAt = DateTime.Now;

        if (!updated) await _repository.AddTargetAsync(target, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return updated;
    }

    private async Task<SalesTargetUser> BuildTargetAsync(SalesTargetUser target, UserTargetRequestDto request, CancellationToken cancellationToken)
    {
        RequireId(request.UserId, "User is required.");
        RequireValue(request.Month, "Month is required.");
        RequireValue(request.Year, "Year is required.");
        if (!Months.Contains(request.Month)) throw BadRequest("Month is invalid.");
        if (request.Target is null) throw BadRequest("Target is required.");

        var user = await _repository.GetUserAsync(request.UserId!.Value, cancellationToken) ?? throw NotFound("User not found");
        var branchId = FirstBranchId(user.BranchId) ?? throw BadRequest("Selected user does not have branch assigned.");
        target.UserId = request.UserId;
        target.BranchId = branchId;
        target.Type = NormalizeType(request.Type ?? user.SalesType);
        target.Month = request.Month;
        target.Year = ParseYear(request.Year);
        target.Target = request.Target;
        return target;
    }

    private static MasterDataFileDto CreateWorkbook(string fileName, string[] headings, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");
        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 9;

        for (var column = 0; column < headings.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = TitleCaseHeading(headings[column]);
            worksheet.Cell(1, column + 1).Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < row.Length; column++)
            {
                worksheet.Cell(rowNumber, column + 1).Value = XLCellValue.FromObject(FormatExportValue(headings[column], row[column]));
            }
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new MasterDataFileDto { FileName = fileName, Content = stream.ToArray() };
    }

    private static async Task<UserTargetImportResultDto> ImportRowsAsync(Stream fileStream, Func<ExcelRow, Task<bool>> importRow, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed() ?? throw BadRequest("Import file is empty.");
        var headings = headerRow.CellsUsed().ToDictionary(cell => NormalizeHeading(cell.GetFormattedString()), cell => cell.Address.ColumnNumber);
        var monthColumns = headerRow.CellsUsed()
            .Select(cell => TryParseMonthHeading(cell, out var month, out var year)
                ? new MonthColumn(cell.Address.ColumnNumber, month, year, false)
                : (MonthColumn?)null)
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .ToArray();

        var totalRows = 0;
        var importedRows = 0;
        var updatedRows = 0;
        var errors = new List<string>();

        foreach (var worksheetRow in worksheet.RowsUsed().Where(row => row.RowNumber() > headerRow.RowNumber()))
        {
            if (worksheetRow.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetString()))) continue;
            if (worksheetRow.RowNumber() == 2 && worksheetRow.Cell(4).GetString().Contains("primary or secondary", StringComparison.OrdinalIgnoreCase)) continue;

            totalRows++;
            try
            {
                var updated = await importRow(new ExcelRow(worksheetRow, headings, monthColumns));
                if (updated) updatedRows++; else importedRows++;
            }
            catch (Exception exception) when (exception is LaravelHttpException or FormatException or InvalidOperationException)
            {
                errors.Add($"Row {worksheetRow.RowNumber()}: {exception.Message}");
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return new UserTargetImportResultDto { TotalRows = totalRows, ImportedRows = importedRows, UpdatedRows = updatedRows, FailedRows = errors.Count, Errors = errors };
    }

    private static bool TryParseMonthHeading(IXLCell cell, out string month, out string year)
    {
        month = string.Empty;
        year = string.Empty;
        var text = cell.GetFormattedString().Trim().Replace("\\", "/");
        var parts = text.Split('/', '-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && int.TryParse(parts[0], out var monthNumber) && monthNumber is >= 1 and <= 12 && int.TryParse(parts[1], out var yearNumber))
        {
            yearNumber = yearNumber < 100 ? 2000 + yearNumber : yearNumber;
            month = new DateTime(yearNumber, monthNumber, 1).ToString("MMM", CultureInfo.InvariantCulture);
            year = yearNumber.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return false;
        month = date.ToString("MMM", CultureInfo.InvariantCulture);
        year = date.Year.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    private static string NormalizeType(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is "primary" or "secondary") return normalized;
        throw BadRequest("The type name field either have primary or secondary value.");
    }

    private static int ParseYear(string? value)
    {
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year is >= 1901 and <= 2155) return year;
        throw BadRequest("Year is invalid.");
    }

    private static ulong? FirstBranchId(string? branchId)
    {
        var first = branchId?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return ulong.TryParse(first, out var parsed) ? parsed : null;
    }

    private static string? ToTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());
    }

    private static string TitleCaseHeading(string heading) => ToTitleCase(heading.Replace("_", " ")) ?? heading;

    private static object? FormatExportValue(string heading, object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return value;
        var normalized = NormalizeHeading(heading);
        if (normalized is "employee_code" or "sales_type" or "month" or "year" or "achievement_percent") return value;
        return ToTitleCase(text);
    }

    private static string NormalizeHeading(string heading) =>
        heading.Trim().ToLowerInvariant().Replace(" ", "_");

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

    private readonly record struct MonthValue(string Month, string Year, decimal Target, decimal? QuantityTarget);
    private readonly record struct MonthColumn(int Column, string Month, string Year, bool Quantity);

    private sealed class ExcelRow
    {
        private readonly IXLRow _row;
        private readonly IReadOnlyDictionary<string, int> _headings;
        private readonly IReadOnlyCollection<MonthColumn> _monthColumns;

        public ExcelRow(IXLRow row, IReadOnlyDictionary<string, int> headings, IReadOnlyCollection<MonthColumn> monthColumns)
        {
            _row = row;
            _headings = headings;
            _monthColumns = monthColumns;
        }

        public string? Value(string heading)
        {
            return _headings.TryGetValue(NormalizeHeading(heading), out var column)
                ? NormalizeText(_row.Cell(column).GetFormattedString())
                : null;
        }

        public ulong? ULong(string heading)
        {
            var value = Value(heading);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return ulong.TryParse(value, out var parsed) ? parsed : throw new FormatException($"{heading} must be numeric.");
        }

        public IEnumerable<MonthValue> MonthValues()
        {
            foreach (var column in _monthColumns)
            {
                var target = DecimalCell(column.Column);
                if (!target.HasValue) continue;
                var quantity = DecimalCell(column.Column + 1);
                yield return new MonthValue(column.Month, column.Year, target.Value, quantity);
            }
        }

        private decimal? DecimalCell(int column)
        {
            var cell = _row.Cell(column);
            if (cell.IsEmpty()) return null;
            if (cell.TryGetValue<decimal>(out var number)) return number;
            var text = NormalizeText(cell.GetFormattedString());
            if (string.IsNullOrWhiteSpace(text)) return null;
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : throw new FormatException($"Column {column} must be numeric.");
        }
    }

    private static string? NormalizeText(string? value)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
