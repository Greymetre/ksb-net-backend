using Application.DTOs.CityAssignments;
using Application.Common;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;
using System.Globalization;

namespace Application.Services;

public sealed class CityAssignmentService : ICityAssignmentService
{
    private readonly ICityAssignmentRepository _repository;

    public CityAssignmentService(ICityAssignmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetAssignmentsAsync(CityAssignmentFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("assignments", await _repository.GetAssignmentsAsync(filter, cancellationToken));

    public async Task<LaravelApiResponse> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("options", await _repository.GetOptionsAsync(actorUserId, cancellationToken));

    public async Task<LaravelApiResponse> SaveAssignmentAsync(CityAssignmentRequestDto request, CancellationToken cancellationToken)
    {
        var userId = RequireId(request.UserId, "User is required.");
        var cityIds = ResolveCityIds(request);
        if (!await _repository.UserExistsAsync(userId, cancellationToken)) throw NotFound("User not found.");
        var user = await _repository.GetUserByIdAsync(userId, cancellationToken);

        foreach (var cityId in cityIds)
        {
            if (!await _repository.CityExistsAsync(cityId, cancellationToken)) throw NotFound($"City {cityId} not found.");
            await SaveSingleAssignmentAsync(userId, cityId, user?.ReportingId, cancellationToken);
        }

        return LaravelApiResponse.MessageOnly("success", "User city assigned successfully.");
    }

    private async Task SaveSingleAssignmentAsync(ulong userId, ulong cityId, ulong? reportingId, CancellationToken cancellationToken)
    {
        var assignment = await _repository.GetAssignmentByUserCityAsync(userId, cityId, cancellationToken);
        if (assignment is null)
        {
            assignment = new UserCityAssign
            {
                UserId = userId,
                CityId = cityId,
                ReportingId = reportingId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _repository.AddAssignmentAsync(assignment, cancellationToken);
        }
        else
        {
            assignment.ReportingId = reportingId;
            assignment.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<LaravelApiResponse> DeleteAssignmentAsync(ulong id, CancellationToken cancellationToken)
    {
        var assignment = await _repository.GetAssignmentEntityAsync(id, cancellationToken) ?? throw NotFound("User city assignment not found.");
        await _repository.DeleteAssignmentAsync(assignment, cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "User city assignment deleted successfully.");
    }

    public async Task<CityAssignmentFileDto> ExportAssignmentsAsync(CityAssignmentFilterDto filter, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetAssignmentsAsync(filter, cancellationToken);
        return Workbook("users.xlsx",
            ["id", "user_id", "user_name", "reportingid", "reporting_name", "city_id", "city_name", "grade", "district_id", "district_name", "status_id", "state_name", "Delete"],
            rows.Select(x => new object?[]
            {
                x.Id,
                x.UserId,
                x.UserName,
                x.ReportingId,
                x.ReportingName,
                x.CityId,
                x.CityName,
                x.Grade,
                x.DistrictId,
                x.DistrictName,
                x.StateId,
                x.StateName,
                ""
            }));
    }

    public Task<CityAssignmentFileDto> GetTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Workbook("user-city-template.xlsx", ["user_id", "city_id", "Delete"], []));

    public async Task<LaravelApiResponse> UploadAssignmentsAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheets.First();
        var header = sheet.FirstRowUsed() ?? throw BadRequest("Import file is empty.");
        var headings = header.CellsUsed().ToDictionary(x => Normalize(x.GetString()), x => x.Address.ColumnNumber);
        var imported = 0;
        var deleted = 0;

        foreach (var row in sheet.RowsUsed().Where(x => x.RowNumber() > header.RowNumber()))
        {
            var userId = ParseUlong(Cell(row, headings, "user_id"));
            var cityId = ParseUlong(Cell(row, headings, "city_id"));
            if (!userId.HasValue || !cityId.HasValue) continue;

            var delete = string.Equals(Cell(row, headings, "Delete"), "Y", StringComparison.OrdinalIgnoreCase);
            if (delete)
            {
                var assignment = await _repository.GetAssignmentByUserCityAsync(userId.Value, cityId.Value, cancellationToken);
                if (assignment is not null)
                {
                    await _repository.DeleteAssignmentAsync(assignment, cancellationToken);
                    deleted++;
                }
                continue;
            }

            await SaveAssignmentAsync(new CityAssignmentRequestDto
            {
                UserId = userId,
                CityId = cityId
            }, cancellationToken);
            imported++;
        }

        return LaravelApiResponse.Success("import", new { imported_rows = imported, deleted_rows = deleted }, "User city import completed.");
    }

    private static CityAssignmentFileDto Workbook(string fileName, string[] headings, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet1");
        for (var i = 0; i < headings.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headings[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++) sheet.Cell(rowNumber, i + 1).Value = XLCellValue.FromObject(row[i]);
            rowNumber++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new CityAssignmentFileDto { FileName = fileName, Content = stream.ToArray() };
    }

    private static ulong RequireId(ulong? value, string message) =>
        value is null or 0 ? throw BadRequest(message) : value.Value;

    private static IReadOnlyCollection<ulong> ResolveCityIds(CityAssignmentRequestDto request)
    {
        var ids = (request.CityIds ?? [])
            .Concat(request.CityId.HasValue ? [request.CityId.Value] : [])
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        return ids.Length == 0 ? throw BadRequest("City is required.") : ids;
    }

    private static ulong? ParseUlong(string? value) =>
        ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;

    private static string Normalize(string value) => value.Trim().ToLowerInvariant().Replace(" ", "_");

    private static string? Cell(IXLRow row, IReadOnlyDictionary<string, int> headings, string heading) =>
        headings.TryGetValue(Normalize(heading), out var column) ? row.Cell(column).GetFormattedString().Trim() : null;

    private static LaravelHttpException BadRequest(string message) => new(LaravelStatusCodes.BadRequest, message);
    private static LaravelHttpException NotFound(string message) => new(LaravelStatusCodes.NotFound, message);
}
