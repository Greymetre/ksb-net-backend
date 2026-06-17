using Application.Common;
using Application.DTOs.MasterData;
using Application.DTOs.NewInvoices;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Services;

public sealed class NewInvoiceService : INewInvoiceService
{
    private readonly INewInvoiceRepository _repository;

    public NewInvoiceService(INewInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetInvoicesAsync(NewInvoiceFilterDto filter, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var invoices = await _repository.GetInvoicesAsync(filter, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("new_invoices", new
        {
            invoices,
            summary = BuildSummary(invoices),
            approval_statuses = ApprovalStatuses()
        });
    }

    public async Task<MasterDataFileDto> ExportInvoicesAsync(NewInvoiceFilterDto filter, ulong? actorUserId, string baseUrl, CancellationToken cancellationToken)
    {
        var invoices = await _repository.GetInvoicesAsync(filter, actorUserId, cancellationToken);
        return CreateWorkbook("new-invoices.xlsx",
            ["id", "retailer_id", "customer", "shop", "mobile", "assigned_distributor", "assigned_employee", "city", "zone", "invoice_date", "invoice_number", "amount", "ss_approved_amount", "ss_remark", "sales_approved_amount", "sales_remark", "ho_approved_amount", "ho_remark", "scheme_name", "points", "scheme_hint", "attachment", "status", "created_by", "created_at"],
            invoices.Select(x => new object?[]
            {
                x.Id,
                x.SecondaryCustomerId,
                x.CustomerName,
                x.ShopName,
                x.MobileNumber,
                x.AssignedDistributorName,
                x.AssignedEmployeeName,
                x.CityName,
                x.ZoneName,
                x.InvoiceDate,
                x.InvoiceNumber,
                x.Amount,
                x.SsApprovedAmount,
                x.SsApprovalRemark,
                x.SalesApprovedAmount,
                x.SalesApprovalRemark,
                x.HoApprovedAmount,
                x.HoApprovalRemark,
                x.SchemeName,
                x.SchemePoints,
                x.SchemeHintMessage,
                ExportHyperlinkFactory.Attachment(x.Attachment, baseUrl),
                x.ApprovalStatusLabel,
                x.CreatedBy,
                x.CreatedAt
            }));
    }

    public async Task<LaravelApiResponse> GetInvoiceAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("new_invoice", await GetOrThrowAsync(id, actorUserId, cancellationToken));

    public async Task<LaravelApiResponse> GetRetailersAsync(string? search, ulong? actorUserId, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("retailers", await _repository.GetRetailerOptionsAsync(search, actorUserId, cancellationToken));

    public async Task<LaravelApiResponse> CreateInvoiceAsync(NewInvoiceRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) throw Http(LaravelStatusCodes.Unauthorized, "Unauthenticated.");
        await ValidateRequestAsync(request, null, actorUserId, cancellationToken);

        var now = DateTime.UtcNow;
        var invoice = new NewInvoice
        {
            SecondaryCustomerId = request.SecondaryCustomerId,
            InvoiceNumber = request.InvoiceNumber!.Trim(),
            InvoiceDate = request.InvoiceDate!.Value.Date,
            Amount = request.Amount!.Value,
            Points = 0,
            Attachment = NormalizeText(request.Attachment),
            ApprovalStatus = NewInvoice.StatusPending,
            CreatedBy = actorUserId.Value,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _repository.CreateInvoiceAsync(invoice, cancellationToken);
        var entity = await _repository.FindInvoiceEntityAsync(created.Id, cancellationToken);
        if (entity is not null)
        {
            await _repository.SaveInvoiceAsync(entity, "generated", null, NewInvoice.StatusPending, actorUserId.Value, null, null, cancellationToken);
            created = await _repository.GetInvoiceAsync(created.Id, actorUserId, cancellationToken) ?? created;
        }

        return LaravelApiResponse.Success("new_invoice", created, "Invoice created successfully");
    }

    public async Task<LaravelApiResponse> UpdateInvoiceAsync(ulong id, NewInvoiceRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) throw Http(LaravelStatusCodes.Unauthorized, "Unauthenticated.");
        var invoice = await FindOrThrowAsync(id, cancellationToken);
        if (invoice.ApprovalStatus != NewInvoice.StatusPending)
        {
            throw Http(403, "Approved or rejected invoices cannot be edited.");
        }

        await ValidateRequestAsync(request, id, actorUserId, cancellationToken);

        invoice.SecondaryCustomerId = request.SecondaryCustomerId;
        invoice.InvoiceNumber = request.InvoiceNumber!.Trim();
        invoice.InvoiceDate = request.InvoiceDate!.Value.Date;
        invoice.Amount = request.Amount!.Value;
        invoice.Points = 0;
        if (request.Attachment is not null) invoice.Attachment = NormalizeText(request.Attachment);

        var updated = await _repository.SaveInvoiceAsync(invoice, "updated", invoice.ApprovalStatus, invoice.ApprovalStatus, actorUserId.Value, null, null, cancellationToken);
        return LaravelApiResponse.Success("new_invoice", updated, "Invoice updated successfully");
    }

    public async Task<LaravelApiResponse> DeleteInvoiceAsync(ulong id, CancellationToken cancellationToken)
    {
        var invoice = await FindOrThrowAsync(id, cancellationToken);
        await _repository.DeleteInvoiceAsync(invoice, cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Invoice deleted successfully");
    }

    public async Task<LaravelApiResponse> ApproveInvoiceAsync(ulong id, string level, string? remark, decimal? approvedAmount, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) throw Http(LaravelStatusCodes.Unauthorized, "Unauthenticated.");
        var invoice = await FindOrThrowAsync(id, cancellationToken);
        var fromStatus = invoice.ApprovalStatus;
        var (toStatus, statusType) = level.ToLowerInvariant() switch
        {
            "ss" => (NewInvoice.StatusApprovedSs, "approved by ss"),
            "sales" => (NewInvoice.StatusApprovedSales, "approved by sales"),
            "ho" => (NewInvoice.StatusApprovedHo, "approved by ho"),
            _ => throw Http(LaravelStatusCodes.NotFound, "Approval level not found.")
        };

        if (!CanMoveToStatus(fromStatus, toStatus))
        {
            throw Http(LaravelStatusCodes.NoContentLikeValidation, "Invoice cannot move to the selected approval status.");
        }

        var finalApprovedAmount = approvedAmount ?? invoice.Amount;
        if (finalApprovedAmount <= 0)
        {
            throw Http(LaravelStatusCodes.NoContentLikeValidation, new { approved_amount = new[] { "Approved invoice amount must be greater than 0." } });
        }

        invoice.ApprovalStatus = toStatus;
        if (toStatus != NewInvoice.StatusApprovedHo) invoice.Points = 0;
        invoice.ApprovalRemark = NormalizeText(remark);
        var now = DateTime.UtcNow;
        if (toStatus == NewInvoice.StatusApprovedSs)
        {
            invoice.ApprovedSsBy = actorUserId;
            invoice.ApprovedSsAt = now;
        }
        if (toStatus == NewInvoice.StatusApprovedSales)
        {
            invoice.ApprovedSalesBy = actorUserId;
            invoice.ApprovedSalesAt = now;
        }
        if (toStatus == NewInvoice.StatusApprovedHo)
        {
            invoice.ApprovedHoBy = actorUserId;
            invoice.ApprovedHoAt = now;
        }

        var updated = await _repository.SaveInvoiceAsync(invoice, statusType, fromStatus, toStatus, actorUserId.Value, remark, finalApprovedAmount, cancellationToken);
        return LaravelApiResponse.Success("new_invoice", updated, "Invoice approved successfully.");
    }

    public async Task<LaravelApiResponse> RejectInvoiceAsync(ulong id, string? remark, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) throw Http(LaravelStatusCodes.Unauthorized, "Unauthenticated.");
        if (string.IsNullOrWhiteSpace(remark)) throw Http(LaravelStatusCodes.NoContentLikeValidation, "Remark is required.");

        var invoice = await FindOrThrowAsync(id, cancellationToken);
        if (!CanMoveToStatus(invoice.ApprovalStatus, NewInvoice.StatusRejected))
        {
            throw Http(LaravelStatusCodes.NoContentLikeValidation, "Invoice cannot be rejected from the current status.");
        }

        var fromStatus = invoice.ApprovalStatus;
        invoice.ApprovalStatus = NewInvoice.StatusRejected;
        invoice.Points = 0;
        invoice.ApprovalRemark = remark.Trim();
        invoice.RejectedBy = actorUserId;
        invoice.RejectedAt = DateTime.UtcNow;

        var updated = await _repository.SaveInvoiceAsync(invoice, "rejected", fromStatus, NewInvoice.StatusRejected, actorUserId.Value, remark, null, cancellationToken);
        return LaravelApiResponse.Success("new_invoice", updated, "Invoice rejected successfully.");
    }

    private async Task ValidateRequestAsync(NewInvoiceRequestDto request, ulong? exceptId, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.SecondaryCustomerId == 0) errors["secondary_customer_id"] = ["Customer is required."];
        if (string.IsNullOrWhiteSpace(request.InvoiceNumber)) errors["invoice_number"] = ["Invoice number is required."];
        if (!request.InvoiceDate.HasValue) errors["invoice_date"] = ["Invoice date is required."];
        if (!request.Amount.HasValue || request.Amount.Value <= 0) errors["amount"] = ["Amount must be greater than 0."];
        if (request.Points.HasValue && request.Points.Value < 0) errors["points"] = ["Points cannot be negative."];

        if (errors.Count > 0) throw Http(LaravelStatusCodes.NoContentLikeValidation, errors);

        var retailer = await _repository.GetRetailerAsync(request.SecondaryCustomerId, actorUserId, cancellationToken);
        if (retailer is null) throw Http(LaravelStatusCodes.NoContentLikeValidation, new { secondary_customer_id = new[] { "Only active retailer customers can be selected." } });

        if (await _repository.InvoiceNumberExistsAsync(request.InvoiceNumber!.Trim(), exceptId, cancellationToken))
        {
            throw Http(LaravelStatusCodes.NoContentLikeValidation, new { invoice_number = new[] { "This invoice number already exists." } });
        }
    }

