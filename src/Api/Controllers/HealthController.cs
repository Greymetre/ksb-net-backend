using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HealthController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("migration-status")]
    public async Task<IActionResult> MigrationStatus(CancellationToken cancellationToken)
    {
        var applied = (await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        var pending = (await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

        return Ok(new
        {
            status = pending.Length == 0 ? "success" : "pending",
            message = pending.Length == 0 ? "Database schema is up to date." : "Database migrations are pending.",
            latest_applied_migration = applied.LastOrDefault(),
            redemption_migration_applied = applied.Contains("20260727120000_AddRedemptionEnabledToLoyaltySchemes"),
            applied_migration_count = applied.Length,
            pending_migrations = pending
        });
    }
}
