using Application.Common;
using Application.DTOs.Roles;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Services;

public sealed class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;

    public RoleService(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetRolesAsync(string? search, bool includePermissions, ulong? actorUserId, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("roles", await _repository.GetRolesAsync(search, includePermissions, actorUserId, cancellationToken));

    public async Task<LaravelApiResponse> GetRoleAsync(ulong id, CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleAsync(id, cancellationToken);
        return LaravelApiResponse.Success("role", role ?? throw NotFound("Role not found"));
    }

    public async Task<LaravelApiResponse> CreateRoleAsync(RoleRequestDto request, CancellationToken cancellationToken)
    {
        var guardName = NormalizeGuard(request.GuardName);
        RequireName(request.Name);
        if (await _repository.RoleNameExistsAsync(request.Name!.Trim(), guardName, null, cancellationToken))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "The role name has already been taken.");
        }

        request.GuardName = guardName;
        var role = await _repository.CreateRoleAsync(request, cancellationToken);
        return LaravelApiResponse.Success("role", role, "Role created successfully");
    }

    public async Task<LaravelApiResponse> UpdateRoleAsync(ulong id, RoleRequestDto request, CancellationToken cancellationToken)
    {
        var current = await _repository.GetRoleAsync(id, cancellationToken);
        if (current is null) throw NotFound("Role not found");

        var guardName = NormalizeGuard(request.GuardName ?? current.GuardName);
        if (!string.IsNullOrWhiteSpace(request.Name)
            && await _repository.RoleNameExistsAsync(request.Name.Trim(), guardName, id, cancellationToken))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "The role name has already been taken.");
        }

        request.GuardName = guardName;
        var role = await _repository.UpdateRoleAsync(id, request, cancellationToken);
        return LaravelApiResponse.Success("role", role ?? throw NotFound("Role not found"), "Role updated successfully");
    }

    public async Task<LaravelApiResponse> DeleteRoleAsync(ulong id, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteRoleAsync(id, cancellationToken)) throw NotFound("Role not found");
        return LaravelApiResponse.MessageOnly("success", "Role deleted successfully!");
    }

    public async Task<LaravelApiResponse> SyncRolePermissionsAsync(ulong id, IReadOnlyCollection<ulong> permissionIds, CancellationToken cancellationToken)
    {
        if (await _repository.GetRoleAsync(id, cancellationToken) is null) throw NotFound("Role not found");
        await _repository.SyncRolePermissionsAsync(id, permissionIds, cancellationToken);
        return LaravelApiResponse.Success("role", await _repository.GetRoleAsync(id, cancellationToken), "Permissions updated successfully");
    }

    public async Task<LaravelApiResponse> SaveRolePermissionsAsync(SaveRolePermissionsRequestDto request, CancellationToken cancellationToken)
    {
        var permissionsByRole = new Dictionary<ulong, IReadOnlyCollection<ulong>>();
        foreach (var item in request.Permissions)
        {
            if (ulong.TryParse(item.Key, out var roleId))
            {
                permissionsByRole[roleId] = item.Value;
            }
        }

        await _repository.SaveRolePermissionsAsync(permissionsByRole, cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Permissions updated successfully");
    }

    public async Task<LaravelApiResponse> GetPermissionsAsync(string? search, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("permissions", await _repository.GetPermissionsAsync(search, cancellationToken));

    private static void RequireName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Role name is required.");
        }
    }

    private static string NormalizeGuard(string? guardName) =>
        string.IsNullOrWhiteSpace(guardName) ? "users" : guardName.Trim();

    private static LaravelHttpException NotFound(string message) =>
        new(LaravelStatusCodes.NotFound, message);
}
