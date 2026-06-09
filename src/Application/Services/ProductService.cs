using Application.Common;
using Application.DTOs.MasterData;
using Application.DTOs.Products;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Shared.Exceptions;
using Shared.Responses;
using System.Globalization;

namespace Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetSegmentsAsync(string? search, bool includeInactive, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("segments", await _repository.GetSegmentsAsync(search, includeInactive, cancellationToken));

    public async Task<MasterDataFileDto> ExportSegmentsAsync(string? search, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetSegmentsAsync(search, true, cancellationToken);
        return Workbook("segments.xlsx", ["id", "name", "active", "created_by", "created_at"],
            rows.Select(x => new object?[] { x.Id, x.Name, x.Active, x.CreatedBy, x.CreatedAt }));
    }

    public Task<MasterDataFileDto> GetSegmentTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Workbook("segments-template.xlsx", ["id", "name", "active"], []));

    public async Task<LaravelApiResponse> UploadSegmentsAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await Import(fileStream, async row =>
        {
            var request = new ProductSegmentRequestDto { Name = row.Value("name"), Active = row.Value("active") };
            if (row.ULong("id") is { } id && await _repository.GetSegmentAsync(id, cancellationToken) is not null)
            {
                await UpdateSegmentAsync(id, request, actorUserId, cancellationToken);
                return true;
            }
            await CreateSegmentAsync(request, actorUserId, cancellationToken);
            return false;
        }, cancellationToken);
        return LaravelApiResponse.Success("import", result, "Segment import completed");
    }

    public async Task<LaravelApiResponse> CreateSegmentAsync(ProductSegmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        Require(request.Name, "Segment name is required.");
        return LaravelApiResponse.Success("segment", await _repository.CreateSegmentAsync(request, actorUserId, cancellationToken), "Segment saved successfully");
    }

    public async Task<LaravelApiResponse> UpdateSegmentAsync(ulong id, ProductSegmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var segment = await _repository.UpdateSegmentAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("segment", segment ?? throw NotFound("Segment not found"), "Segment updated successfully");
    }

    public async Task<LaravelApiResponse> SetSegmentActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var segment = await _repository.SetSegmentActiveAsync(id, active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("segment", segment ?? throw NotFound("Segment not found"), "Segment status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteSegmentAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteSegmentAsync(id, actorUserId, cancellationToken)) throw NotFound("Segment not found");
        return LaravelApiResponse.MessageOnly("success", "Segment deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetFamiliesAsync(ulong? segmentId, string? search, bool includeInactive, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("families", await _repository.GetFamiliesAsync(segmentId, search, includeInactive, cancellationToken));

    public async Task<MasterDataFileDto> ExportFamiliesAsync(ulong? segmentId, string? search, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetFamiliesAsync(segmentId, search, true, cancellationToken);
        return Workbook("families.xlsx", ["id", "segment_id", "segment_name", "name", "active", "created_by", "created_at"],
            rows.Select(x => new object?[] { x.Id, x.SegmentId, x.SegmentName, x.Name, x.Active, x.CreatedBy, x.CreatedAt }));
    }

    public Task<MasterDataFileDto> GetFamilyTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Workbook("families-template.xlsx", ["id", "segment_id", "name", "active"], []));

    public async Task<LaravelApiResponse> UploadFamiliesAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await Import(fileStream, async row =>
        {
            var request = new ProductFamilyRequestDto { SegmentId = row.ULong("segment_id"), Name = row.Value("name"), Active = row.Value("active") };
            if (row.ULong("id") is { } id && await _repository.GetFamilyAsync(id, cancellationToken) is not null)
            {
                await UpdateFamilyAsync(id, request, actorUserId, cancellationToken);
                return true;
            }
            await CreateFamilyAsync(request, actorUserId, cancellationToken);
            return false;
        }, cancellationToken);
        return LaravelApiResponse.Success("import", result, "Family import completed");
    }

    public async Task<LaravelApiResponse> CreateFamilyAsync(ProductFamilyRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireId(request.SegmentId, "Segment is required.");
        Require(request.Name, "Family name is required.");
        await RequireSegmentAsync(request.SegmentId!.Value, cancellationToken);
        return LaravelApiResponse.Success("family", await _repository.CreateFamilyAsync(request, actorUserId, cancellationToken), "Family saved successfully");
    }

    public async Task<LaravelApiResponse> UpdateFamilyAsync(ulong id, ProductFamilyRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (request.SegmentId.HasValue) await RequireSegmentAsync(request.SegmentId.Value, cancellationToken);
        var family = await _repository.UpdateFamilyAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("family", family ?? throw NotFound("Family not found"), "Family updated successfully");
    }

    public async Task<LaravelApiResponse> SetFamilyActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var family = await _repository.SetFamilyActiveAsync(id, active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("family", family ?? throw NotFound("Family not found"), "Family status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteFamilyAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteFamilyAsync(id, actorUserId, cancellationToken)) throw NotFound("Family not found");
        return LaravelApiResponse.MessageOnly("success", "Family deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetProductsAsync(ulong? segmentId, ulong? familyId, string? search, bool includeInactive, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("products", await _repository.GetProductsAsync(segmentId, familyId, search, includeInactive, cancellationToken));

    public async Task<MasterDataFileDto> ExportProductsAsync(ulong? segmentId, ulong? familyId, string? search, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetProductsAsync(segmentId, familyId, search, true, cancellationToken);
        return Workbook("products.xlsx", ["id", "segment_id", "segment_name", "family_id", "family_name", "part_no", "product_name", "mrp", "attachment", "active"],
            rows.Select(x => new object?[] { x.Id, x.SegmentId, x.SegmentName, x.FamilyId, x.FamilyName, x.PartNo, x.ProductName, x.Mrp, x.Attachment, x.Active }));
    }

    public Task<MasterDataFileDto> GetProductTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Workbook("products-template.xlsx", ["id", "segment_id", "family_id", "part_no", "product_name", "mrp", "attachment", "active"], []));

    public async Task<LaravelApiResponse> UploadProductsAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await Import(fileStream, async row =>
        {
            var request = new ProductRequestDto
            {
                SegmentId = row.ULong("segment_id"),
                FamilyId = row.ULong("family_id"),
                PartNo = row.Value("part_no"),
                ProductName = row.Value("product_name"),
                Mrp = row.Decimal("mrp"),
                Attachment = row.Value("attachment"),
                Active = row.Value("active")
            };
            if (row.ULong("id") is { } id && await _repository.GetProductAsync(id, cancellationToken) is not null)
            {
                await UpdateProductAsync(id, request, actorUserId, cancellationToken);
                return true;
            }
            await CreateProductAsync(request, actorUserId, cancellationToken);
            return false;
        }, cancellationToken);
        return LaravelApiResponse.Success("import", result, "Product import completed");
    }

    public async Task<LaravelApiResponse> CreateProductAsync(ProductRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        await ValidateProductAsync(request, cancellationToken);
        return LaravelApiResponse.Success("product", await _repository.CreateProductAsync(request, actorUserId, cancellationToken), "Product saved successfully");
    }

    public async Task<LaravelApiResponse> UpdateProductAsync(ulong id, ProductRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (request.SegmentId.HasValue || request.FamilyId.HasValue) await ValidateProductRelationsAsync(request, cancellationToken);
        var product = await _repository.UpdateProductAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("product", product ?? throw NotFound("Product not found"), "Product updated successfully");
    }

    public async Task<LaravelApiResponse> SetProductActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var product = await _repository.SetProductActiveAsync(id, active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("product", product ?? throw NotFound("Product not found"), "Product status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteProductAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteProductAsync(id, actorUserId, cancellationToken)) throw NotFound("Product not found");
        return LaravelApiResponse.MessageOnly("success", "Product deleted successfully!");
    }

    private async Task ValidateProductAsync(ProductRequestDto request, CancellationToken cancellationToken)
    {
        RequireId(request.SegmentId, "Segment is required.");
        RequireId(request.FamilyId, "Family is required.");
        Require(request.PartNo, "Part no is required.");
        Require(request.ProductName, "Product name is required.");
        await ValidateProductRelationsAsync(request, cancellationToken);
    }

    private async Task ValidateProductRelationsAsync(ProductRequestDto request, CancellationToken cancellationToken)
    {
        if (request.SegmentId.HasValue) await RequireSegmentAsync(request.SegmentId.Value, cancellationToken);
        if (request.FamilyId.HasValue)
        {
            var family = await _repository.GetFamilyAsync(request.FamilyId.Value, cancellationToken) ?? throw NotFound("Family not found");
            if (request.SegmentId.HasValue && family.SegmentId != request.SegmentId) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Family does not belong to selected segment.");
        }
    }

    private async Task RequireSegmentAsync(ulong segmentId, CancellationToken cancellationToken)
    {
        if (await _repository.GetSegmentAsync(segmentId, cancellationToken) is null) throw NotFound("Segment not found");
    }

    private static MasterDataFileDto Workbook(string fileName, string[] headings, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");
        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 9;
        for (var i = 0; i < headings.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = TitleCaseHeading(headings[i]);
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }
        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++) worksheet.Cell(rowNumber, i + 1).Value = XLCellValue.FromObject(FormatExportValue(headings[i], row[i]));
            rowNumber++;
        }
        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new MasterDataFileDto { FileName = fileName, Content = stream.ToArray() };
    }

    private static async Task<MasterDataImportResultDto> Import(Stream stream, Func<ExcelRow, Task<bool>> importRow, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed() ?? throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Import file is empty.");
        var headings = headerRow.CellsUsed().ToDictionary(cell => Normalize(cell.GetString()), cell => cell.Address.ColumnNumber);
        var total = 0;
        var imported = 0;
        var updated = 0;
        var errors = new List<string>();
        foreach (var row in worksheet.RowsUsed().Where(x => x.RowNumber() > headerRow.RowNumber()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (row.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetString()))) continue;
            total++;
            try
            {
                if (await importRow(new ExcelRow(row, headings))) updated++; else imported++;
            }
            catch (Exception exception) when (exception is LaravelHttpException or FormatException or InvalidOperationException)
            {
                errors.Add($"Row {row.RowNumber()}: {exception.Message}");
            }
        }
        return new MasterDataImportResultDto { TotalRows = total, ImportedRows = imported, UpdatedRows = updated, FailedRows = errors.Count, Errors = errors };
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant().Replace(" ", "_");
    private static string TitleCaseHeading(string heading) => FirstCaps(heading.Replace("_", " "));
    private static object? FormatExportValue(string heading, object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return value;
        var normalized = Normalize(heading);
        if (normalized is "id" or "segment_id" or "family_id" or "part_no" or "mrp" or "attachment" or "created_at") return value;
        return FirstCaps(text);
    }
    private static string FirstCaps(string value)
    {
        var text = value.Trim().ToLowerInvariant();
        return text.Length == 0 ? text : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
    }
    private static void Require(string? value, string message) { if (string.IsNullOrWhiteSpace(value)) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, message); }
    private static void RequireId(ulong? value, string message) { if (value is null or 0) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, message); }
    private static LaravelHttpException NotFound(string message) => new(LaravelStatusCodes.NotFound, message);

    private sealed class ExcelRow
    {
        private readonly IXLRow _row;
        private readonly IReadOnlyDictionary<string, int> _headings;
        public ExcelRow(IXLRow row, IReadOnlyDictionary<string, int> headings) { _row = row; _headings = headings; }
        public string? Value(string heading) => _headings.TryGetValue(Normalize(heading), out var column) ? Text(_row.Cell(column).GetFormattedString()) : null;
        public ulong? ULong(string heading)
        {
            var value = Value(heading);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return ulong.TryParse(value, out var parsed) ? parsed : throw new FormatException($"{heading} must be numeric.");
        }
        public decimal? Decimal(string heading)
        {
            var value = Value(heading);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return decimal.TryParse(value, out var parsed) ? parsed : throw new FormatException($"{heading} must be numeric.");
        }
        private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
