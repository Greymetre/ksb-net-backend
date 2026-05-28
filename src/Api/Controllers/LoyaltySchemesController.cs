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

    public LoyaltySchemesController(ILoyaltySchemeService loyaltySchemeService)
    {
        _loyaltySchemeService = loyaltySchemeService;
    }

    [RequirePermission("scheme_access_list", "scheme_access")]
    [HttpGet]
    public async Task<IActionResult> GetSchemes([FromQuery] LoyaltySchemeFilterDto filter, CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.GetSchemesAsync(filter, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("scheme_access_list", "scheme_access", "scheme_create", "scheme_edit")]
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
        var response = await _loyaltySchemeService.UpdateSchemeAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("scheme_delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScheme(ulong id, CancellationToken cancellationToken)
    {
        var response = await _loyaltySchemeService.DeleteSchemeAsync(id, cancellationToken);
        return Ok(response);
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }
}
