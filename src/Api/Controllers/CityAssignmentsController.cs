using Api.Filters;
using Application.DTOs.CityAssignments;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class CityAssignmentsController : ControllerBase
{
    private readonly ICityAssignmentService _service;

    public CityAssignmentsController(ICityAssignmentService service)
    {
        _service = service;
    }

    [Authorize]
    [RequirePermission("city_assigned")]
    [HttpGet("usercity")]
    [HttpGet("city-assignments")]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] string? search,
        [FromQuery(Name = "user_id")] ulong? userId,
        [FromQuery(Name = "page_number")] int pageNumber,
        [FromQuery(Name = "page_length")] int pageLength,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetAssignmentsAsync(new CityAssignmentFilterDto
        {
            Search = search,
            UserId = userId,
            PageNumber = pageNumber <= 0 ? 1 : pageNumber,
            PageLength = pageLength <= 0 ? 50000 : pageLength
        }, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("city-assignments/options")]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken) =>
        Ok(await _service.GetOptionsAsync(CurrentUserId(), cancellationToken));

    [Authorize]
    [RequirePermission("city_assigned")]
    [HttpPost("city-assignments")]
    public async Task<IActionResult> SaveAssignment([FromBody] CityAssignmentRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _service.SaveAssignmentAsync(request, cancellationToken));

    [Authorize]
    [RequirePermission("city_assigned")]
    [HttpDelete("city-assignments/{id}")]
    public async Task<IActionResult> DeleteAssignment(ulong id, CancellationToken cancellationToken) =>
        Ok(await _service.DeleteAssignmentAsync(id, cancellationToken));

    [Authorize]
    [RequirePermission("user_download", "city_assigned")]
    [HttpGet("usercity-download")]
    [HttpGet("city-assignments/export")]
    public async Task<IActionResult> ExportAssignments(
        [FromQuery] string? search,
        [FromQuery(Name = "user_id")] ulong? userId,
        [FromQuery(Name = "page_number")] int pageNumber,
        [FromQuery(Name = "page_length")] int pageLength,
        CancellationToken cancellationToken)
    {
        var file = await _service.ExportAssignmentsAsync(new CityAssignmentFilterDto
        {
            Search = search,
            UserId = userId,
            PageNumber = pageNumber <= 0 ? 1 : pageNumber,
            PageLength = pageLength <= 0 ? 50000 : pageLength
        }, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("city_assigned")]
    [HttpGet("city-assignments/template")]
    public async Task<IActionResult> Template(CancellationToken cancellationToken)
    {
        var file = await _service.GetTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("user_upload", "city_assigned")]
    [HttpPost("usercity-upload")]
    [HttpPost("city-assignments/upload")]
    public async Task<IActionResult> UploadAssignments(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        return Ok(await _service.UploadAssignmentsAsync(stream, cancellationToken));
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }
}
