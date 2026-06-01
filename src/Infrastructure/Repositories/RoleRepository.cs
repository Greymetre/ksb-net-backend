using Application.DTOs.Roles;
using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private const int MaxRows = 50000;
    private readonly AppDbContext _dbContext;

    public RoleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(string? search, bool includePermissions, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Roles.AsNoTracking();
        if (!await IsSuperAdminAsync(actorUserId, cancellationToken))
        {
            query = query.Where(x => x.Id > 1);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search.Trim()));
        }

        var roles = await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        if (!includePermissions || roles.Count == 0)
        {
            return roles.Select(role => ToRoleDto(role, [])).ToList();
        }

        var permissionsByRole = await GetPermissionsByRoleAsync(roles.Select(x => x.Id), cancellationToken);
        return roles
            .Select(role => ToRoleDto(role, permissionsByRole.GetValueOrDefault(role.Id, [])))
            .ToList();
    }

    public async Task<RoleDto?> GetRoleAsync(ulong id, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null) return null;

        var permissionsByRole = await GetPermissionsByRoleAsync([id], cancellationToken);
        return ToRoleDto(role, permissionsByRole.GetValueOrDefault(id, []));
    }

    public async Task<RoleDto> CreateRoleAsync(RoleRequestDto request, CancellationToken cancellationToken)
    {
        var role = new Role
        {
            Name = request.Name!.Trim(),
            GuardName = request.GuardName ?? "users",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Roles.AddAsync(role, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.Permissions is not null)
        {
            await SyncRolePermissionsAsync(role.Id, request.Permissions, cancellationToken);
            return (await GetRoleAsync(role.Id, cancellationToken))!;
        }

        return ToRoleDto(role, []);
    }

    public async Task<RoleDto?> UpdateRoleAsync(ulong id, RoleRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null) return null;

        if (!string.IsNullOrWhiteSpace(request.Name)) role.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.GuardName)) role.GuardName = request.GuardName.Trim();
        role.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.Permissions is not null)
        {
            await SyncRolePermissionsAsync(role.Id, request.Permissions, cancellationToken);
        }

        return await GetRoleAsync(role.Id, cancellationToken);
    }

    public async Task<bool> DeleteRoleAsync(ulong id, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null) return false;

        var rolePermissions = _dbContext.RoleHasPermissions.Where(x => x.RoleId == id);
        var modelRoles = _dbContext.ModelHasRoles.Where(x => x.RoleId == id);
        _dbContext.RoleHasPermissions.RemoveRange(rolePermissions);
        _dbContext.ModelHasRoles.RemoveRange(modelRoles);
        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task SyncRolePermissionsAsync(ulong roleId, IEnumerable<ulong> permissionIds, CancellationToken cancellationToken)
    {
        var ids = permissionIds.Distinct().ToArray();
        var existingPermissionIds = await _dbContext.Permissions
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var current = _dbContext.RoleHasPermissions.Where(x => x.RoleId == roleId);
        _dbContext.RoleHasPermissions.RemoveRange(current);

        await _dbContext.RoleHasPermissions.AddRangeAsync(existingPermissionIds.Select(permissionId => new RoleHasPermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        }), cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveRolePermissionsAsync(IDictionary<ulong, IReadOnlyCollection<ulong>> permissionsByRole, CancellationToken cancellationToken)
    {
        foreach (var item in permissionsByRole)
        {
            if (await _dbContext.Roles.AnyAsync(x => x.Id == item.Key, cancellationToken))
            {
                await SyncRolePermissionsAsync(item.Key, item.Value, cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyCollection<PermissionDto>> GetPermissionsAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Permissions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search.Trim()));
        }

        return await query
            .OrderBy(x => x.Name)
            .Take(MaxRows)
            .Select(x => new PermissionDto { Id = x.Id, Name = x.Name, GuardName = x.GuardName })
            .ToListAsync(cancellationToken);
    }

    public Task<bool> RoleNameExistsAsync(string name, string guardName, ulong? exceptRoleId, CancellationToken cancellationToken) =>
        _dbContext.Roles.AsNoTracking().AnyAsync(x =>
            x.Name == name
            && x.GuardName == guardName
            && (!exceptRoleId.HasValue || x.Id != exceptRoleId.Value),
            cancellationToken);

    private async Task<Dictionary<ulong, IReadOnlyCollection<PermissionDto>>> GetPermissionsByRoleAsync(IEnumerable<ulong> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds.Distinct().ToArray();
        var rows = await _dbContext.RoleHasPermissions.AsNoTracking()
            .Where(x => ids.Contains(x.RoleId))
            .Join(_dbContext.Permissions.AsNoTracking(),
                rolePermission => rolePermission.PermissionId,
                permission => permission.Id,
                (rolePermission, permission) => new { rolePermission.RoleId, Permission = permission })
            .OrderBy(x => x.Permission.Name)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.RoleId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<PermissionDto>)group
                    .Select(x => new PermissionDto { Id = x.Permission.Id, Name = x.Permission.Name, GuardName = x.Permission.GuardName })
                    .ToList());
    }

    private async Task<bool> IsSuperAdminAsync(ulong? userId, CancellationToken cancellationToken)
    {
        if (!userId.HasValue) return false;

        return await _dbContext.ModelHasRoles.AsNoTracking()
            .Where(x => x.ModelId == userId.Value && x.ModelType == LaravelModelTypes.User)
            .Join(_dbContext.Roles.AsNoTracking(), modelRole => modelRole.RoleId, role => role.Id, (_, role) => role.Name)
            .AnyAsync(name => name == "superadmin", cancellationToken);
    }

    private static RoleDto ToRoleDto(Role role, IReadOnlyCollection<PermissionDto> permissions) => new()
    {
        Id = role.Id,
        Name = role.Name,
        GuardName = role.GuardName,
        CreatedAt = role.CreatedAt,
        UpdatedAt = role.UpdatedAt,
        Permissions = permissions
    };
}