    private async Task<NewInvoiceDto> GetOrThrowAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken) =>
        await _repository.GetInvoiceAsync(id, actorUserId, cancellationToken) ?? throw Http(LaravelStatusCodes.NotFound, "Invoice not found");

    private async Task<NewInvoice> FindOrThrowAsync(ulong id, CancellationToken cancellationToken) =>
        await _repository.FindInvoiceEntityAsync(id, cancellationToken) ?? throw Http(LaravelStatusCodes.NotFound, "Invoice not found");

    private static NewInvoiceSummaryDto BuildSummary(IReadOnlyCollection<NewInvoiceDto> invoices)
    {
        var distinctInvoices = invoices
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();

        return new NewInvoiceSummaryDto
        {
            TotalInvoices = distinctInvoices.Count,
            TotalRetailers = distinctInvoices.Select(x => x.SecondaryCustomerId).Distinct().Count(),
            ApprovedSs = distinctInvoices.Count(x => x.ApprovalStatus == NewInvoice.StatusApprovedSs),
            ApprovedSales = distinctInvoices.Count(x => x.ApprovalStatus == NewInvoice.StatusApprovedSales),
            ApprovedHo = distinctInvoices.Count(x => x.ApprovalStatus == NewInvoice.StatusApprovedHo),
            Pending = distinctInvoices.Count(x => x.ApprovalStatus == NewInvoice.StatusPending),
            Rejected = distinctInvoices.Count(x => x.ApprovalStatus == NewInvoice.StatusRejected),
            TotalPoints = invoices.Sum(x => x.SchemePoints),
            TotalAmount = distinctInvoices.Sum(x => x.Amount)
        };
    }

    private static Dictionary<int, string> ApprovalStatuses() => new()
    {
        [NewInvoice.StatusPending] = "Pending",
        [NewInvoice.StatusApprovedSs] = "Approved By SS",
        [NewInvoice.StatusApprovedSales] = "Approved By Sales",
        [NewInvoice.StatusApprovedHo] = "Approved By HO",
        [NewInvoice.StatusRejected] = "Rejected"
    };

    private static bool CanMoveToStatus(int current, int next)
    {
        if (current is NewInvoice.StatusRejected or NewInvoice.StatusApprovedHo) return false;
        return next switch
        {
            NewInvoice.StatusApprovedSs => current == NewInvoice.StatusPending,
            NewInvoice.StatusApprovedSales => current == NewInvoice.StatusApprovedSs,
            NewInvoice.StatusApprovedHo => current == NewInvoice.StatusApprovedSales,
            NewInvoice.StatusRejected => true,
            _ => false
        };
    }

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }

    private static MasterDataFileDto CreateWorkbook(string fileName, string[] headings, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");
        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 9;
        for (var column = 0; column < headings.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = TitleCase(headings[column].Replace("_", " "));
            worksheet.Cell(1, column + 1).Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < row.Length; column++)
            {
                SetCellValue(worksheet.Cell(rowNumber, column + 1), row[column]);
            }

            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new MasterDataFileDto { FileName = fileName, Content = stream.ToArray() };
    }

    private static string TitleCase(string value) =>
        System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());

    private static void SetCellValue(IXLCell cell, object? value)
    {
        if (value is ExportHyperlink link)
        {
            cell.Value = link.Text;
            cell.SetHyperlink(new XLHyperlink(new Uri(link.Url)));
            return;
        }

        cell.Value = XLCellValue.FromObject(value);
    }

    private static LaravelHttpException Http(int statusCode, object message) => new(statusCode, message);
}
