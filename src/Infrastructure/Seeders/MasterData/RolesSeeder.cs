using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeders.MasterData;

public static class RolesSeeder
{
    private const string GuardName = "users";
    private const string DistributorRoleName = "Distributor";

    private static readonly string[] DistributorPermissions =
    [
        "dashboard_access",
        "scheme_access",
        "new_invoice_access",
        "new_invoice_create"
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var role = await db.Roles.FirstOrDefaultAsync(
            x => x.Name == DistributorRoleName && x.GuardName == GuardName,
            cancellationToken);

        if (role is null)
        {
            role = new Role
            {
                Name = DistributorRoleName,
                GuardName = GuardName,
                CreatedAt = now,
                UpdatedAt = now
            };
            await db.Roles.AddAsync(role, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            role.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        var permissionIds = await db.Permissions
            .Where(x => x.GuardName == GuardName && DistributorPermissions.Contains(x.Name))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var currentPermissionIds = await db.RoleHasPermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        var missing = permissionIds
            .Except(currentPermissionIds)
            .Select(permissionId => new RoleHasPermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            })
            .ToArray();

        if (missing.Length > 0)
        {
            await db.RoleHasPermissions.AddRangeAsync(missing, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
