using Api.Filters;
using Application.DTOs.Expenses;
using Application.DTOs.MasterData;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expenses-types")]
public sealed class ExpenseTypesController : ControllerBase
{
    private readonly IExpenseTypeService _service;

    public ExpenseTypesController(IExpenseTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission("expenses_type")]
    public async Task<IActionResult> GetExpenseTypes([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _service.GetExpenseTypesAsync(search, cancellationToken);
        return Ok(response);
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken)
    {
        var response = await _service.GetOptionsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [RequirePermission("expenses_type")]
    public async Task<IActionResult> GetExpenseType(ulong id, CancellationToken cancellationToken)
    {
        var response = await _service.GetExpenseTypeAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [RequirePermission("expenses_type_create")]
    public async Task<IActionResult> CreateExpenseType([FromBody] ExpenseTypeRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _service.CreateExpenseTypeAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id}")]
    [HttpPatch("{id}")]
    [RequirePermission("expenses_type_update")]
    public async Task<IActionResult> UpdateExpenseType(ulong id, [FromBody] ExpenseTypeRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _service.UpdateExpenseTypeAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id}/status")]
    [RequirePermission("expenses_type_update")]
    public async Task<IActionResult> SetExpenseTypeActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _service.SetExpenseTypeActiveAsync(id, request.Active, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [RequirePermission("expenses_type_update")]
    public async Task<IActionResult> DeleteExpenseType(ulong id, CancellationToken cancellationToken)
    {
        var response = await _service.DeleteExpenseTypeAsync(id, cancellationToken);
        return Ok(response);
    }
}
