using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeders;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var role = await db.Roles.FirstOrDefaultAsync(
            x => x.Name == "superadmin" && x.GuardName == "users",
            cancellationToken);

        if (role is null)
        {
            role = new Role
            {
                Name = "superadmin",
                GuardName = "users",
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync(cancellationToken);
        }

        var user = await db.Users
            .FirstOrDefaultAsync(x => x.Email == "gajendra@greymetre.io", cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Active = "Y",
                Name = "gajendra",
                FirstName = "gajendra",
                LastName = "admin",
                Mobile = "9713113280",
                Email = "gajendra@greymetre.io",
                Password = BCrypt.Net.BCrypt.HashPassword("9713113280"),
                NotificationId = string.Empty,
                DeviceType = string.Empty,
                Gender = string.Empty,
                ProfileImage = string.Empty,
                Latitude = string.Empty,
                Longitude = string.Empty,
                UserCode = string.Empty,
                Location = string.Empty,
                SalesType = string.Empty,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            user.Name = "gajendra";
            user.FirstName = "gajendra";
            user.LastName = string.IsNullOrWhiteSpace(user.LastName) ? "admin" : user.LastName;
            user.Active = "Y";
            user.Password = BCrypt.Net.BCrypt.HashPassword("9713113280");
            user.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        var permissionIds = await db.Permissions
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var assignedPermissionIds = await db.RoleHasPermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        var missingRolePermissions = permissionIds
            .Except(assignedPermissionIds)
            .Select(permissionId => new RoleHasPermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            })
            .ToArray();

        if (missingRolePermissions.Length > 0)
        {
            db.RoleHasPermissions.AddRange(missingRolePermissions);
            await db.SaveChangesAsync(cancellationToken);
        }

        var directPermissions = await db.ModelHasPermissions
            .Where(x => x.ModelId == user.Id && x.ModelType == LaravelModelTypes.User)
            .ToListAsync(cancellationToken);

        if (directPermissions.Count > 0)
        {
            db.ModelHasPermissions.RemoveRange(directPermissions);
            await db.SaveChangesAsync(cancellationToken);
        }

        var hasRole = await db.ModelHasRoles.AnyAsync(
            x => x.RoleId == role.Id && x.ModelId == user.Id && x.ModelType == LaravelModelTypes.User,
            cancellationToken);

        if (!hasRole)
        {
            db.ModelHasRoles.Add(new ModelHasRole
            {
                RoleId = role.Id,
                ModelId = user.Id,
                ModelType = LaravelModelTypes.User
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
