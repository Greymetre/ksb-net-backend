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
    private readonly IWebHostEnvironment _environment;

    public NewInvoicesController(INewInvoiceService newInvoiceService, IWebHostEnvironment environment)
    {
        _newInvoiceService = newInvoiceService;
        _environment = environment;
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

    [RequirePermission("new_invoice_export", "new_invoice_access")]
    [HttpGet("export")]
    public async Task<IActionResult> ExportInvoices(
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
        var file = await _newInvoiceService.ExportInvoicesAsync(filter, CurrentUserId(), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
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
    public async Task<IActionResult> CreateInvoice([FromForm] NewInvoiceFormRequest form, CancellationToken cancellationToken)
    {
        var request = await ToRequestAsync(form, cancellationToken);
        var response = await _newInvoiceService.CreateInvoiceAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [RequirePermission("new_invoice_edit")]
    [HttpPut("{id}")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateInvoice(ulong id, [FromForm] NewInvoiceFormRequest form, CancellationToken cancellationToken)
    {
        var request = await ToRequestAsync(form, cancellationToken);
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
        var response = await _newInvoiceService.ApproveInvoiceAsync(id, "ss", request.Remark, request.ApprovedAmount, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_approve_sales")]
    [HttpPost("{id}/approve/sales")]
    public async Task<IActionResult> ApproveSales(ulong id, [FromBody] NewInvoiceApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.ApproveInvoiceAsync(id, "sales", request.Remark, request.ApprovedAmount, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("new_invoice_approve_ho")]
    [HttpPost("{id}/approve/ho")]
    public async Task<IActionResult> ApproveHo(ulong id, [FromBody] NewInvoiceApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _newInvoiceService.ApproveInvoiceAsync(id, "ho", request.Remark, request.ApprovedAmount, CurrentUserId(), cancellationToken);
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

    private async Task<NewInvoiceRequestDto> ToRequestAsync(NewInvoiceFormRequest form, CancellationToken cancellationToken)
    {
        return new NewInvoiceRequestDto
        {
            SecondaryCustomerId = form.SecondaryCustomerId,
            InvoiceNumber = form.InvoiceNumber,
            InvoiceDate = form.InvoiceDate,
            Amount = form.Amount,
            Points = form.Points,
            Attachment = await SaveFileAsync(form.AttachmentFile, cancellationToken) ?? form.Attachment
        };
    }

    private async Task<string?> SaveFileAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return null;
        var root = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "new-invoices");
        Directory.CreateDirectory(root);
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(root, fileName);
        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream, cancellationToken);
        return $"/uploads/new-invoices/{fileName}";
    }
}

public sealed class NewInvoiceFormRequest
{
    [FromForm(Name = "secondary_customer_id")] public ulong SecondaryCustomerId { get; set; }
    [FromForm(Name = "invoice_number")] public string? InvoiceNumber { get; set; }
    [FromForm(Name = "invoice_date")] public DateTime? InvoiceDate { get; set; }
    [FromForm(Name = "amount")] public decimal? Amount { get; set; }
    [FromForm(Name = "points")] public decimal? Points { get; set; }
    [FromForm(Name = "attachment")] public string? Attachment { get; set; }
    [FromForm(Name = "attachment_file")] public IFormFile? AttachmentFile { get; set; }
}
