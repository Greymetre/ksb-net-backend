using Application.Common;
using Application.DTOs.Customers;
using Application.DTOs.MasterData;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Services;

public sealed class CustomerService : ICustomerService
{
    private static readonly string[] DistributorExportColumns =
    [
        "id", "customer_type", "name", "mobile", "email", "customer_code", "contact_number", "active",
        "legal_name", "trade_name", "distributor_code", "business_status", "business_start_date",
        "contact_person", "alternate_mobile", "address1", "shipping_address", "beat_id", "beat_route",
        "registration_type", "sales_executive_id", "supervisor_id", "customer_segment",
        "shop_image", "profile_image", "documents", "mou_file"
    ];

    private static readonly string[] RetailerExportColumns =
    [
        "id", "customer_type", "name", "mobile", "contact_number", "active",
        "owner_name", "shop_name", "mobile_numbers", "distributor_name", "agri_distributor", "employee_id", "beat_id", "whatsapp_number",
        "address_line", "country_id", "state_id", "district_id", "city_id", "pincode_id",
        "belt_area_market_name", "gst_number", "gst_attachment", "pan_number", "pan_attachment", "aadhar_attachment",
        "bank_account_type", "bank_name", "bank_account_number", "ifsc_code", "account_holder_name",
        "bank_proof", "shop_photo", "gps_location"
    ];

    private static readonly string[] ImportColumns = DistributorExportColumns.Concat(RetailerExportColumns).Distinct().ToArray();

