using System.Security.Claims;
using Api.Filters;
using Application.DTOs.Roles;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [RequirePermission("role_access")]
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(
        [FromQuery] string? search,
        [FromQuery(Name = "include_permissions")] bool includePermissions = true,
        CancellationToken cancellationToken = default)
    {
        var response = await _roleService.GetRolesAsync(search, includePermissions, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("role_access")]
    [HttpGet("roles/{id}")]
    public async Task<IActionResult> GetRole(ulong id, CancellationToken cancellationToken)
    {
        var response = await _roleService.GetRoleAsync(id, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("role_create")]
    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] RoleRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _roleService.CreateRoleAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [RequirePermission("role_edit")]
    [HttpPut("roles/{id}")]
    [HttpPatch("roles/{id}")]
    public async Task<IActionResult> UpdateRole(ulong id, [FromBody] RoleRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _roleService.UpdateRoleAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("role_delete")]
    [HttpDelete("roles/{id}")]
    public async Task<IActionResult> DeleteRole(ulong id, CancellationToken cancellationToken)
    {
        var response = await _roleService.DeleteRoleAsync(id, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("role_edit")]
    [HttpPut("roles/{id}/permissions")]
    [HttpPatch("roles/{id}/permissions")]
    public async Task<IActionResult> SyncRolePermissions(ulong id, [FromBody] RolePermissionsRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _roleService.SyncRolePermissionsAsync(id, request.Permissions, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("role_edit")]
    [HttpPost("roles/save-permissions")]
    public async Task<IActionResult> SaveRolePermissions([FromBody] SaveRolePermissionsRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _roleService.SaveRolePermissionsAsync(request, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("role_access")]
    [HttpGet("permissions")]
    [HttpGet("roles/permissions")]
    public async Task<IActionResult> GetPermissions([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _roleService.GetPermissionsAsync(search, cancellationToken);
        return Ok(response);
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }
}
