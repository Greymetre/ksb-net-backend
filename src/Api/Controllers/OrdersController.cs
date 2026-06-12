using Api.Filters;
using Application.DTOs.Orders;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    [Authorize]
    [RequirePermission("order_access")]
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery(Name = "retailers_id")] ulong? retailersId,
        [FromQuery(Name = "distributor_id")] ulong? distributorId,
        [FromQuery(Name = "user_id")] ulong? userId,
        [FromQuery(Name = "division_id")] ulong? divisionId,
        [FromQuery(Name = "designation_id")] ulong[] designationIds,
        [FromQuery(Name = "pending_status")] int? pendingStatus,
        [FromQuery(Name = "startdate")] DateTime? startDate,
        [FromQuery(Name = "enddate")] DateTime? endDate,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = new OrderFilterDto
        {
            RetailersId = retailersId,
            DistributorId = distributorId,
            UserId = userId,
            DivisionId = divisionId,
            DesignationIds = designationIds,
            PendingStatus = pendingStatus,
            StartDate = startDate,
            EndDate = endDate,
            Search = search,
            ActorUserId = CurrentUserId()
        };
        return Ok(await _service.GetOrdersAsync(filter, cancellationToken));
    }

    [Authorize]
    [HttpGet("orders/options")]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken) =>
        Ok(await _service.GetOptionsAsync(CurrentUserId(), cancellationToken));

    [Authorize]
    [HttpGet("orders/products")]
    public async Task<IActionResult> GetProductsByFamily([FromQuery(Name = "subcategory_id")] ulong subcategoryId, CancellationToken cancellationToken) =>
        Ok(await _service.GetProductsByFamilyAsync(subcategoryId, cancellationToken));

    [Authorize]
    [RequirePermission("order_access", "order_show")]
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrder(ulong id, CancellationToken cancellationToken) =>
        Ok(await _service.GetOrderAsync(id, cancellationToken));

    [Authorize]
    [RequirePermission("order_access", "order_create")]
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _service.CreateOrderAsync(request, CurrentUserId(), cancellationToken));

    [Authorize]
    [RequirePermission("order_access", "order_download")]
    [HttpGet("orders/export")]
    public async Task<IActionResult> ExportOrders(
        [FromQuery(Name = "retailers_id")] ulong? retailersId,
        [FromQuery(Name = "distributor_id")] ulong? distributorId,
        [FromQuery(Name = "user_id")] ulong? userId,
        [FromQuery(Name = "division_id")] ulong? divisionId,
        [FromQuery(Name = "designation_id")] ulong[] designationIds,
        [FromQuery(Name = "pending_status")] int? pendingStatus,
        [FromQuery(Name = "startdate")] DateTime? startDate,
        [FromQuery(Name = "enddate")] DateTime? endDate,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var file = await _service.ExportOrdersAsync(new OrderFilterDto
        {
            RetailersId = retailersId,
            DistributorId = distributorId,
            UserId = userId,
            DivisionId = divisionId,
            DesignationIds = designationIds,
            PendingStatus = pendingStatus,
            StartDate = startDate,
            EndDate = endDate,
            Search = search,
            ActorUserId = CurrentUserId()
        }, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }
}