    private static readonly HashSet<string> PreserveRawColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "mobile", "mobile_numbers", "email", "customer_code", "contact_number", "alternate_mobile", "whatsapp_number",
        "distributor_code", "business_start_date", "country_id", "state_id", "district_id", "city_id", "pincode_id",
        "beat_id", "sales_executive_id", "supervisor_id", "distributor_name", "agri_distributor", "employee_id",
        "shop_image", "profile_image", "documents", "mou_file", "gst_number", "gst_attachment", "pan_number",
        "pan_attachment", "aadhar_attachment", "bank_account_number", "ifsc_code", "bank_proof", "shop_photo", "gps_location"
    };

    private static readonly HashSet<string> KycDocumentKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "gst",
        "pan",
        "aadhar",
        "bank"
    };

    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetCustomersAsync(CustomerListFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("customers", await _repository.GetCustomersAsync(filter, cancellationToken));

    public async Task<LaravelApiResponse> GetCustomerAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("customer", await GetOrThrowAsync(_repository.GetCustomerAsync(id, cancellationToken), "Customer not found"));

    public async Task<LaravelApiResponse> CreateCustomerAsync(CustomerRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        NormalizeRequest(request);
        await ValidateAsync(request, null, cancellationToken);
        var customer = await _repository.CreateCustomerAsync(request, actorUserId, cancellationToken);
        await _repository.EnsureDistributorLoginUserAsync(customer.Id, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("customer", customer, "Customer created successfully");
    }

    public async Task<LaravelApiResponse> UpdateCustomerAsync(ulong id, CustomerRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        NormalizeRequest(request);
        await ValidateAsync(request, id, cancellationToken);
        var customer = await _repository.UpdateCustomerAsync(id, request, actorUserId, cancellationToken);
        if (customer is not null) await _repository.EnsureDistributorLoginUserAsync(customer.Id, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("customer", customer ?? throw NotFound("Customer not found"), "Customer updated successfully");
    }

    public async Task<LaravelApiResponse> ApproveKycDocumentAsync(ulong id, string documentKey, string? remark, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) throw new LaravelHttpException(LaravelStatusCodes.Unauthorized, "Unauthenticated.");
        var key = NormalizeKycDocumentKey(documentKey);
        var customer = await _repository.UpdateKycStatusAsync(id, key, "approved", remark, actorUserId.Value, cancellationToken);
        return LaravelApiResponse.Success("customer", customer ?? throw NotFound("Customer not found"), "KYC document approved successfully");
    }

    public async Task<LaravelApiResponse> RejectKycDocumentAsync(ulong id, string documentKey, string? remark, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) throw new LaravelHttpException(LaravelStatusCodes.Unauthorized, "Unauthenticated.");
        if (string.IsNullOrWhiteSpace(remark)) throw new LaravelHttpException(LaravelStatusCodes.NoContentLikeValidation, "Remark is required.");

        var key = NormalizeKycDocumentKey(documentKey);
        var customer = await _repository.UpdateKycStatusAsync(id, key, "rejected", remark, actorUserId.Value, cancellationToken);
        return LaravelApiResponse.Success("customer", customer ?? throw NotFound("Customer not found"), "KYC document rejected successfully");
    }

    public async Task<LaravelApiResponse> SetCustomerActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var customer = await _repository.SetCustomerActiveAsync(id, active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("customer", customer ?? throw NotFound("Customer not found"), "Customer status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteCustomerAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteCustomerAsync(id, actorUserId, cancellationToken)) throw NotFound("Customer not found");
        return LaravelApiResponse.MessageOnly("success", "Customer deleted successfully!");
    }

    public async Task<MasterDataFileDto> ExportCustomersAsync(CustomerListFilterDto filter, CancellationToken cancellationToken)
    {
        if (filter.CustomerType is not 1 and not 2)
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Customer type filter is required for export.");
        }

        var columns = ExportColumnsFor(filter.CustomerType);
        var rows = await _repository.GetCustomersAsync(filter, cancellationToken);
        return CreateWorkbook(
            $"customers-{CustomerTypeName(filter.CustomerType).ToLowerInvariant()}.xlsx",
            columns,
            rows.Select(customer => ToExportRow(customer, columns)));
    }

    public Task<MasterDataFileDto> GetCustomerTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(CreateWorkbook("customers-template.xlsx", ImportColumns.Where(x => x != "id").ToArray(), []));

    public async Task<LaravelApiResponse> UploadCustomersAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await ImportRowsAsync(fileStream, async row =>
        {
            var request = new CustomerRequestDto
            {
                CustomerType = row.CustomerType("customer_type"),
                Name = row.Value("name"),
                Mobile = row.Value("mobile"),
                Email = row.Value("email"),
                CustomerCode = row.Value("customer_code"),
                ContactNumber = row.Value("contact_number"),
                Active = row.Value("active"),
                CustomFields = ReadCustomFields(row)
            };

            NormalizeRequest(request);
            if (row.ULong("id") is { } id)
            {
                await UpdateCustomerAsync(id, request, actorUserId, cancellationToken);
                return true;
            }

            await CreateCustomerAsync(request, actorUserId, cancellationToken);
            return false;
        }, cancellationToken);

        return LaravelApiResponse.Success("import", result, "Customer import completed");
    }

    private async Task ValidateAsync(CustomerRequestDto request, ulong? id, CancellationToken cancellationToken)
    {
        RequireId(request.CustomerType, "Customer type is required.");
        RequireValue(request.Name, "Customer name is required.");

        if (!string.IsNullOrWhiteSpace(request.Mobile) && await _repository.MobileExistsAsync(request.Mobile.Trim(), id, cancellationToken))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Mobile already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && await _repository.EmailExistsAsync(request.Email.Trim(), id, cancellationToken))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Email already exists.");
        }
    }

    private static void NormalizeRequest(CustomerRequestDto request)
    {
        request.CustomerType ??= ReadULong(request.CustomFields, "customer_type");
        request.Name = FirstNonBlank(
            request.Name,
            ReadField(request.CustomFields, "legal_name"),
            ReadField(request.CustomFields, "shop_name"),
            ReadField(request.CustomFields, "owner_name"));
        request.Mobile = FirstNonBlank(request.Mobile, request.MobileNumber, ReadField(request.CustomFields, "mobile_number"));
        request.ContactNumber = FirstNonBlank(request.ContactNumber, request.WhatsappNumber, request.AlternateMobile, ReadField(request.CustomFields, "whatsapp_number"));

        request.CustomFields ??= [];
        SetField(request.CustomFields, "customer_type", request.CustomerType?.ToString());
        SetField(request.CustomFields, "name", request.Name);
        SetField(request.CustomFields, "mobile", request.Mobile);
        SetField(request.CustomFields, "mobile_number", request.Mobile);
        SetField(request.CustomFields, "contact_number", request.ContactNumber);
        SetField(request.CustomFields, "email", request.Email);
        SetField(request.CustomFields, "customer_code", request.CustomerCode);
        SetField(request.CustomFields, "profile_image", request.ProfileImage);
        SetField(request.CustomFields, "shop_image", request.ShopImage);
    }

    private static Dictionary<string, string?> ReadCustomFields(ExcelRow row)
    {
        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in ImportColumns.Where(x => x != "id" && x != "customer_type" && x != "name" && x != "mobile" && x != "email" && x != "customer_code" && x != "contact_number" && x != "active"))
        {
            SetField(fields, column, row.Value(column));
        }

        return fields;
    }

    private static object?[] ToExportRow(CustomerDto customer, string[] columns) =>
        columns.Select(column => ExportValue(customer, column)).ToArray();

    private static object? ExportValue(CustomerDto customer, string column)
    {
        object? value = column switch
        {
            "id" => customer.Id,
            "customer_type" => CustomerTypeName(customer.CustomerType),
            "name" => customer.Name,
            "mobile" => customer.Mobile,
            "email" => customer.Email,
            "customer_code" => customer.CustomerCode,
            "contact_number" => customer.ContactNumber,
            "active" => customer.Active.Equals("Y", StringComparison.OrdinalIgnoreCase) ? "Active" : "Inactive",
            "profile_image" => customer.ProfileImage ?? Field(customer, column),
            "shop_image" => customer.ShopImage ?? Field(customer, column),
            _ => Field(customer, column)
        };

        return value is string text && !PreserveRawColumns.Contains(column) ? TitleCase(text) : value;
    }

    private static string[] ExportColumnsFor(ulong? customerType) =>
        customerType == 1 ? DistributorExportColumns : RetailerExportColumns;

    private static string CustomerTypeName(ulong? type) => type switch
    {
        1 => "Distributor",
        2 => "Retailer",
        null => "All",
        _ => $"Type-{type}"
    };

    private static string? Field(CustomerDto customer, string key) =>
        customer.CustomFields.TryGetValue(key, out var value) ? value : null;

    private static string NormalizeKycDocumentKey(string documentKey)
    {
        var key = NormalizeText(documentKey)?.ToLowerInvariant();
        if (key is null || !KycDocumentKeys.Contains(key))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Invalid KYC document.");
        }

        return key;
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static void SetField(IDictionary<string, string?> fields, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) fields[key] = value.Trim();
    }

    private static string? ReadField(IReadOnlyDictionary<string, string?>? fields, string key) =>
        fields is not null && fields.TryGetValue(key, out var value) ? value : null;

    private static ulong? ReadULong(IReadOnlyDictionary<string, string?>? fields, string key) =>
        ulong.TryParse(ReadField(fields, key), out var parsed) ? parsed : null;

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
                worksheet.Cell(rowNumber, column + 1).Value = XLCellValue.FromObject(row[column]);
            }

            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new MasterDataFileDto { FileName = fileName, Content = stream.ToArray() };
    }

    private static async Task<MasterDataImportResultDto> ImportRowsAsync(Stream fileStream, Func<ExcelRow, Task<bool>> importRow, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed() ?? throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Import file is empty.");
        var headings = headerRow.CellsUsed().ToDictionary(cell => NormalizeHeading(cell.GetString()), cell => cell.Address.ColumnNumber);
        var totalRows = 0;
        var importedRows = 0;
        var updatedRows = 0;
        var errors = new List<string>();

        foreach (var worksheetRow in worksheet.RowsUsed().Where(row => row.RowNumber() > headerRow.RowNumber()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (worksheetRow.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetString()))) continue;

            totalRows++;
            try
            {
                if (await importRow(new ExcelRow(worksheetRow, headings))) updatedRows++;
                else importedRows++;
            }
            catch (Exception exception) when (exception is LaravelHttpException or FormatException or InvalidOperationException)
            {
                errors.Add($"Row {worksheetRow.RowNumber()}: {exception.Message}");
            }
        }

        return new MasterDataImportResultDto { TotalRows = totalRows, ImportedRows = importedRows, UpdatedRows = updatedRows, FailedRows = errors.Count, Errors = errors };
    }

    private static string NormalizeHeading(string heading) => heading.Trim().ToLowerInvariant().Replace(" ", "_");

    private static string TitleCaseHeading(string heading) => TitleCase(heading.Replace("_", " "));

    private static string TitleCase(string value)
    {
        var text = NormalizeText(value);
        return text is null ? string.Empty : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
    }

    private static async Task<T> GetOrThrowAsync<T>(Task<T?> task, string message)
    {
        var value = await task;
        return value ?? throw NotFound(message);
    }

    private static void RequireValue(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, message);
    }

    private static void RequireId(ulong? value, string message)
    {
        if (value is null or 0) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, message);
    }

    private static LaravelHttpException NotFound(string message) => new(LaravelStatusCodes.NotFound, message);

    private sealed class ExcelRow
    {
        private readonly IXLRow _row;
        private readonly IReadOnlyDictionary<string, int> _headings;

        public ExcelRow(IXLRow row, IReadOnlyDictionary<string, int> headings)
        {
            _row = row;
            _headings = headings;
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

        public ulong? CustomerType(string heading)
        {
            var value = Value(heading);
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (ulong.TryParse(value, out var parsed)) return parsed;

            return value.Trim().ToLowerInvariant() switch
            {
                "distributor" => 1,
                "retailer" => 2,
                _ => throw new FormatException($"{heading} must be Distributor, Retailer, 1, or 2.")
            };
        }
    }

    private static string? NormalizeText(string? value)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
