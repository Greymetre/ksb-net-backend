using Application.Common;
using Application.DTOs.MasterData;
using Application.DTOs.Users;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;
using System.Globalization;

namespace Application.Services;

public sealed class UserService : IUserService
{
    private static readonly string[] ExportHeadings =
    [
        "ID", "Employees Code", "User Name", "Designation", "Role", "Zone Name", "Location", "Department", "Division",
        "Reporting To", "Mobile", "Email", "Date Of Joining", "Date Of Birth", "Date of Confirmation", "Date of leaving",
        "Grade", "Designation Code", "Employee Super Code", "Base Location Coordinates (latitude, longitude)",
        "Reporting ID", "Role Ids", "payroll", "designation_id", "branch_id", "division_id", "department_id",
        "Attandance Summary Report"
    ];

    private static readonly string[] TemplateHeadings =
    [
        "id", "employees_code", "user_name", "designation", "role", "zone_name", "base_location", "department",
        "division", "reporting_to", "mobile", "password", "email", "date_of_joining", "date_of_birth",
        "date_of_confirmation", "date_of_leaving", "grade", "designation_code", "employee_super_code",
        "base_location_coordinates_latitude_longitude", "reporting_id", "role_ids", "payroll", "designation_id",
        "branch_id", "division_id", "department_id", "attandance_summary_report"
    ];

    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository repository, IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<LaravelApiResponse> GetUsersAsync(UserListFiltersDto filters, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("users", await _repository.GetUsersAsync(filters, cancellationToken));

    public async Task<LaravelApiResponse> GetUserAsync(ulong id, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserDtoAsync(id, cancellationToken);
        return LaravelApiResponse.Success("user", user ?? throw NotFound("User not found"));
    }

    public async Task<LaravelApiResponse> GetUserOptionsAsync(CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("options", await _repository.GetUserOptionsAsync(cancellationToken));

    public async Task<LaravelApiResponse> CreateUserAsync(UserRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        await ValidateUserAsync(request, null, cancellationToken);

        var (firstName, lastName, name) = ResolveName(request);
        var (latitude, longitude) = SplitCoordinates(request.BaseLocationCoordinates);
        var user = new User
        {
            Active = NormalizeActive(request.Active),
            Name = name,
            FirstName = firstName,
            LastName = lastName,
            EmployeeCodes = request.EmployeeCodes,
            Mobile = request.Mobile?.Trim(),
            Email = request.Email?.Trim(),
            Password = string.IsNullOrWhiteSpace(request.Password) ? string.Empty : _passwordHasher.Hash(request.Password),
            PasswordString = request.Password,
            NotificationId = string.Empty,
            DeviceType = string.Empty,
            Gender = string.Empty,
            ProfileImage = string.Empty,
            Latitude = latitude ?? string.Empty,
            Longitude = longitude ?? string.Empty,
            UserCode = string.Empty,
            Location = request.Location ?? string.Empty,
            BranchId = NormalizeBranchIds(request),
            DesignationId = request.DesignationId,
            DivisionId = request.DivisionId,
            DepartmentId = request.DepartmentId,
            ReportingId = request.ReportingId,
            Payroll = request.Payroll,
            WarehouseId = request.WarehouseId,
            SalesType = request.SalesType ?? string.Empty,
            ShowAttandanceReport = string.IsNullOrWhiteSpace(request.ShowAttandanceReport) ? "1" : request.ShowAttandanceReport,
            DateOfJoining = request.DateOfJoining,
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddUserAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        if (request.Roles is not null)
        {
            await _repository.SyncUserRolesAsync(user.Id, request.Roles, cancellationToken);
        }

        await _repository.AddUserDetailsAsync(new UserDetails
        {
            UserId = user.Id,
            DateOfJoining = request.DateOfJoining,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("user", await _repository.GetUserDtoAsync(user.Id, cancellationToken), "User created successfully");
    }

    public async Task<LaravelApiResponse> UpdateUserAsync(ulong id, UserRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserAsync(id, cancellationToken) ?? throw NotFound("User not found");
        await ValidateUserAsync(request, id, cancellationToken);

        var (firstName, lastName, name) = ResolveName(request, user);
        var (latitude, longitude) = SplitCoordinates(request.BaseLocationCoordinates);

        user.Active = NormalizeActive(request.Active);
        user.Name = name;
        user.FirstName = firstName;
        user.LastName = lastName;
        user.EmployeeCodes = request.EmployeeCodes;
        user.Mobile = request.Mobile?.Trim();
        user.Email = request.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.Password = _passwordHasher.Hash(request.Password);
            user.PasswordString = request.Password;
        }
        user.Latitude = latitude ?? user.Latitude;
        user.Longitude = longitude ?? user.Longitude;
        user.Location = request.Location ?? string.Empty;
        user.BranchId = NormalizeBranchIds(request);
        user.DesignationId = request.DesignationId;
        user.DivisionId = request.DivisionId;
        user.DepartmentId = request.DepartmentId;
        user.ReportingId = request.ReportingId;
        user.Payroll = request.Payroll;
        user.WarehouseId = request.WarehouseId;
        user.SalesType = request.SalesType ?? string.Empty;
        user.ShowAttandanceReport = string.IsNullOrWhiteSpace(request.ShowAttandanceReport) ? "1" : request.ShowAttandanceReport;
        user.DateOfJoining = request.DateOfJoining;
        user.UpdatedAt = DateTime.UtcNow;

        if (request.Roles is not null)
        {
            await _repository.SyncUserRolesAsync(user.Id, request.Roles, cancellationToken);
        }

        var details = await _repository.GetUserDetailsAsync(user.Id, cancellationToken);
        if (details is null)
        {
            details = new UserDetails { UserId = user.Id, CreatedAt = DateTime.UtcNow };
            await _repository.AddUserDetailsAsync(details, cancellationToken);
        }
        details.DateOfJoining = request.DateOfJoining;
        details.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("user", await _repository.GetUserDtoAsync(user.Id, cancellationToken), "User updated successfully");
    }

    public async Task<LaravelApiResponse> SetUserActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserAsync(id, cancellationToken) ?? throw NotFound("User not found");
        user.Active = NormalizeActive(active);
        user.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.Success("user", await _repository.GetUserDtoAsync(id, cancellationToken), "User status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteUserAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteUserAsync(id, actorUserId, cancellationToken)) throw NotFound("User not found");
        return LaravelApiResponse.MessageOnly("success", "User deleted successfully!");
    }

    public async Task<MasterDataFileDto> ExportUsersAsync(UserExportFiltersDto filters, CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportUsersAsync(filters, cancellationToken);
        return CreateWorkbook("users.xlsx", ExportHeadings, rows.Select(x => new object?[]
        {
            x.Id,
            x.EmployeeCodes,
            x.Name,
            x.DesignationName,
            x.RoleNames,
            x.BranchNames,
            x.Location,
            x.DepartmentName,
            x.DivisionName,
            x.ReportingName,
            x.Mobile,
            x.Email,
            FormatDate(x.DateOfJoining),
            FormatDate(x.DateOfBirth),
            FormatDate(x.DateOfConfirmation),
            FormatDate(x.DateOfLeaving),
            x.Grade,
            x.DesignationCode,
            x.EmployeeSuperCode,
            x.BaseLocationCoordinates,
            x.ReportingId,
            x.RoleIds,
            x.Payroll,
            x.DesignationId,
            x.BranchId,
            x.DivisionId,
            x.DepartmentId,
            x.AttendanceSummaryReport
        }));
    }

    public Task<MasterDataFileDto> GetUserTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(CreateWorkbook("users.xlsx", TemplateHeadings, []));

    public async Task<LaravelApiResponse> UploadUsersAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await ImportRowsAsync(fileStream, row => ImportUserRowAsync(row, actorUserId, cancellationToken), cancellationToken);
        return LaravelApiResponse.Success("import", result, "User import completed");
    }

    private async Task<bool> ImportUserRowAsync(ExcelRow row, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var name = row.Value("user_name");
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "User name is required.");
        }

        var (firstName, lastName) = SplitName(name);
        var (latitude, longitude) = SplitCoordinates(row.Value("base_location_coordinates_latitude_longitude"));
        var roleIds = await ResolveRoleIdsAsync(row, cancellationToken);
        var id = row.ULong("id");
        var user = id.HasValue ? await _repository.GetUserAsync(id.Value, cancellationToken) : null;
        var updated = user is not null;

        if (user is null)
        {
            user = new User
            {
                Active = "Y",
                CreatedBy = actorUserId,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.AddUserAsync(user, cancellationToken);
        }

        user.Name = CapitalizeWords(name);
        user.FirstName = CapitalizeWords(firstName);
        user.LastName = CapitalizeWords(lastName);
        if (!updated || !string.IsNullOrWhiteSpace(row.Value("mobile"))) user.Mobile = row.Value("mobile");
        user.Email = row.Value("email");
        if (!string.IsNullOrWhiteSpace(row.Value("password"))) user.Password = _passwordHasher.Hash(row.Value("password")!);
        user.Gender = row.Value("gender") ?? user.Gender;
        user.ProfileImage = row.Value("profile_image") ?? user.ProfileImage;
        user.UserCode = row.Value("user_code") ?? user.UserCode;
        user.Location = row.Value("base_location") ?? row.Value("location") ?? user.Location;
        user.EmployeeCodes = row.Value("employees_code");
        user.BranchId = row.Value("branch_id");
        user.PrimaryBranchId = row.ULong("primary_branch_id");
        user.DesignationId = row.ULong("designation_id");
        user.DivisionId = row.ULong("division_id");
        user.DepartmentId = row.ULong("department_id");
        user.WarehouseId = row.ULong("warehouse_id");
        user.ReportingId = row.ULong("reporting_id");
        user.Payroll = row.Value("payroll");
        user.SalesType = row.Value("sales_type") ?? user.SalesType;
        user.ShowAttandanceReport = NormalizeAttendance(row.Value("attandance_summary_report")) ?? user.ShowAttandanceReport;
        user.Grade = row.Value("grade");
        user.BloodGroup = row.Value("designation_code") ?? row.Value("blood_group");
        user.PersonalNumber = row.Value("employee_super_code") ?? row.Value("personal_number");
        user.Latitude = latitude ?? user.Latitude;
        user.Longitude = longitude ?? user.Longitude;
        user.DateOfJoining = row.Date("date_of_joining") ?? user.DateOfJoining;
        user.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

        if (roleIds.Count > 0)
        {
            await _repository.SyncUserRolesAsync(user.Id, roleIds, cancellationToken);
        }

        var details = await _repository.GetUserDetailsAsync(user.Id, cancellationToken);
        if (details is null)
        {
            details = new UserDetails
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.AddUserDetailsAsync(details, cancellationToken);
        }

        details.DateOfJoining = row.Date("date_of_joining") ?? details.DateOfJoining;
        details.DateOfBirth = row.Date("date_of_birth") ?? details.DateOfBirth;
        details.DateOfConfirmation = row.Date("date_of_confirmation") ?? details.DateOfConfirmation;
        details.DateOfLeaving = row.Date("date_of_leaving") ?? details.DateOfLeaving;
        details.LastYearIncrements = row.Decimal("last_year_increments") ?? details.LastYearIncrements;
        details.LastYearIncrementPercent = NormalizePercent(row.Value("last_year_increment_percent")) ?? details.LastYearIncrementPercent;
        details.LastPromotion = row.Value("last_promotion") ?? details.LastPromotion;
        details.CtcAnnual = row.Decimal("ctc_annual") ?? details.CtcAnnual;
        details.GrossSalaryMonthly = row.Decimal("gross_salary_monthly") ?? details.GrossSalaryMonthly;
        details.Salary = row.Decimal("ctc_per_month") ?? details.Salary;
        details.LastYearIncrementValue = row.Decimal("last_year_increments_value") ?? details.LastYearIncrementValue;
        details.MaritalStatus = row.Value("marital_status") ?? details.MaritalStatus;
        details.FatherName = row.Value("father_name") ?? details.FatherName;
        details.FatherDateOfBirth = row.Date("father_date_of_birth") ?? details.FatherDateOfBirth;
        details.MotherName = row.Value("mother_name") ?? details.MotherName;
        details.MotherDateOfBirth = row.Date("mother_dob") ?? details.MotherDateOfBirth;
        details.MarriageAnniversary = row.Date("marriage_anniversary") ?? details.MarriageAnniversary;
        details.SpouseName = row.Value("spouse_name") ?? details.SpouseName;
        details.SpouseDateOfBirth = row.Date("spouse_date_of_birth") ?? details.SpouseDateOfBirth;
        details.PanNumber = row.Value("pan_number") ?? details.PanNumber;
        details.AadharNumber = row.Value("adhar_number") ?? row.Value("aadhar_number") ?? details.AadharNumber;
        details.EmergencyNumber = row.Value("emergency_number") ?? details.EmergencyNumber;
        details.CurrentAddress = row.Value("current_address") ?? details.CurrentAddress;
        details.PermanentAddress = row.Value("permanent_address") ?? details.PermanentAddress;
        details.BiometricCode = row.Value("biometric_code") ?? details.BiometricCode;
        details.AccountNumber = row.Value("account_number") ?? details.AccountNumber;
        details.BankName = row.Value("bank_name") ?? details.BankName;
        details.IfscCode = row.Value("ifsc_code") ?? details.IfscCode;
        details.PfNumber = row.Value("pf_number") ?? details.PfNumber;
        details.UnNumber = row.Value("un_number") ?? details.UnNumber;
        details.EsiNumber = row.Value("esi_number") ?? details.EsiNumber;
        details.ProbationPeriod = FormatDate(row.Date("probation_period")) ?? details.ProbationPeriod;
        details.NoticePeriod = row.Value("notice_period") ?? details.NoticePeriod;
        details.CurrentCompanyTenture = row.Value("current_company_tenure") ?? details.CurrentCompanyTenture;
        details.PreviousExp = row.Value("previous_exp") ?? details.PreviousExp;
        details.TotalExp = row.Value("total_exp") ?? details.TotalExp;
        details.OrderMails = row.Value("order_mails") ?? details.OrderMails;
        details.OrderMailsType = row.Value("order_mail_type_id") ?? details.OrderMailsType;
        details.UpdatedAt = DateTime.UtcNow;

        await UpsertEducationAsync(user.Id, 0, row.Value("high_school"), cancellationToken);
        await UpsertEducationAsync(user.Id, 1, row.Value("higher_secondary"), cancellationToken);
        await UpsertEducationAsync(user.Id, 2, row.Value("graducation"), cancellationToken);
        await UpsertEducationAsync(user.Id, 3, row.Value("post_graducation"), cancellationToken);
        await UpsertEducationAsync(user.Id, 4, row.Value("other"), cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
        return updated;
    }

    private async Task<IReadOnlyCollection<ulong>> ResolveRoleIdsAsync(ExcelRow row, CancellationToken cancellationToken)
    {
        var ids = ReadUlongList(row.Value("role_ids"));
        if (ids.Count > 0)
        {
            return ids;
        }

        var roleName = row.Value("role");
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return [];
        }

        var role = await _repository.GetRoleByNameAsync(roleName.Trim(), cancellationToken);
        return role is null ? [] : [role.Id];
    }

    private async Task UpsertEducationAsync(ulong userId, ulong educationTypeId, string? degreeName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(degreeName))
        {
            return;
        }

        await _repository.AddUserEducationAsync(new UserEducation
        {
            UserId = userId,
            EducationTypeId = educationTypeId,
            DegreeName = degreeName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private static MasterDataFileDto CreateWorkbook(string fileName, string[] headings, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");
        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 9;

        for (var column = 0; column < headings.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = FormatHeading(headings[column]);
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
        return new MasterDataFileDto
        {
            FileName = fileName,
            Content = stream.ToArray()
        };
    }

    private static async Task<UserImportResultDto> ImportRowsAsync(Stream fileStream, Func<ExcelRow, Task<bool>> importRow, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed() ?? throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Import file is empty.");
        var headings = headerRow.CellsUsed()
            .ToDictionary(cell => NormalizeHeading(cell.GetString()), cell => cell.Address.ColumnNumber);

        var totalRows = 0;
        var importedRows = 0;
        var updatedRows = 0;
        var errors = new List<string>();

        foreach (var worksheetRow in worksheet.RowsUsed().Where(row => row.RowNumber() > headerRow.RowNumber()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (worksheetRow.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetString())))
            {
                continue;
            }

            totalRows++;
            try
            {
                var updated = await importRow(new ExcelRow(worksheetRow, headings));
                if (updated) updatedRows++;
                else importedRows++;
            }
            catch (Exception exception) when (exception is LaravelHttpException or FormatException or InvalidOperationException)
            {
                errors.Add($"Row {worksheetRow.RowNumber()}: {exception.Message}");
            }
        }

        return new UserImportResultDto
        {
            TotalRows = totalRows,
            ImportedRows = importedRows,
            UpdatedRows = updatedRows,
            FailedRows = errors.Count,
            Errors = errors
        };
    }

    private static string NormalizeHeading(string heading) =>
        heading.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_").Replace("(", "").Replace(")", "").Replace(",", "");

    private static string FormatHeading(string heading) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(heading.Replace("_", " ").Trim().ToLowerInvariant());

    private static object? FormatExportValue(string heading, object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return value;
        var normalized = NormalizeHeading(heading);
        if (normalized is "id" or "employees_code" or "mobile" or "email" or "date_of_joining" or "date_of_birth" or "date_of_confirmation"
            or "date_of_leaving" or "designation_code" or "employee_super_code" or "base_location_coordinates_latitude_longitude"
            or "reporting_id" or "role_ids" or "designation_id" or "branch_id" or "division_id" or "department_id")
        {
            return value;
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.Trim().ToLowerInvariant());
    }

    private static (string FirstName, string LastName) SplitName(string name)
    {
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return (string.Empty, string.Empty);
        if (parts.Length == 1) return (parts[0], string.Empty);
        return (string.Join(' ', parts[..^1]), parts[^1]);
    }

    private static (string? Latitude, string? Longitude) SplitCoordinates(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (null, null);
        var parts = value.Split(',', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 && decimal.TryParse(parts[0], out _) && decimal.TryParse(parts[1], out _)
            ? (parts[0], parts[1])
            : (null, null);
    }

    private static string? NormalizeAttendance(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Equals("yes", StringComparison.OrdinalIgnoreCase)) return "1";
        if (normalized.Equals("no", StringComparison.OrdinalIgnoreCase)) return "0";
        return normalized;
    }

    private static string? NormalizePercent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.EndsWith('%') ? normalized : $"{normalized}%";
    }

    private static IReadOnlyCollection<ulong> ReadUlongList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => ulong.TryParse(part, out var parsed) ? parsed : (ulong?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToArray();
    }

    private static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string CapitalizeWords(string value)
    {
        value = value.Trim().ToLowerInvariant();
        return value.Length == 0 ? value : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
    }

    private async Task ValidateUserAsync(UserRequestDto request, ulong? currentUserId, CancellationToken cancellationToken)
    {
        var (_, _, name) = ResolveName(request);
        if (string.IsNullOrWhiteSpace(name)) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Name is required.");
        if (string.IsNullOrWhiteSpace(request.Mobile)) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Mobile number is required.");
        if (request.Mobile.Trim().Length is < 10 or > 11 || !request.Mobile.Trim().All(char.IsDigit)) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Mobile number must be numeric.");
        if (await _repository.UserMobileExistsAsync(request.Mobile.Trim(), currentUserId, cancellationToken)) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "This mobile number is already registered.");
        if (string.IsNullOrWhiteSpace(request.Email)) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Email address is required.");
        if (await _repository.UserEmailExistsAsync(request.Email.Trim(), currentUserId, cancellationToken)) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "This email address is already registered.");
        if (!currentUserId.HasValue && string.IsNullOrWhiteSpace(request.Password)) throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Password is required.");
    }

    private static (string FirstName, string LastName, string Name) ResolveName(UserRequestDto request, User? current = null)
    {
        var firstName = request.FirstName?.Trim() ?? current?.FirstName ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? current?.LastName ?? string.Empty;
        var name = string.IsNullOrWhiteSpace(request.Name)
            ? $"{firstName} {lastName}".Trim()
            : request.Name.Trim();

        if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(name))
        {
            var split = SplitName(name);
            firstName = split.FirstName;
            lastName = split.LastName;
        }

        return (CapitalizeWords(firstName), CapitalizeWords(lastName), CapitalizeWords(name));
    }

