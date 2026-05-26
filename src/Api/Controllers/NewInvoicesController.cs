using System.Security.Claims;
using Api.Filters;
using Application.DTOs.NewInvoices;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/new-invoices")]
public sealed class NewInvoicesController : ControllerBase
{
    private readonly INewInvoiceService _newInvoiceService;

    public NewInvoicesController(INewInvoiceService newInvoiceService)
    {
        _newInvoiceService = newInvoiceService;
    }

    [RequirePermission("new_invoice_access")]
    [HttpGet]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] NewInvoiceFilterDto filter,
        [FromQuery(Name = "retailer_search")] string? retailerSearch,
        [FromQuery(Name = "invoice_number")] string? invoiceNumber,
        [FromQuery(Name = "approval_status")] int? approvalStatus,
        [FromQuery(Name = "branch_id")] ulong? branchId,
        [FromQuery(Name = "from_date")] DateTime? fromDate,
        [FromQuery(Name = "to_date")] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        filter.RetailerSearch ??= retailerSearch;
        filter.InvoiceNumber ??= invoiceNumber;
        filter.ApprovalStatus ??= approvalStatus;
        filter.BranchId ??= branchId;
        filter.FromDate ??= fromDate;
        filter.ToDate ??= toDate;
        var response = await _newInvoiceService.GetInvoicesAsync(filter, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_access", "new_invoice_create")]
    [HttpGet("retailers")]
    public async Task<IActionResult> GetRetailers([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.GetRetailersAsync(search, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_access")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoice(ulong id, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.GetInvoiceAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_create")]
    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] NewInvoiceRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.CreateInvoiceAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [RequirePermission("new_invoice_edit")]
    [HttpPut("{id}")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateInvoice(ulong id, [FromBody] NewInvoiceRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.UpdateInvoiceAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInvoice(ulong id, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.DeleteInvoiceAsync(id, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_approve_ss")]
    [HttpPost("{id}/approve/ss")]
    public async Task<IActionResult> ApproveSs(ulong id, [FromBody] NewInvoiceApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.ApproveInvoiceAsync(id, "ss", request.Remark, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_approve_sales")]
    [HttpPost("{id}/approve/sales")]
    public async Task<IActionResult> ApproveSales(ulong id, [FromBody] NewInvoiceApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.ApproveInvoiceAsync(id, "sales", request.Remark, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_approve_ho")]
    [HttpPost("{id}/approve/ho")]
    public async Task<IActionResult> ApproveHo(ulong id, [FromBody] NewInvoiceApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.ApproveInvoiceAsync(id, "ho", request.Remark, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_reject")]
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(ulong id, [FromBody] NewInvoiceApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.RejectInvoiceAsync(id, request.Remark, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }
}
