using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class LaravelApiCompatibilityController : ControllerBase
{
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("{**path}", Order = 9999)]
    public IActionResult HandleLaravelApiRoute(string? path)
    {
        var normalizedPath = (path ?? string.Empty).Trim('/');
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            status = "error",
            message = "This Laravel API endpoint exists in routes/api.php, but its module logic has not been migrated to netProject yet.",
            endpoint = $"/api/{normalizedPath}",
            method = Request.Method,
            migration_status = "pending",
            note = "Existing migrated endpoints are handled by their concrete .NET controllers before this compatibility route."
        });
    }
}
