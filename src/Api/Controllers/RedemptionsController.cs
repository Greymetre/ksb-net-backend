using System.Security.Claims;
using Api.Filters;
using Application.DTOs.Redemptions;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/redemptions")]
public sealed class RedemptionsController : ControllerBase
{
    private readonly IRedemptionService _service;

    public RedemptionsController(IRedemptionService service)
    {
        _service = service;
    }

    [RequirePermission("redemption_access")]
    [HttpGet]
    public async Task<IActionResult> GetRedemptions(
        [FromQuery] RedemptionFilterDto filter,
        [FromQuery(Name = "redeem_mode")] string? redeemMode,
        CancellationToken cancellationToken)
    {
        filter.RedeemMode ??= redeemMode;
        return Ok(await _service.GetRedemptionsAsync(filter, cancellationToken));
    }

    [RequirePermission("redemption_download", "redemption_access")]
    [HttpGet("export")]
    public async Task<IActionResult> ExportRedemptions(
        [FromQuery] RedemptionFilterDto filter,
        [FromQuery(Name = "redeem_mode")] string? redeemMode,
        CancellationToken cancellationToken)
    {
        filter.RedeemMode ??= redeemMode;
        var file = await _service.ExportRedemptionsAsync(filter, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _service.GetCustomerOptionsAsync(search, CurrentUserId(), cancellationToken));

    [RequirePermission("redemption_create")]
    [HttpPost]
    public async Task<IActionResult> CreateRedemption([FromBody] RedemptionCreateRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _service.CreateRedemptionAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }
}