    private static string NormalizeActive(string? active) =>
        string.Equals(active, "N", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";

    private static string? NormalizeBranchIds(UserRequestDto request)
    {
        if (request.BranchIds?.Count > 0)
        {
            return string.Join(',', request.BranchIds.Distinct());
        }

        return request.BranchId;
    }

    private static LaravelHttpException NotFound(string message) =>
        new(LaravelStatusCodes.NotFound, message);

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
            return ulong.TryParse(value, out var parsed)
                ? parsed
                : throw new FormatException($"{heading} must be numeric.");
        }

        public decimal? Decimal(string heading)
        {
            var value = Value(heading);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return decimal.TryParse(value.Replace("%", string.Empty), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : throw new FormatException($"{heading} must be numeric.");
        }

        public DateTime? Date(string heading)
        {
            if (!_headings.TryGetValue(NormalizeHeading(heading), out var column)) return null;
            var cell = _row.Cell(column);
            if (cell.TryGetValue<DateTime>(out var date)) return date;

            var value = NormalizeText(cell.GetFormattedString());
            if (string.IsNullOrWhiteSpace(value)) return null;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
                ? parsed
                : throw new FormatException($"{heading} must be a valid date.");
        }
    }

    private static string? NormalizeText(string? value)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 || trimmed == "-" ? null : trimmed;
    }
}
