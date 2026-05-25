using System.Security.Claims;
using Domain.Constants;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(params string[] permissions) : base(typeof(RequirePermissionFilter))
    {
        Arguments = [permissions];
    }
}

public sealed class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    private readonly IReadOnlyCollection<string> _permissions;
    private readonly AppDbContext _dbContext;

    public RequirePermissionFilter(string[] permissions, AppDbContext dbContext)
    {
        _permissions = permissions.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        _dbContext = dbContext;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ObjectResult(LaravelApiResponse.MessageOnly("error", "Unauthenticated."))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        var provider = user.FindFirstValue("provider") ?? "users";
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (provider != "users" || !ulong.TryParse(subject, out var userId))
        {
            context.Result = Forbidden();
            return;
        }

        if (_permissions.Count == 0 || await HasAnyPermissionAsync(userId, context.HttpContext.RequestAborted))
        {
            return;
        }

        context.Result = Forbidden();
    }

    private async Task<bool> HasAnyPermissionAsync(ulong userId, CancellationToken cancellationToken)
    {
        var hasRolePermission = await _dbContext.ModelHasRoles
            .Where(x => x.ModelId == userId && x.ModelType == LaravelModelTypes.User)
            .Join(_dbContext.RoleHasPermissions, modelRole => modelRole.RoleId, rolePermission => rolePermission.RoleId, (_, rolePermission) => rolePermission)
            .Join(_dbContext.Permissions, rolePermission => rolePermission.PermissionId, permission => permission.Id, (_, permission) => permission.Name)
            .AnyAsync(permission => _permissions.Contains(permission), cancellationToken);

        if (hasRolePermission)
        {
            return true;
        }

        return await _dbContext.ModelHasPermissions
            .Where(x => x.ModelId == userId && x.ModelType == LaravelModelTypes.User)
            .Join(_dbContext.Permissions, modelPermission => modelPermission.PermissionId, permission => permission.Id, (_, permission) => permission.Name)
            .AnyAsync(permission => _permissions.Contains(permission), cancellationToken);
    }

    private static ObjectResult Forbidden() =>
        new(LaravelApiResponse.MessageOnly("error", "403 Forbidden"))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
}
