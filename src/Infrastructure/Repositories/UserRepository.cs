using Application.DTOs.Users;
using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private const string DistributorRoleName = "Distributor";
    private const int MaxRows = 1000;
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<UserDto>> GetUsersAsync(UserListFiltersDto filters, CancellationToken cancellationToken)
    {
        var query = ApplyUserFilters(_dbContext.Users.AsNoTracking(), new UserExportFiltersDto
        {
            UserType = filters.UserType,
            Active = filters.Active,
            DivisionId = filters.DivisionId,
            BranchId = filters.BranchId,
            DepartmentId = filters.DepartmentId
        });

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(x =>
                x.Name.Contains(search)
                || (x.Email != null && x.Email.Contains(search))
                || (x.Mobile != null && x.Mobile.Contains(search))
                || (x.EmployeeCodes != null && x.EmployeeCodes.Contains(search)));
        }

        var users = await ProjectUsersAsync(query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows), cancellationToken);

        return users;
    }

    public async Task<UserDto?> GetUserDtoAsync(ulong id, CancellationToken cancellationToken)
    {
        var users = await ProjectUsersAsync(_dbContext.Users.AsNoTracking().Where(x => x.Id == id), cancellationToken);
        return users.FirstOrDefault();
    }

    public async Task<UserOptionsDto> GetUserOptionsAsync(CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles.AsNoTracking()
            .Where(x => x.Name != "super-admin" && x.Name != DistributorRoleName)
            .OrderBy(x => x.Name)
            .Select(x => new OptionDto { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

        var branches = await _dbContext.Branches.AsNoTracking()
            .Where(x => x.Active == "Y")
            .OrderBy(x => x.BranchName)
            .Select(x => new OptionDto { Id = x.Id, Name = x.BranchName })
            .ToListAsync(cancellationToken);

        var designations = await _dbContext.Designations.AsNoTracking()
            .Where(x => x.Active == "Y")
            .OrderBy(x => x.DesignationName)
            .Select(x => new OptionDto { Id = x.Id, Name = x.DesignationName })
            .ToListAsync(cancellationToken);

        var divisions = await _dbContext.Divisions.AsNoTracking()
            .Where(x => x.Active == "Y")
            .OrderBy(x => x.DivisionName)
            .Select(x => new OptionDto { Id = x.Id, Name = x.DivisionName })
            .ToListAsync(cancellationToken);

        var departments = await _dbContext.Departments.AsNoTracking()
            .Where(x => x.Active == "Y")
            .OrderBy(x => x.Name)
            .Select(x => new OptionDto { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

        var reportings = await InternalUsersQuery(_dbContext.Users.AsNoTracking())
            .Where(x => x.Active == "Y")
            .OrderBy(x => x.Name)
            .Take(MaxRows)
            .Select(x => new OptionDto { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

        return new UserOptionsDto
        {
            Roles = roles,
            Branches = branches,
            Designations = designations,
            Divisions = divisions,
            Departments = departments,
            Reportings = reportings
        };
    }

    public async Task<IReadOnlyCollection<UserExcelRowDto>> ExportUsersAsync(UserExportFiltersDto filters, CancellationToken cancellationToken)
    {
        var query = ApplyUserFilters(_dbContext.Users.AsNoTracking(), filters);

        var users = await (
            from user in query
            join details in _dbContext.UserDetails.AsNoTracking() on user.Id equals details.UserId into detailsJoin
            from details in detailsJoin.DefaultIfEmpty()
            join designation in _dbContext.Designations.AsNoTracking() on user.DesignationId equals designation.Id into designationJoin
            from designation in designationJoin.DefaultIfEmpty()
            join department in _dbContext.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departmentJoin
            from department in departmentJoin.DefaultIfEmpty()
            join division in _dbContext.Divisions.AsNoTracking() on user.DivisionId equals division.Id into divisionJoin
            from division in divisionJoin.DefaultIfEmpty()
            join reporting in _dbContext.Users.AsNoTracking() on user.ReportingId equals reporting.Id into reportingJoin
            from reporting in reportingJoin.DefaultIfEmpty()
            orderby user.Id descending
            select new
            {
                user,
                details,
                DesignationName = designation == null ? null : designation.DesignationName,
                DepartmentName = department == null ? null : department.Name,
                DivisionName = division == null ? null : division.DivisionName,
                ReportingName = reporting == null ? null : reporting.Name
            })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(x => x.user.Id).ToArray();
        var roles = await (
            from modelRole in _dbContext.ModelHasRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on modelRole.RoleId equals role.Id
            where modelRole.ModelType == LaravelModelTypes.User && userIds.Contains(modelRole.ModelId)
            select new { modelRole.ModelId, role.Id, role.Name })
            .ToListAsync(cancellationToken);

        var branches = await _dbContext.Branches.AsNoTracking()
            .Select(x => new { x.Id, x.BranchName })
            .ToDictionaryAsync(x => x.Id.ToString(), x => x.BranchName, cancellationToken);

        return users.Select(row =>
        {
            var userRoles = roles.Where(x => x.ModelId == row.user.Id).ToArray();
            return new UserExcelRowDto
            {
                Id = row.user.Id,
                EmployeeCodes = row.user.EmployeeCodes,
                Name = row.user.Name,
                DesignationName = row.DesignationName,
                RoleNames = string.Join(", ", userRoles.Select(x => x.Name)),
                BranchNames = ResolveBranchNames(row.user.BranchId, branches),
                Location = row.user.Location,
                DepartmentName = row.DepartmentName,
                DivisionName = row.DivisionName,
                ReportingName = row.ReportingName,
                Mobile = row.user.Mobile,
                Email = row.user.Email,
                DateOfJoining = row.details?.DateOfJoining ?? row.user.DateOfJoining,
                DateOfBirth = row.details?.DateOfBirth,
                DateOfConfirmation = row.details?.DateOfConfirmation,
                DateOfLeaving = row.details?.DateOfLeaving,
                Grade = row.user.Grade,
                DesignationCode = row.user.BloodGroup,
                EmployeeSuperCode = row.user.PersonalNumber,
                BaseLocationCoordinates = string.IsNullOrWhiteSpace(row.user.Latitude) || string.IsNullOrWhiteSpace(row.user.Longitude)
                    ? null
                    : $"{row.user.Latitude}, {row.user.Longitude}",
                ReportingId = row.user.ReportingId,
                RoleIds = string.Join(", ", userRoles.Select(x => x.Id)),
                Payroll = row.user.Payroll,
                DesignationId = row.user.DesignationId,
                BranchId = row.user.BranchId,
                DivisionId = row.user.DivisionId,
                DepartmentId = row.user.DepartmentId,
                AttendanceSummaryReport = row.user.ShowAttandanceReport == "1" ? "Yes" : "No"
            };
        }).ToList();
    }

    public Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken) =>
        _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> UserEmailExistsAsync(string email, ulong? exceptUserId, CancellationToken cancellationToken) =>
        _dbContext.Users.AsNoTracking().AnyAsync(x =>
            x.Email == email
            && (!exceptUserId.HasValue || x.Id != exceptUserId.Value),
            cancellationToken);

    public Task<bool> UserMobileExistsAsync(string mobile, ulong? exceptUserId, CancellationToken cancellationToken) =>
        _dbContext.Users.AsNoTracking().AnyAsync(x =>
            x.Mobile == mobile
            && (!exceptUserId.HasValue || x.Id != exceptUserId.Value),
            cancellationToken);

    public Task<UserDetails?> GetUserDetailsAsync(ulong userId, CancellationToken cancellationToken) =>
        _dbContext.UserDetails.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken) =>
        _dbContext.Roles.FirstOrDefaultAsync(x => x.Name == roleName, cancellationToken);

    public async Task AddUserAsync(User user, CancellationToken cancellationToken) =>
        await _dbContext.Users.AddAsync(user, cancellationToken);

    public async Task AddUserDetailsAsync(UserDetails userDetails, CancellationToken cancellationToken) =>
        await _dbContext.UserDetails.AddAsync(userDetails, cancellationToken);

    public async Task AddUserEducationAsync(UserEducation userEducation, CancellationToken cancellationToken) =>
        await _dbContext.UserEducation.AddAsync(userEducation, cancellationToken);

    public async Task SyncUserRolesAsync(ulong userId, IEnumerable<ulong> roleIds, CancellationToken cancellationToken)
    {
        var currentRoles = _dbContext.ModelHasRoles.Where(x => x.ModelId == userId && x.ModelType == LaravelModelTypes.User);
        _dbContext.ModelHasRoles.RemoveRange(currentRoles);

        await _dbContext.ModelHasRoles.AddRangeAsync(roleIds.Distinct().Select(roleId => new ModelHasRole
        {
            RoleId = roleId,
            ModelId = userId,
            ModelType = LaravelModelTypes.User
        }), cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return false;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<User> ApplyUserFilters(IQueryable<User> query, UserExportFiltersDto filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Active))
        {
            query = query.Where(x => x.Active == filters.Active.Trim());
        }

        if (filters.DivisionId.HasValue)
        {
            query = query.Where(x => x.DivisionId == filters.DivisionId);
        }

        if (!string.IsNullOrWhiteSpace(filters.BranchId))
        {
            var branchId = filters.BranchId.Trim();
            query = query.Where(x => x.BranchId != null && (x.BranchId == branchId || x.BranchId.StartsWith(branchId + ",") || x.BranchId.EndsWith("," + branchId) || x.BranchId.Contains("," + branchId + ",")));
        }

        if (filters.DepartmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == filters.DepartmentId);
        }

        return string.Equals(filters.UserType, "customer", StringComparison.OrdinalIgnoreCase)
            ? query.Where(user => _dbContext.ModelHasRoles
                .Join(_dbContext.Roles, modelRole => modelRole.RoleId, role => role.Id, (modelRole, role) => new { modelRole, role })
                .Any(x => x.modelRole.ModelId == user.Id && x.modelRole.ModelType == LaravelModelTypes.User && x.role.Name == DistributorRoleName))
            : query.Where(user => !_dbContext.ModelHasRoles
                .Join(_dbContext.Roles, modelRole => modelRole.RoleId, role => role.Id, (modelRole, role) => new { modelRole, role })
                .Any(x => x.modelRole.ModelId == user.Id && x.modelRole.ModelType == LaravelModelTypes.User && x.role.Name == DistributorRoleName));
    }

    private IQueryable<User> InternalUsersQuery(IQueryable<User> query) =>
        query.Where(user =>
            !user.CustomerId.HasValue
            && !_dbContext.ModelHasRoles
                .Join(_dbContext.Roles, modelRole => modelRole.RoleId, role => role.Id, (modelRole, role) => new { modelRole, role })
                .Any(x => x.modelRole.ModelId == user.Id && x.modelRole.ModelType == LaravelModelTypes.User && x.role.Name == DistributorRoleName));

    private async Task<IReadOnlyCollection<UserDto>> ProjectUsersAsync(IQueryable<User> query, CancellationToken cancellationToken)
    {
        var users = await (
            from user in query
            join details in _dbContext.UserDetails.AsNoTracking() on user.Id equals details.UserId into detailsJoin
            from details in detailsJoin.DefaultIfEmpty()
            join designation in _dbContext.Designations.AsNoTracking() on user.DesignationId equals designation.Id into designationJoin
            from designation in designationJoin.DefaultIfEmpty()
            join department in _dbContext.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departmentJoin
            from department in departmentJoin.DefaultIfEmpty()
            join division in _dbContext.Divisions.AsNoTracking() on user.DivisionId equals division.Id into divisionJoin
            from division in divisionJoin.DefaultIfEmpty()
            join reporting in _dbContext.Users.AsNoTracking() on user.ReportingId equals reporting.Id into reportingJoin
            from reporting in reportingJoin.DefaultIfEmpty()
            select new
            {
                user,
                details,
                DesignationName = designation == null ? null : designation.DesignationName,
                DepartmentName = department == null ? null : department.Name,
                DivisionName = division == null ? null : division.DivisionName,
                ReportingName = reporting == null ? null : reporting.Name
            })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(x => x.user.Id).ToArray();
        var roles = await (
            from modelRole in _dbContext.ModelHasRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on modelRole.RoleId equals role.Id
            where modelRole.ModelType == LaravelModelTypes.User && userIds.Contains(modelRole.ModelId)
            select new { modelRole.ModelId, role.Id, role.Name })
            .ToListAsync(cancellationToken);

        var branches = await _dbContext.Branches.AsNoTracking()
            .Select(x => new { x.Id, x.BranchName })
            .ToDictionaryAsync(x => x.Id.ToString(), x => x.BranchName, cancellationToken);

        return users.Select(row => new UserDto
        {
            Id = row.user.Id,
            Active = row.user.Active,
            Name = row.user.Name,
            FirstName = row.user.FirstName,
            LastName = row.user.LastName,
            EmployeeCodes = row.user.EmployeeCodes,
            Mobile = row.user.Mobile,
            Email = row.user.Email,
            BranchId = row.user.BranchId,
            BranchNames = ResolveBranchNames(row.user.BranchId, branches),
            DesignationId = row.user.DesignationId,
            DesignationName = row.DesignationName,
            DivisionId = row.user.DivisionId,
            DivisionName = row.DivisionName,
            DepartmentId = row.user.DepartmentId,
            DepartmentName = row.DepartmentName,
            ReportingId = row.user.ReportingId,
            ReportingName = row.ReportingName,
            Location = row.user.Location,
            Latitude = row.user.Latitude,
            Longitude = row.user.Longitude,
            Payroll = row.user.Payroll,
            WarehouseId = row.user.WarehouseId,
            SalesType = row.user.SalesType,
            ShowAttandanceReport = row.user.ShowAttandanceReport,
            DateOfJoining = row.details?.DateOfJoining ?? row.user.DateOfJoining,
            PasswordString = row.user.PasswordString,
            CreatedAt = row.user.CreatedAt,
            UpdatedAt = row.user.UpdatedAt,
            Roles = roles
                .Where(x => x.ModelId == row.user.Id)
                .Select(x => new UserRoleDto { Id = x.Id, Name = x.Name })
                .ToArray()
        }).ToList();
    }

    private static string ResolveBranchNames(string? branchIds, IReadOnlyDictionary<string, string> branches)
    {
        if (string.IsNullOrWhiteSpace(branchIds)) return "-";
        var names = branchIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => branches.TryGetValue(id, out var name) ? name : null)
            .Where(name => !string.IsNullOrWhiteSpace(name));
        return string.Join(", ", names);
    }
}
