using Application.DTOs.Roles;

namespace Application.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(string? search, bool includePermissions, ulong? actorUserId, CancellationToken cancellationToken);
    Task<RoleDto?> GetRoleAsync(ulong id, CancellationToken cancellationToken);
    Task<RoleDto> CreateRoleAsync(RoleRequestDto request, CancellationToken cancellationToken);
    Task<RoleDto?> UpdateRoleAsync(ulong id, RoleRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteRoleAsync(ulong id, CancellationToken cancellationToken);
    Task SyncRolePermissionsAsync(ulong roleId, IEnumerable<ulong> permissionIds, CancellationToken cancellationToken);
    Task SaveRolePermissionsAsync(IDictionary<ulong, IReadOnlyCollection<ulong>> permissionsByRole, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PermissionDto>> GetPermissionsAsync(string? search, CancellationToken cancellationToken);
    Task<bool> RoleNameExistsAsync(string name, string guardName, ulong? exceptRoleId, CancellationToken cancellationToken);
}
