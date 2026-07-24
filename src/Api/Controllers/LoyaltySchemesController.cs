using System.Security.Claims;
using Api.Filters;
using Application.DTOs.LoyaltySchemes;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/loyalty-schemes")]
public sealed class LoyaltySchemesController : ControllerBase
{
    private readonly ILoyaltySchemeService _loyaltySchemeService;
    private readonly IWebHostEnvironment _environment;

    public LoyaltySchemesController(ILoyaltySchemeService loyaltySchemeService, IWebHostEnvironment environment)
    {
        _loyaltySchemeService = loyaltySchemeService;
        _environment = environment;
    }

    [RequirePermission("scheme_access_list", "scheme_access")]
    [HttpGet]
    public async Task<IActionResult> GetSchemes([FromQuery] LoyaltySchemeFilterDto filter, CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.GetSchemesAsync(filter, cancellationToken);
        return Ok(response);
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.GetOptionsAsync(cancellationToken);
        return Ok(response);
    }

    [RequirePermission("scheme_access_list", "scheme_access", "scheme_create", "scheme_edit")]
    [HttpGet("generate-code")]
    public async Task<IActionResult> GenerateCode(
        [FromQuery(Name = "scheme_name")] string? schemeName,
        [FromQuery(Name = "scheme_tag")] string? schemeTag,
        [FromQuery(Name = "based_on")] string? basedOn,
        CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.GenerateSchemeCodeAsync(schemeName, schemeTag, basedOn, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("scheme_access_list", "scheme_access")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetScheme(ulong id, CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.GetSchemeAsync(id, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("scheme_create")]
    [HttpPost]
    public async Task<IActionResult> CreateScheme([FromBody] LoyaltySchemeRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.CreateSchemeAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [RequirePermission("scheme_edit")]
    [HttpPut("{id}")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateScheme(ulong id, [FromBody] LoyaltySchemeRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.UpdateSchemeAsync(id, request, CurrentUserId(), IsSuperAdmin(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("scheme_edit")]
    [HttpPost("{id}/draft")]
    public async Task<IActionResult> SendToDraft(ulong id, CancellationToken cancellationToken)
    {
        return Ok(await _loyaltySchemeService.SendToDraftAsync(id, CurrentUserId(), IsSuperAdmin(), cancellationToken));
    }

    [RequirePermission("scheme_submit")]
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitScheme(ulong id, CancellationToken cancellationToken)
    {
        return Ok(await _loyaltySchemeService.SubmitSchemeAsync(id, CurrentUserId(), cancellationToken));
    }

    [RequirePermission("scheme_approve")]
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveScheme(ulong id, [FromBody] LoyaltySchemeDecisionDto request, CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.ApproveSchemeAsync(id, request.Remark, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("scheme_reject", "scheme_approve")]
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectScheme(ulong id, [FromBody] LoyaltySchemeDecisionDto request, CancellationToken cancellationToken)
    {
        return Ok(await _loyaltySchemeService.RejectSchemeAsync(id, request.Remark, CurrentUserId(), cancellationToken));
    }

    [RequirePermission("scheme_publish")]
    [HttpPost("{id}/publish")]
    public async Task<IActionResult> PublishScheme(ulong id, CancellationToken cancellationToken)
    {
        return Ok(await _loyaltySchemeService.PublishSchemeAsync(id, CurrentUserId(), cancellationToken));
    }

    [RequirePermission("scheme_create", "scheme_edit")]
    [HttpPost("{id}/brochure")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadBrochure(ulong id, IFormFile brochure, CancellationToken cancellationToken)
    {
        if (brochure.Length == 0 || brochure.Length > 10 * 1024 * 1024)
            return BadRequest(new { status = "error", message = "Brochure must be a non-empty PDF up to 10 MB." });
        if (!string.Equals(Path.GetExtension(brochure.FileName), ".pdf", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(brochure.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { status = "error", message = "Only PDF brochure files are allowed." });

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "loyalty-schemes");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}.pdf";
        await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
            await brochure.CopyToAsync(stream, cancellationToken);
        return Ok(await _loyaltySchemeService.SetBrochureAsync(id, $"/uploads/loyalty-schemes/{fileName}", CurrentUserId(), cancellationToken));
    }

    [RequirePermission("scheme_delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScheme(ulong id, CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.DeleteSchemeAsync(id, CurrentUserId(), IsSuperAdmin(), cancellationToken);
        return Ok(response);
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }

    private bool IsSuperAdmin() =>
        User.Claims.Any(claim => claim.Type == ClaimTypes.Role
            && string.Equals(claim.Value, "superadmin", StringComparison.OrdinalIgnoreCase));
}
