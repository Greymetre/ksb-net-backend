using Application.DTOs.Orders;
using Application.DTOs.MasterData;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Entities;
using Application.Common;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Services;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetOrdersAsync(OrderFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("orders", await _repository.GetOrdersAsync(filter, cancellationToken));

    public async Task<LaravelApiResponse> GetOrderAsync(ulong id, CancellationToken cancellationToken)
    {
        var order = await _repository.GetOrderAsync(id, cancellationToken) ?? throw NotFound("Order not found");
        var response = LaravelApiResponse.Success("order", order);
        response.Extra["order_details"] = await _repository.GetOrderDetailsAsync(id, cancellationToken);
        return response;
    }

    public async Task<LaravelApiResponse> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("options", await _repository.GetOptionsAsync(actorUserId, cancellationToken));

    public async Task<LaravelApiResponse> GetProductsByFamilyAsync(ulong familyId, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("products", await _repository.GetProductsByFamilyAsync(familyId, cancellationToken));

    public async Task<LaravelApiResponse> CreateOrderAsync(OrderRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireId(request.SellerId, "Dealer / Distributor is required.");
        RequireId(request.ExecutiveId, "Employee is required.");
        RequireValue(request.Type, "Customer Type is required.");

        var type = request.Type!.Trim().ToUpperInvariant();
        if (type != "DISTRIBUTER") RequireId(request.BuyerId, "Customer is required.");
        if (request.OrderDetail.Count == 0) throw BadRequest("At least one order item is required.");

        var rows = request.OrderDetail.Where(x => x.ProductId.HasValue && (x.Quantity ?? 0) > 0).ToArray();
        if (rows.Length == 0) throw BadRequest("At least one valid product row is required.");

        var now = DateTime.Now;
        var order = new Order
        {
            Active = "Y",
            BuyerId = type == "DISTRIBUTER" ? null : request.BuyerId,
            SellerId = request.SellerId,
            ExecutiveId = request.ExecutiveId,
            TotalQty = ToLongQuantity(request.TotalQty ?? rows.Sum(x => x.Quantity ?? 0)),
            ShippedQty = 0,
            OrderDate = request.OrderDate?.Date ?? now.Date,
            TotalGst = request.TotalGst ?? rows.Sum(x => x.TaxAmount ?? 0),
            SubTotal = request.SubTotal ?? rows.Sum(x => x.LineTotal ?? 0),
            GrandTotal = request.GrandTotal ?? rows.Sum(x => x.LineTotal ?? 0),
            OrderTaking = "Web",
            OrderType = type == "DISTRIBUTER" ? "MASTER_DISTRIBUTER" : "SECONDARY_CUSTOMER",
            OrderRemark = request.OrderRemark,
            CreatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddOrderAsync(order, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        order.OrderNo = $"{now:yyyy}-{order.SellerId ?? 0}-{order.BuyerId ?? 0}-{order.Id}";
        order.UpdatedAt = now;

        var details = rows.Select(row =>
        {
            var lineTotal = row.LineTotal ?? ((row.Quantity ?? 0) * (row.Mrp ?? 0));
            var tax = row.TaxAmount ?? 0;
            return new OrderDetail
            {
                Active = "Y",
                OrderId = order.Id,
                ProductId = row.ProductId,
                ProductDetailId = row.ProductDetail,
                Quantity = ToLongQuantity(row.Quantity),
                ShippedQty = 0,
                Price = row.Mrp ?? 0,
                Gst = row.Gst ?? 0,
                TaxAmount = tax,
                LineTotal = lineTotal,
                GstAmount = lineTotal + tax,
                SubcategoryId = row.SubcategoryId,
                CategoryId = row.CategoryId,
                CreatedAt = now,
                UpdatedAt = now
            };
        }).ToArray();

        await _repository.AddOrderDetailsAsync(details, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return LaravelApiResponse.Success("order", await _repository.GetOrderAsync(order.Id, cancellationToken), "Order Created Successfully");
    }

    public async Task<MasterDataFileDto> ExportOrdersAsync(OrderFilterDto filter, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetOrderExportRowsAsync(filter, cancellationToken);
        var headings = new[]
        {
            "Order Date", "Order No", "Employee Name", "Reporting Manager", "Designation", "Branch",
            "Retailer Name", "Distributor Name", "Distributor Code", "Product Code", "Product Name",
            "Quantity", "Rate", "Total Order Value", "Employee Code", "Retailer ID", "Distributor ID",
            "Order Remark", "Segment", "Family", "id", "Zone"
        };

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Orders");
        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 9;

        for (var column = 0; column < headings.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = headings[column];
            worksheet.Cell(1, column + 1).Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.OrderDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            worksheet.Cell(rowNumber, 2).Value = row.OrderNo;
            worksheet.Cell(rowNumber, 3).Value = row.EmployeeName ?? string.Empty;
            worksheet.Cell(rowNumber, 4).Value = row.ReportingManager ?? string.Empty;
            worksheet.Cell(rowNumber, 5).Value = row.Designation ?? string.Empty;
            worksheet.Cell(rowNumber, 6).Value = row.Branch ?? string.Empty;
            worksheet.Cell(rowNumber, 7).Value = row.RetailerName ?? string.Empty;
            worksheet.Cell(rowNumber, 8).Value = row.DistributorName ?? string.Empty;
            worksheet.Cell(rowNumber, 9).Value = row.DistributorCode ?? string.Empty;
            worksheet.Cell(rowNumber, 10).Value = row.ProductCode ?? string.Empty;
            worksheet.Cell(rowNumber, 11).Value = row.ProductName ?? string.Empty;
            worksheet.Cell(rowNumber, 12).Value = row.Quantity;
            worksheet.Cell(rowNumber, 13).Value = row.Rate;
            worksheet.Cell(rowNumber, 14).Value = row.TotalOrderValue;
            worksheet.Cell(rowNumber, 15).Value = row.EmployeeCode ?? string.Empty;
            worksheet.Cell(rowNumber, 16).Value = row.RetailerId?.ToString() ?? string.Empty;
            worksheet.Cell(rowNumber, 17).Value = row.DistributorId?.ToString() ?? string.Empty;
            worksheet.Cell(rowNumber, 18).Value = row.OrderRemark ?? string.Empty;
            worksheet.Cell(rowNumber, 19).Value = row.Segment ?? string.Empty;
            worksheet.Cell(rowNumber, 20).Value = row.Family ?? string.Empty;
            worksheet.Cell(rowNumber, 21).Value = row.DetailId;
            worksheet.Cell(rowNumber, 22).Value = row.Zone ?? string.Empty;
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new MasterDataFileDto { FileName = "orders.xlsx", Content = stream.ToArray() };
    }

    private static void RequireValue(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw BadRequest(message);
    }

    private static void RequireId(ulong? value, string message)
    {
        if (value is null or 0) throw BadRequest(message);
    }

    private static long ToLongQuantity(decimal? value) =>
        Convert.ToInt64(Math.Round(value ?? 0, 0, MidpointRounding.AwayFromZero));

    private static LaravelHttpException BadRequest(string message) =>
        new(LaravelStatusCodes.BadRequest, message);

    private static LaravelHttpException NotFound(string message) =>
        new(LaravelStatusCodes.NotFound, message);
}
