using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal static class ReportingVisibility
{
    private const string DistributorRoleName = "Distributor";
    private const ulong CustomerRoleId = 61;
    private const int MaxRows = 50000;

    private static readonly HashSet<string> PrivilegedReportingRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin", "Admin", "ZONAL", "subAdmin", "GM.", "CRM", "HR_Admin", "HO_Account",
        "Sub_Support", "Accounts Order", "Service Admin", "All Customers", "Sub billing",
        "Sales Admin", "Marketing_Admin", "MIS_ADMIN", "Data_Crm"
    };

    private static readonly HashSet<string> BranchWideReportingRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "BM.", "Marketing Team"
    };

    public static IQueryable<User> InternalUsersQuery(AppDbContext db, IQueryable<User> query) =>
        query.Where(user =>
            !user.CustomerId.HasValue
            && !db.ModelHasRoles
                .Join(db.Roles, modelRole => modelRole.RoleId, role => role.Id, (modelRole, role) => new { modelRole, role })
                .Any(x => x.modelRole.ModelId == user.Id
                    && x.modelRole.ModelType == LaravelModelTypes.User
                    && (x.role.Name == DistributorRoleName || x.modelRole.RoleId == CustomerRoleId)));

    public static async Task<IReadOnlyCollection<ulong>> GetVisibleUserIdsAsync(AppDbContext db, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var internalUsers = await InternalUsersQuery(db, db.Users.AsNoTracking())
            .Where(x => x.Active == "Y" && !x.IsDeleted)
            .Select(x => new { x.Id, x.ReportingId, x.BranchId })
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        if (!actorUserId.HasValue)
        {
            return internalUsers.Select(x => x.Id).ToArray();
        }

        var actor = await db.Users.AsNoTracking()
            .Where(x => x.Id == actorUserId.Value)
            .Select(x => new { x.Id, x.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (actor is null)
        {
            return [];
        }

        var roleNames = await db.ModelHasRoles.AsNoTracking()
            .Where(modelRole => modelRole.ModelId == actorUserId.Value && modelRole.ModelType == LaravelModelTypes.User)
            .Join(db.Roles.AsNoTracking(), modelRole => modelRole.RoleId, role => role.Id, (_, role) => role.Name)
            .ToListAsync(cancellationToken);

        if (roleNames.Any(role => BranchWideReportingRoles.Contains(role)))
        {
            var actorBranches = SplitCsv(actor.BranchId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (actorBranches.Count == 0) return [actor.Id];

            return internalUsers
                .Where(user => SplitCsv(user.BranchId).Any(actorBranches.Contains))
                .Select(user => user.Id)
                .Distinct()
                .ToArray();
        }

        if (roleNames.Any(role => PrivilegedReportingRoles.Contains(role)))
        {
            return internalUsers.Select(x => x.Id).ToArray();
        }

        var visible = new HashSet<ulong> { actor.Id };
        var frontier = new HashSet<ulong> { actor.Id };
        while (frontier.Count > 0)
        {
            var children = internalUsers
                .Where(user => user.ReportingId.HasValue && frontier.Contains(user.ReportingId.Value) && visible.Add(user.Id))
                .Select(user => user.Id)
                .ToArray();

            frontier = children.ToHashSet();
        }

        return visible.ToArray();
    }

    private static string[] SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
