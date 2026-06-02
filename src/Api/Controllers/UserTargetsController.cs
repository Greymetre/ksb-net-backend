using Api.Filters;
using Application.DTOs.UserTargets;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class UserTargetsController : ControllerBase
{
    private readonly IUserTargetService _service;

    public UserTargetsController(IUserTargetService service)
    {
        _service = service;
    }

    [Authorize]
    [RequirePermission("target_access", "target_users_access")]
    [HttpGet("user-targets")]
    public async Task<IActionResult> GetTargets([FromQuery(Name = "branch_id")] ulong? branchId, [FromQuery(Name = "user_id")] ulong? userId, [FromQuery(Name = "division_id")] ulong? divisionId, [FromQuery] string? type, [FromQuery] string? month, [FromQuery(Name = "financial_year")] string? financialYear, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _service.GetTargetsAsync(new UserTargetFilterDto { BranchId = branchId, UserId = userId, DivisionId = divisionId, Type = type, Month = month, FinancialYear = financialYear, Search = search }, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("target_access", "target_users_access")]
    [HttpGet("user-targets/options")]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken) =>
        Ok(await _service.GetOptionsAsync(cancellationToken));

    [Authorize]
    [RequirePermission("target_access", "target_users_access")]
    [HttpGet("user-targets/{id}")]
    public async Task<IActionResult> GetTarget(ulong id, CancellationToken cancellationToken) =>
        Ok(await _service.GetTargetAsync(id, cancellationToken));

    [Authorize]
    [RequirePermission("target_access", "target_users_access", "target_users_access_create")]
    [HttpPost("user-targets")]
    public async Task<IActionResult> CreateTarget([FromBody] UserTargetRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _service.CreateTargetAsync(request, cancellationToken));

    [Authorize]
    [RequirePermission("target_access", "target_users_access", "target_users_access_edit")]
    [HttpPut("user-targets/{id}")]
    [HttpPatch("user-targets/{id}")]
    public async Task<IActionResult> UpdateTarget(ulong id, [FromBody] UserTargetRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateTargetAsync(id, request, cancellationToken));

    [Authorize]
    [RequirePermission("target_access", "target_users_access", "target_users_access_delete")]
    [HttpDelete("user-targets/{id}")]
    public async Task<IActionResult> DeleteTarget(ulong id, CancellationToken cancellationToken) =>
        Ok(await _service.DeleteTargetAsync(id, cancellationToken));

    [Authorize]
    [RequirePermission("target_access", "target_users_access", "sales_target_users_download")]
    [HttpGet("user-targets/export")]
    public async Task<IActionResult> ExportTargets([FromQuery(Name = "branch_id")] ulong? branchId, [FromQuery(Name = "user_id")] ulong? userId, [FromQuery(Name = "division_id")] ulong? divisionId, [FromQuery] string? type, [FromQuery] string? month, [FromQuery(Name = "financial_year")] string? financialYear, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var file = await _service.ExportTargetsAsync(new UserTargetFilterDto { BranchId = branchId, UserId = userId, DivisionId = divisionId, Type = type, Month = month, FinancialYear = financialYear, Search = search }, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("target_access", "target_users_access", "sales_target_users_template")]
    [HttpGet("user-targets/template")]
    public async Task<IActionResult> Template(CancellationToken cancellationToken)
    {
        var file = await _service.GetTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("target_access", "target_users_access", "sales_target_users_upload")]
    [HttpPost("user-targets/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadTargets(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        return Ok(await _service.UploadTargetsAsync(stream, cancellationToken));
    }
}

