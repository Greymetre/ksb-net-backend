using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _dbContext;

    public AuthRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken) =>
        _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Mobile == username || x.Email == username, cancellationToken);

    public Task<Customer?> FindCustomerByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var normalized = "91" + username.TrimStart('0', '+');
        return _dbContext.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Email == username || x.Mobile == username || x.Mobile == normalized, cancellationToken);
    }

    public Task<bool> UserMobileExistsAsync(string mobile, CancellationToken cancellationToken) =>
        _dbContext.Users.IgnoreQueryFilters().AnyAsync(x => x.Mobile == mobile, cancellationToken);

    public Task<bool> UserEmailExistsAsync(string email, CancellationToken cancellationToken) =>
        _dbContext.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == email, cancellationToken);

    public async Task<User> AddUserAsync(User user, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
        return user;
    }

    public async Task AddUserDetailsAsync(UserDetails userDetails, CancellationToken cancellationToken) =>
        await _dbContext.UserDetails.AddAsync(userDetails, cancellationToken);

    public async Task AddUserCityAssignsAsync(IEnumerable<UserCityAssign> cityAssigns, CancellationToken cancellationToken) =>
        await _dbContext.UserCityAssigns.AddRangeAsync(cityAssigns, cancellationToken);

    public async Task AddUserEducationAsync(IEnumerable<UserEducation> education, CancellationToken cancellationToken) =>
        await _dbContext.UserEducation.AddRangeAsync(education, cancellationToken);

    public async Task SyncUserRolesAndRolePermissionsAsync(ulong userId, IEnumerable<ulong> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds.Distinct().ToArray();
        var currentRoles = _dbContext.ModelHasRoles.Where(x => x.ModelId == userId && x.ModelType == LaravelModelTypes.User);
        _dbContext.ModelHasRoles.RemoveRange(currentRoles);

        await _dbContext.ModelHasRoles.AddRangeAsync(ids.Select(roleId => new ModelHasRole
        {
            RoleId = roleId,
            ModelId = userId,
            ModelType = LaravelModelTypes.User
        }), cancellationToken);

        var currentPermissions = _dbContext.ModelHasPermissions.Where(x => x.ModelId == userId && x.ModelType == LaravelModelTypes.User);
        _dbContext.ModelHasPermissions.RemoveRange(currentPermissions);
    }

    public async Task<Customer> AddCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        await _dbContext.Customers.AddAsync(customer, cancellationToken);
        return customer;
    }

    public async Task<IReadOnlyCollection<Role>> GetUserRolesAsync(ulong userId, CancellationToken cancellationToken)
    {
        return await _dbContext.ModelHasRoles
            .Where(x => x.ModelId == userId && x.ModelType == LaravelModelTypes.User)
            .Join(_dbContext.Roles, modelRole => modelRole.RoleId, role => role.Id, (_, role) => role)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Permission>> GetUserPermissionsAsync(ulong userId, CancellationToken cancellationToken)
    {
        return await _dbContext.ModelHasRoles
            .Where(x => x.ModelId == userId && x.ModelType == LaravelModelTypes.User)
            .Join(_dbContext.RoleHasPermissions, modelRole => modelRole.RoleId, rolePermission => rolePermission.RoleId, (_, rolePermission) => rolePermission)
            .Join(_dbContext.Permissions, rolePermission => rolePermission.PermissionId, permission => permission.Id, (_, permission) => permission)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<MobileUserLoginDetail?> GetUserLoginDetailAsync(ulong userId, CancellationToken cancellationToken) =>
        _dbContext.MobileUserLoginDetails.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task UpsertLoginDetailAsync(MobileUserLoginDetail detail, CancellationToken cancellationToken)
    {
        MobileUserLoginDetail? current = null;
        if (detail.UserId.HasValue)
        {
            current = await _dbContext.MobileUserLoginDetails.FirstOrDefaultAsync(x => x.UserId == detail.UserId, cancellationToken);
        }
        else if (detail.CustomerId.HasValue)
        {
            current = await _dbContext.MobileUserLoginDetails.FirstOrDefaultAsync(x => x.CustomerId == detail.CustomerId, cancellationToken);
        }

        if (current is null)
        {
            detail.CreatedAt = DateTime.UtcNow;
            detail.UpdatedAt = DateTime.UtcNow;
            await _dbContext.MobileUserLoginDetails.AddAsync(detail, cancellationToken);
            return;
        }

        current.AppVersion = detail.AppVersion ?? current.AppVersion;
        current.DeviceName = detail.DeviceName ?? current.DeviceName;
        current.DeviceType = detail.DeviceType ?? current.DeviceType;
        current.UniqueId = detail.UniqueId ?? current.UniqueId;
        current.FirstLoginDate = detail.FirstLoginDate ?? current.FirstLoginDate;
        current.LastLoginDate = detail.LastLoginDate ?? current.LastLoginDate;
        current.LoginStatus = detail.LoginStatus;
        current.App = detail.App ?? current.App;
        current.LoginAt = detail.LoginAt ?? current.LoginAt;
        current.UpdatedAt = DateTime.UtcNow;
    }

    public async Task StoreTokenAsync(OAuthAccessToken token, CancellationToken cancellationToken) =>
        await _dbContext.OAuthAccessTokens.AddAsync(token, cancellationToken);

    public async Task RevokeTokenAsync(string tokenId, CancellationToken cancellationToken)
    {
        var token = await _dbContext.OAuthAccessTokens.FirstOrDefaultAsync(x => x.Id == tokenId, cancellationToken);
        if (token is null) return;
        token.Revoked = true;
        token.UpdatedAt = DateTime.UtcNow;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
