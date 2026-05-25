using Application.Common;
using Application.DTOs.Auth;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;
using System.Net.Mail;
using System.Text.Json;

namespace Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(IAuthRepository repository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LaravelApiResponse> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new LaravelHttpException(LaravelStatusCodes.NoContentLikeValidation, new Dictionary<string, string[]>
            {
                ["username"] = string.IsNullOrWhiteSpace(request.Username) ? ["The username field is required."] : [],
                ["password"] = string.IsNullOrWhiteSpace(request.Password) ? ["The password field is required."] : []
            });
        }

        var username = request.Username.Trim();
        var user = await _repository.FindUserByUsernameAsync(username, cancellationToken);
        if (user is not null)
        {
            return await HandleUserLoginAsync(user, request, cancellationToken);
        }

        var customer = await _repository.FindCustomerByUsernameAsync(username, cancellationToken);
        if (customer is not null)
        {
            return await HandleCustomerLoginAsync(customer, request, cancellationToken);
        }

        throw new LaravelHttpException(LaravelStatusCodes.NotFound, "Invalid credentials or account not found");
    }

    public async Task<LaravelApiResponse> SignupAsync(SignupRequestDto request, CancellationToken cancellationToken)
    {
        var firstError = await ValidateSignupAsync(request, cancellationToken);
        if (firstError is not null)
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, firstError);
        }

        var branches = ReadUlongList(request.BranchId);
        var branchShows = ReadUlongList(request.BranchShow);
        var roles = ReadUlongList(request.Roles);
        var cities = ReadUlongList(request.Cities);
        var latitude = request.Latitude;
        var longitude = request.Longitude;

        if (!string.IsNullOrWhiteSpace(request.BaseLocationCoordinates))
        {
            var parts = request.BaseLocationCoordinates.Split(',', 2, StringSplitOptions.TrimEntries);
            latitude = parts.Length == 2 && decimal.TryParse(parts[0], out _) ? parts[0] : null;
            longitude = parts.Length == 2 && decimal.TryParse(parts[1], out _) ? parts[1] : null;
        }

        var firstName = request.FirstName!.Trim();
        var lastName = request.LastName!.Trim();
        var user = new User
        {
            Active = string.IsNullOrWhiteSpace(request.Active) ? "Y" : request.Active.Trim(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"{firstName} {lastName}" : request.Name.Trim(),
            FirstName = firstName,
            LastName = lastName,
            Mobile = request.Mobile!.Trim(),
            Email = request.Email!.Trim(),
            Password = string.IsNullOrWhiteSpace(request.Password) ? string.Empty : _passwordHasher.Hash(request.Password),
            NotificationId = request.NotificationId ?? string.Empty,
            DeviceType = request.DeviceType ?? string.Empty,
            Gender = request.Gender ?? string.Empty,
            ProfileImage = request.ProfileImage ?? string.Empty,
            Latitude = latitude ?? string.Empty,
            Longitude = longitude ?? string.Empty,
            Location = request.Location ?? string.Empty,
            BranchId = branches.Count > 0 ? string.Join(',', branches) : null,
            PrimaryBranchId = request.PrimaryBranchId,
            BranchShow = branchShows.Count > 0 ? string.Join(',', branchShows) : null,
            DepartmentId = request.DepartmentId,
            EmployeeCodes = request.EmployeeCodes,
            DesignationId = request.DesignationId,
            DivisionId = request.DivisionId,
            ReportingId = request.ReportingId,
            ShowAttandanceReport = ReadString(request.ShowAttandanceReport) ?? "1",
            Payroll = request.Payroll,
            WarehouseId = request.WarehouseId,
            CustomerId = request.CustomerId,
            LeaveBalance = request.LeaveBalance ?? 0.00m,
            EarnedLeaveBalance = request.EarnedLeaveBalance ?? 0.00m,
            CasualLeaveBalance = request.CasualLeaveBalance ?? 0.00m,
            SickLeaveBalance = request.SickLeaveBalance ?? 0.00m,
            DateOfJoining = request.DateOfJoining,
            Grade = request.Grade,
            BloodGroup = request.BloodGroup,
            PersonalNumber = request.PersonalNumber,
            SalesType = request.SalesType ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user = await _repository.AddUserAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        if (roles.Count > 0)
        {
            await _repository.SyncUserRolesAndRolePermissionsAsync(user.Id, roles, cancellationToken);
        }

        await _repository.AddUserDetailsAsync(new UserDetails
        {
            UserId = user.Id,
            MaritalStatus = request.MaritalStatus,
            DateOfBirth = request.DateOfBirth,
            PanNumber = request.PanNumber,
            AadharNumber = request.AadharNumber,
            EmergencyNumber = request.EmergencyNumber,
            CurrentAddress = request.CurrentAddress,
            PermanentAddress = request.PermanentAddress,
            FatherName = request.FatherName,
            FatherDateOfBirth = request.FatherDateOfBirth,
            MotherName = request.MotherName,
            MotherDateOfBirth = request.MotherDateOfBirth,
            MarriageAnniversary = request.MarriageAnniversary,
            SpouseName = request.SpouseName,
            SpouseDateOfBirth = request.SpouseDateOfBirth,
            ChildrenOne = request.ChildrenOne,
            ChildrenOneDateOfBirth = request.ChildrenOneDateOfBirth,
            ChildrenTwo = request.ChildrenTwo,
            ChildrenTwoDateOfBirth = request.ChildrenTwoDateOfBirth,
            ChildrenThree = request.ChildrenThree,
            ChildrenThreeDateOfBirth = request.ChildrenThreeDateOfBirth,
            ChildrenFour = request.ChildrenFour,
            ChildrenFourDateOfBirth = request.ChildrenFourDateOfBirth,
            ChildrenFive = request.ChildrenFive,
            ChildrenFiveDateOfBirth = request.ChildrenFiveDateOfBirth,
            AccountNumber = request.AccountNumber,
            BankName = request.BankName,
            IfscCode = request.IfscCode,
            Salary = request.Salary ?? 0.00m,
            CtcAnnual = request.CtcAnnual ?? 0.00m,
            GrossSalaryMonthly = request.GrossSalaryMonthly ?? 0.00m,
            LastYearIncrements = request.LastYearIncrements ?? 0.00m,
            LastYearIncrementPercent = request.LastYearIncrementPercent,
            LastYearIncrementValue = request.LastYearIncrementValue ?? 0.00m,
            LastPromotion = request.LastPromotion,
            PfNumber = request.PfNumber,
            UnNumber = request.UnNumber,
            EsiNumber = request.EsiNumber,
            ProbationPeriod = request.ProbationPeriod,
            DateOfConfirmation = request.DateOfConfirmation,
            NoticePeriod = request.NoticePeriod,
            DateOfLeaving = request.DateOfLeaving,
            DateOfJoining = request.DateOfJoining,
            BiometricCode = request.BiometricCode,
            OrderMails = request.OrderMails ?? string.Empty,
            OrderMailsType = JoinJsonValues(request.OrderMailsType),
            OtherEducation = request.OtherEducation,
            PreviousExp = request.PreviousExp,
            CurrentCompanyTenture = request.CurrentCompanyTenture,
            TotalExp = request.TotalExp,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);

        if (cities.Count > 0)
        {
            await _repository.AddUserCityAssignsAsync(cities.Select(city => new UserCityAssign
            {
                UserId = user.Id,
                CityId = city,
                ReportingId = request.ReportingId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }), cancellationToken);
        }

        var education = ReadEducation(request.EducationDetail, user.Id);
        if (education.Count > 0)
        {
            await _repository.AddUserEducationAsync(education, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return LaravelApiResponse.Success("data", new SignupResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Mobile = user.Mobile,
            Email = user.Email,
            Active = user.Active
        }, "User created successfully");
    }

    public async Task<LaravelApiResponse> CustomerSignupAsync(CustomerSignupRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ShopName) || string.IsNullOrWhiteSpace(request.Mobile))
        {
            throw new LaravelHttpException(LaravelStatusCodes.NoContentLikeValidation, "Validation failed");
        }

        var mobile = NormalizeIndianMobile(request.Mobile);
        var customer = new Customer
        {
            Active = "Y",
            Name = Capitalize(request.ShopName),
            FirstName = Capitalize(request.Name),
            LastName = string.Empty,
            Mobile = mobile,
            Email = request.Email?.Trim().ToLowerInvariant(),
            Password = string.IsNullOrWhiteSpace(request.Password) ? string.Empty : _passwordHasher.Hash(request.Password),
            CustomerType = request.CustomerType ?? 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        customer = await _repository.AddCustomerAsync(customer, cancellationToken);

        var token = _tokenService.CreateAccessToken("customers", customer.Id, customer.Name, [], out var tokenId);
        await _repository.StoreTokenAsync(new OAuthAccessToken
        {
            Id = tokenId,
            UserId = customer.Id,
            ClientId = 0,
            Name = "mobile-app-token",
            Scopes = "[]",
            Revoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        }, cancellationToken);

        await _repository.UpsertLoginDetailAsync(new MobileUserLoginDetail
        {
            CustomerId = customer.Id,
            AppVersion = request.AppVersion ?? "unknown",
            DeviceType = request.DeviceType ?? "unknown",
            DeviceName = request.DeviceName ?? "unknown",
            UniqueId = request.UniqueId,
            FirstLoginDate = DateTime.UtcNow,
            LastLoginDate = DateTime.UtcNow,
            LoginStatus = "1",
            App = "1"
        }, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return LaravelApiResponse.Success("userinfo", new
        {
            id = customer.Id,
            name = customer.Name,
            first_name = customer.FirstName,
            last_name = customer.LastName,
            email = customer.Email,
            mobile = customer.Mobile,
            token,
            access_token = token,
            total_point = 0,
            active_point = 0,
            provision_point = 0,
            profile_image = customer.ProfileImage,
            shop_image = customer.ShopImage
        }, "Account created successfully");
    }

    public async Task<LaravelApiResponse> LogoutAsync(string tokenId, string provider, ulong subjectId, CancellationToken cancellationToken)
    {
        await _repository.RevokeTokenAsync(tokenId, cancellationToken);
        await _repository.UpsertLoginDetailAsync(new MobileUserLoginDetail
        {
            UserId = provider == "users" ? subjectId : null,
            CustomerId = provider == "customers" ? subjectId : null,
            LoginStatus = "0"
        }, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Logout Successfully");
    }

    private async Task<LaravelApiResponse> HandleUserLoginAsync(User user, LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (user.Active != "Y")
        {
            throw new LaravelHttpException(LaravelStatusCodes.NotFound, "Account deactivated. Contact admin.");
        }

        if (!_passwordHasher.Verify(request.Password!, user.Password))
        {
            throw new LaravelHttpException(LaravelStatusCodes.Unauthorized, "Incorrect password");
        }

        var roles = await _repository.GetUserRolesAsync(user.Id, cancellationToken);
        var permissions = await _repository.GetUserPermissionsAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(role => role.Name).ToArray();
        var isSuperAdmin = roleNames.Any(role => string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase));
        var loginDetail = await _repository.GetUserLoginDetailAsync(user.Id, cancellationToken);

        if (loginDetail is not null && !isSuperAdmin && !string.IsNullOrWhiteSpace(loginDetail.UniqueId)
            && loginDetail.UniqueId != request.UniqueId && loginDetail.MultiLogin == "0")
        {
            throw new LaravelHttpException(LaravelStatusCodes.NoContentLikeValidation, "Multiple device login not allowed. Contact support: 9713113280.");
        }

        var token = _tokenService.CreateAccessToken("users", user.Id, user.Name, roleNames, out var tokenId);
        await _repository.StoreTokenAsync(new OAuthAccessToken
        {
            Id = tokenId,
            UserId = user.Id,
            ClientId = 0,
            Name = "mobile-app-token",
            Scopes = "[]",
            Revoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        }, cancellationToken);

        await _repository.UpsertLoginDetailAsync(new MobileUserLoginDetail
        {
            UserId = user.Id,
            AppVersion = request.AppVersion,
            DeviceName = request.DeviceName,
            DeviceType = request.DeviceType,
            UniqueId = request.UniqueId,
            LastLoginDate = DateTime.UtcNow,
            LoginStatus = "1",
            App = "2",
            LoginAt = request.LoginAt.HasValue ? DateTime.UtcNow : null
        }, cancellationToken);

        user.NotificationId = request.FcmToken ?? user.NotificationId;
        user.DeviceType = request.DeviceType ?? user.DeviceType;
        await _repository.SaveChangesAsync(cancellationToken);

        return LaravelApiResponse.Success("userinfo", new LoginUserInfoDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Mobile = user.Mobile,
            ProfileImage = user.ProfileImage,
            AccessToken = token,
            Roles = roles.Select(role => role.Id).ToArray(),
            Permissions = permissions.Select(permission => permission.Name).ToArray(),
            UserType = roleNames,
            LeaveBalance = user.LeaveBalance,
            Provider = roleNames.Contains("Customer Dealer") ? "retailers" : "users"
        });
    }

    private async Task<LaravelApiResponse> HandleCustomerLoginAsync(Customer customer, LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (customer.Active != "Y")
        {
            throw new LaravelHttpException(LaravelStatusCodes.NotFound, "Account deactivated. Contact admin.");
        }

        if (!_passwordHasher.Verify(request.Password!, customer.Password))
        {
            throw new LaravelHttpException(LaravelStatusCodes.Unauthorized, "Incorrect password");
        }

        var token = _tokenService.CreateAccessToken("customers", customer.Id, customer.Name, [], out var tokenId);
        await _repository.StoreTokenAsync(new OAuthAccessToken
        {
            Id = tokenId,
            UserId = customer.Id,
            ClientId = 0,
            Name = "mobile-app-token",
            Scopes = "[]",
            Revoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        }, cancellationToken);

        await _repository.UpsertLoginDetailAsync(new MobileUserLoginDetail
        {
            CustomerId = customer.Id,
            AppVersion = request.AppVersion,
            DeviceName = request.DeviceName,
            DeviceType = request.DeviceType,
            UniqueId = request.UniqueId,
            LastLoginDate = DateTime.UtcNow,
            LoginStatus = "1",
            App = "1"
        }, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return LaravelApiResponse.Success("userinfo", new
        {
            id = customer.Id,
            name = customer.Name,
            email = customer.Email,
            mobile = customer.Mobile,
            profile_image = customer.ProfileImage,
            shop_image = customer.ShopImage,
            access_token = token,
            provider = "customers"
        });
    }

    private async Task<string?> ValidateSignupAsync(SignupRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) && (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))) return "Name is required.";
        if (string.IsNullOrWhiteSpace(request.FirstName)) return "First name is required.";
        if (request.FirstName.Trim().Length < 2) return "First name must be at least 2 characters.";
        if (string.IsNullOrWhiteSpace(request.LastName)) return "Last name is required.";
        if (request.LastName.Trim().Length < 2) return "Last name must be at least 2 characters.";
        if (string.IsNullOrWhiteSpace(request.Mobile)) return "Mobile number is required.";
        if (request.Mobile.Length != 10 || !request.Mobile.All(char.IsDigit)) return "Mobile number must be 10 digits.";
        if (await _repository.UserMobileExistsAsync(request.Mobile, cancellationToken)) return "This mobile number is already registered.";
        if (string.IsNullOrWhiteSpace(request.Email)) return "Email address is required.";
        if (request.Email.Trim().Length < 7 || request.Email.Trim().Length > 200 || !IsValidEmail(request.Email)) return "Please enter a valid email address.";
        if (await _repository.UserEmailExistsAsync(request.Email, cancellationToken)) return "This email address is already registered.";
        if (!string.IsNullOrEmpty(request.Password) && request.Password.Length < 6) return "Password must be at least 6 characters.";
        return null;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IReadOnlyList<ulong> ReadUlongList(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return [];
        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray().Select(ReadUlong).Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        }

        var single = ReadUlong(value);
        return single.HasValue ? [single.Value] : [];
    }

    private static ulong? ReadUlong(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String && ulong.TryParse(value.GetString(), out number)) return number;
        return null;
    }

    private static string JoinJsonValues(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return string.Empty;
        if (value.ValueKind == JsonValueKind.Array)
        {
            return string.Join(',', value.EnumerateArray().Select(ReadString).Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        return ReadString(value) ?? string.Empty;
    }

    private static string? ReadString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "1",
            JsonValueKind.False => "0",
            _ => null
        };
    }

    private static IReadOnlyList<UserEducation> ReadEducation(JsonElement value, ulong userId)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return [];

        var rows = value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
            : value.ValueKind == JsonValueKind.Object
                ? value.EnumerateObject().Select(x => x.Value)
                : [];

        return rows
            .Where(x => x.ValueKind == JsonValueKind.Object)
            .Select(row => new UserEducation
            {
                UserId = userId,
                EducationTypeId = ReadObjectUlong(row, "education_type_id"),
                DegreeName = ReadObjectString(row, "degree_name"),
                BoardName = ReadObjectString(row, "board_name"),
                Percentage = ReadObjectString(row, "percentage"),
                Grade = ReadObjectString(row, "grade"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.DegreeName))
            .ToArray();
    }

    private static string? ReadObjectString(JsonElement row, string propertyName)
    {
        return row.TryGetProperty(propertyName, out var value) ? ReadString(value) : null;
    }

    private static ulong? ReadObjectUlong(JsonElement row, string propertyName)
    {
        return row.TryGetProperty(propertyName, out var value) ? ReadUlong(value) : null;
    }

    private static string NormalizeIndianMobile(string mobile)
    {
        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        return digits.Length == 10 ? $"91{digits}" : digits;
    }

    private static string Capitalize(string value)
    {
        value = value.Trim();
        return value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
    }
}
