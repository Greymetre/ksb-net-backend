using System.Security.Claims;
using Api.Filters;
using Application.DTOs.Products;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(IProductService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    [RequirePermission("category_access")]
    [HttpGet("segments")]
    public async Task<IActionResult> Segments([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _service.GetSegmentsAsync(search, includeInactive: true, cancellationToken));

    [HttpGet("getsegments")]
    public async Task<IActionResult> SegmentOptions([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _service.GetSegmentsAsync(search, includeInactive: false, cancellationToken));

    [RequirePermission("category_download")]
    [HttpGet("segments/export")]
    public async Task<IActionResult> ExportSegments([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var file = await _service.ExportSegmentsAsync(search, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("category_template")]
    [HttpGet("segments/template")]
    public async Task<IActionResult> SegmentTemplate(CancellationToken cancellationToken)
    {
        var file = await _service.GetSegmentTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("category_upload")]
    [HttpPost("segments/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadSegments(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        return Ok(await _service.UploadSegmentsAsync(stream, CurrentUserId(), cancellationToken));
    }

    [RequirePermission("category_create")]
    [HttpPost("segments")]
    public async Task<IActionResult> CreateSegment([FromBody] ProductSegmentRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _service.CreateSegmentAsync(request, CurrentUserId(), cancellationToken));

    [RequirePermission("category_edit")]
    [HttpPut("segments/{id}")]
    [HttpPatch("segments/{id}")]
    public async Task<IActionResult> UpdateSegment(ulong id, [FromBody] ProductSegmentRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateSegmentAsync(id, request, CurrentUserId(), cancellationToken));

    [RequirePermission("category_active", "category_edit")]
    [HttpPatch("segments/{id}/status")]
    public async Task<IActionResult> SegmentStatus(ulong id, [FromBody] ActiveRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.SetSegmentActiveAsync(id, request.Active, CurrentUserId(), cancellationToken));

    [RequirePermission("category_delete")]
    [HttpDelete("segments/{id}")]
    public async Task<IActionResult> DeleteSegment(ulong id, CancellationToken cancellationToken) =>
        Ok(await _service.DeleteSegmentAsync(id, CurrentUserId(), cancellationToken));

    [RequirePermission("subcategory_access")]
    [HttpGet("families")]
    public async Task<IActionResult> Families([FromQuery(Name = "segment_id")] ulong? segmentId, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _service.GetFamiliesAsync(segmentId, search, includeInactive: true, cancellationToken));

    [HttpGet("getfamilies")]
    public async Task<IActionResult> FamilyOptions([FromQuery(Name = "segment_id")] ulong? segmentId, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _service.GetFamiliesAsync(segmentId, search, includeInactive: false, cancellationToken));

    [RequirePermission("subcategory_download")]
    [HttpGet("families/export")]
    public async Task<IActionResult> ExportFamilies([FromQuery(Name = "segment_id")] ulong? segmentId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var file = await _service.ExportFamiliesAsync(segmentId, search, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("subcategory_template")]
    [HttpGet("families/template")]
    public async Task<IActionResult> FamilyTemplate(CancellationToken cancellationToken)
    {
        var file = await _service.GetFamilyTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("subcategory_upload")]
    [HttpPost("families/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFamilies(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        return Ok(await _service.UploadFamiliesAsync(stream, CurrentUserId(), cancellationToken));
    }

    [RequirePermission("subcategory_create")]
    [HttpPost("families")]
    public async Task<IActionResult> CreateFamily([FromBody] ProductFamilyRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _service.CreateFamilyAsync(request, CurrentUserId(), cancellationToken));

    [RequirePermission("subcategory_edit")]
    [HttpPut("families/{id}")]
    [HttpPatch("families/{id}")]
    public async Task<IActionResult> UpdateFamily(ulong id, [FromBody] ProductFamilyRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateFamilyAsync(id, request, CurrentUserId(), cancellationToken));

    [RequirePermission("subcategory_active", "subcategory_edit")]
    [HttpPatch("families/{id}/status")]
    public async Task<IActionResult> FamilyStatus(ulong id, [FromBody] ActiveRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.SetFamilyActiveAsync(id, request.Active, CurrentUserId(), cancellationToken));

    [RequirePermission("subcategory_delete")]
    [HttpDelete("families/{id}")]
    public async Task<IActionResult> DeleteFamily(ulong id, CancellationToken cancellationToken) =>
        Ok(await _service.DeleteFamilyAsync(id, CurrentUserId(), cancellationToken));

    [RequirePermission("product_access")]
    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery(Name = "segment_id")] ulong? segmentId, [FromQuery(Name = "family_id")] ulong? familyId, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _service.GetProductsAsync(segmentId, familyId, search, includeInactive: true, cancellationToken));

    [RequirePermission("product_download")]
    [HttpGet("products/export")]
    public async Task<IActionResult> ExportProducts([FromQuery(Name = "segment_id")] ulong? segmentId, [FromQuery(Name = "family_id")] ulong? familyId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var file = await _service.ExportProductsAsync(segmentId, familyId, search, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("product_template")]
    [HttpGet("products/template")]
    public async Task<IActionResult> ProductTemplate(CancellationToken cancellationToken)
    {
        var file = await _service.GetProductTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("product_upload")]
    [HttpPost("products/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProducts(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        return Ok(await _service.UploadProductsAsync(stream, CurrentUserId(), cancellationToken));
    }

    [RequirePermission("product_create")]
    [HttpPost("products")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateProduct([FromForm] ProductFormRequest form, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _service.CreateProductAsync(await ToProductRequestAsync(form, cancellationToken), CurrentUserId(), cancellationToken));

    [RequirePermission("product_edit")]
    [HttpPut("products/{id}")]
    [HttpPatch("products/{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProduct(ulong id, [FromForm] ProductFormRequest form, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateProductAsync(id, await ToProductRequestAsync(form, cancellationToken), CurrentUserId(), cancellationToken));

    [RequirePermission("product_active", "product_edit")]
    [HttpPatch("products/{id}/status")]
    public async Task<IActionResult> ProductStatus(ulong id, [FromBody] ActiveRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.SetProductActiveAsync(id, request.Active, CurrentUserId(), cancellationToken));

    [RequirePermission("product_delete")]
    [HttpDelete("products/{id}")]
    public async Task<IActionResult> DeleteProduct(ulong id, CancellationToken cancellationToken) =>
        Ok(await _service.DeleteProductAsync(id, CurrentUserId(), cancellationToken));

    private async Task<ProductRequestDto> ToProductRequestAsync(ProductFormRequest form, CancellationToken cancellationToken)
    {
        var attachment = await SaveFileAsync(form.AttachmentFile, cancellationToken);
        return new ProductRequestDto
        {
            Active = form.Active,
            SegmentId = form.SegmentId,
            FamilyId = form.FamilyId,
            PartNo = form.PartNo,
            ProductName = form.ProductName,
            Mrp = form.Mrp,
            Attachment = attachment ?? form.Attachment
        };
    }

    private async Task<string?> SaveFileAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return null;
        var root = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "products");
        Directory.CreateDirectory(root);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(root, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);
        return $"/uploads/products/{fileName}";
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }
}

public sealed class ActiveRequest
{
    public string? Active { get; set; }
}

public sealed class ProductFormRequest
{
    [FromForm(Name = "active")] public string? Active { get; set; }
    [FromForm(Name = "segment_id")] public ulong? SegmentId { get; set; }
    [FromForm(Name = "family_id")] public ulong? FamilyId { get; set; }
    [FromForm(Name = "part_no")] public string? PartNo { get; set; }
    [FromForm(Name = "product_name")] public string? ProductName { get; set; }
    [FromForm(Name = "mrp")] public decimal? Mrp { get; set; }
    [FromForm(Name = "attachment")] public string? Attachment { get; set; }
    [FromForm(Name = "attachment_file")] public IFormFile? AttachmentFile { get; set; }
}
