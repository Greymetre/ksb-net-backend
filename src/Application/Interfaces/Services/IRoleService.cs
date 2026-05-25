using Application.DTOs.Roles;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IRoleService
{
    Task<LaravelApiResponse> GetRolesAsync(string? search, bool includePermissions, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetRoleAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateRoleAsync(RoleRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateRoleAsync(ulong id, RoleRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteRoleAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SyncRolePermissionsAsync(ulong id, IReadOnlyCollection<ulong> permissionIds, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SaveRolePermissionsAsync(SaveRolePermissionsRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetPermissionsAsync(string? search, CancellationToken cancellationToken);
}
