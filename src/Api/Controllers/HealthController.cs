using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("migration-status")]
    public IActionResult MigrationStatus()
    {
        return Ok(new
        {
            status = "success",
            message = "ASP.NET Core migration scaffold is running",
            migrated_modules = new[] { "authentication-foundation", "users-schema", "customers-schema", "roles-permissions-schema" }
        });
    }
}
