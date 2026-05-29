using System.Security.Claims;
using System.Text.Json;
using Api.Filters;
using Application.Common;
using Application.DTOs.Customers;
using Application.DTOs.MasterData;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ICustomerService _customerService;
    private readonly IWebHostEnvironment _environment;

    public CustomersController(ICustomerService customerService, IWebHostEnvironment environment)
    {
        _customerService = customerService;
        _environment = environment;
    }

    [RequirePermission("customer_access")]
    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] CustomerListFilterDto filter,
        [FromQuery(Name = "customer_type")] ulong? customerType,
        [FromQuery(Name = "state_id")] ulong? stateId,
        [FromQuery(Name = "city_id")] ulong? cityId,
        [FromQuery(Name = "pincode_id")] ulong? pincodeId,
        CancellationToken cancellationToken)
    {
        ApplyQueryAliases(filter, customerType, stateId, cityId, pincodeId);
        var response = await _customerService.GetCustomersAsync(filter, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("customer_download", "customers_report")]
    [HttpGet("export")]
    public async Task<IActionResult> ExportCustomers(
        [FromQuery] CustomerListFilterDto filter,
        [FromQuery(Name = "customer_type")] ulong? customerType,
        [FromQuery(Name = "state_id")] ulong? stateId,
        [FromQuery(Name = "city_id")] ulong? cityId,
        [FromQuery(Name = "pincode_id")] ulong? pincodeId,
        CancellationToken cancellationToken)
    {
        ApplyQueryAliases(filter, customerType, stateId, cityId, pincodeId);
        var file = await _customerService.ExportCustomersAsync(filter, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("customer_template", "customer_upload")]
    [HttpGet("template")]
    public async Task<IActionResult> CustomerTemplate(CancellationToken cancellationToken)
    {
        MasterDataFileDto file = await _customerService.GetCustomerTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [RequirePermission("customer_upload")]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCustomers(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        var response = await _customerService.UploadCustomersAsync(stream, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("customer_access")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(ulong id, CancellationToken cancellationToken)
    {
        var response = await _customerService.GetCustomerAsync(id, cancellationToken);
        return Ok(response);
    }

    [RequirePermission("customer_create")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateCustomer([FromForm] CustomerFormRequestDto form, CancellationToken cancellationToken)
    {
        var request = await ToCustomerRequestAsync(form, cancellationToken);
        var response = await _customerService.CreateCustomerAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [RequirePermission("customer_edit")]
    [HttpPut("{id}")]
    [HttpPatch("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateCustomer(ulong id, [FromForm] CustomerFormRequestDto form, CancellationToken cancellationToken)
    {
        var request = await ToCustomerRequestAsync(form, cancellationToken);
        var response = await _customerService.UpdateCustomerAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("customer_edit")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetCustomerActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _customerService.SetCustomerActiveAsync(id, request.Active, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("customer_kyc_access", "customer_edit")]
    [HttpPost("{id}/kyc/{documentKey}/approve")]
    public async Task<IActionResult> ApproveKycDocument(ulong id, string documentKey, [FromBody] CustomerKycApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _customerService.ApproveKycDocumentAsync(id, documentKey, request.Remark, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("customer_kyc_access", "customer_edit")]
    [HttpPost("{id}/kyc/{documentKey}/reject")]
    public async Task<IActionResult> RejectKycDocument(ulong id, string documentKey, [FromBody] CustomerKycApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _customerService.RejectKycDocumentAsync(id, documentKey, request.Remark, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [RequirePermission("customer_delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(ulong id, CancellationToken cancellationToken)
    {
        var response = await _customerService.DeleteCustomerAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }

    private static void ApplyQueryAliases(CustomerListFilterDto filter, ulong? customerType, ulong? stateId, ulong? cityId, ulong? pincodeId)
    {
        filter.CustomerType ??= customerType;
        filter.StateId ??= stateId;
        filter.CityId ??= cityId;
        filter.PincodeId ??= pincodeId;
    }

    private async Task<CustomerRequestDto> ToCustomerRequestAsync(CustomerFormRequestDto form, CancellationToken cancellationToken)
    {
        var fields = ReadCustomFields(form.CustomFields);
        var shopImage = await SaveFileAsync(form.ShopImage, "shop-images", cancellationToken);
        var profileImage = await SaveFileAsync(form.ProfileImage, "profile-images", cancellationToken);

        SetField(fields, "shop_image", shopImage);
        SetField(fields, "profile_image", profileImage);
        SetKycFileField(fields, "gst", "gst_attachment", await SaveFileAsync(form.GstAttachment, "gst-attachments", cancellationToken));
        SetKycFileField(fields, "pan", "pan_attachment", await SaveFileAsync(form.PanAttachment, "pan-attachments", cancellationToken));
        SetKycFileField(fields, "aadhar", "aadhar_attachment", await SaveFileAsync(form.AadharAttachment, "aadhar-attachments", cancellationToken));
        SetKycFileField(fields, "bank", "bank_proof", await SaveFileAsync(form.BankProof, "bank-proofs", cancellationToken));
        SetField(fields, "shop_photo", await SaveFileAsync(form.ShopPhoto, "shop-photos", cancellationToken));
        SetField(fields, "mou_file", await SaveFileAsync(form.MouFile, "mou-files", cancellationToken));

        if (form.Documents.Count > 0)
        {
            var documents = new List<string>();
            foreach (var document in form.Documents)
            {
                var path = await SaveFileAsync(document, "documents", cancellationToken);
                if (!string.IsNullOrWhiteSpace(path)) documents.Add(path);
            }

            if (documents.Count > 0) fields["documents"] = JsonSerializer.Serialize(documents, JsonOptions);
        }

        return new CustomerRequestDto
        {
            Active = form.Active,
            Name = form.Name,
            Mobile = form.Mobile,
            ContactNumber = form.ContactNumber,
            Email = form.Email,
            CustomerCode = form.CustomerCode,
            CustomerType = form.CustomerType,
            ParentId = form.ParentId,
            SapCode = form.SapCode,
            ProfileImage = profileImage,
            ShopImage = shopImage,
            CustomFields = fields
        };
    }

    private static Dictionary<string, string?> ReadCustomFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<string?> SaveFileAsync(IFormFile? file, string folder, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return null;
        if (IsRetailerImageFolder(folder) && !IsImageFile(file))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Retailer KYC attachments must be image files only.");
        }

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var uploadRoot = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "customers", folder);
        Directory.CreateDirectory(uploadRoot);
        var fullPath = Path.Combine(uploadRoot, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);
        return $"/uploads/customers/{folder}/{fileName}";
    }

    private static bool IsRetailerImageFolder(string folder) =>
        folder is "gst-attachments" or "pan-attachments" or "aadhar-attachments" or "bank-proofs" or "shop-photos";

    private static bool IsImageFile(IFormFile file)
    {
        if (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return true;
        return Path.GetExtension(file.FileName).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp";
    }

    private static void SetField(IDictionary<string, string?> fields, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) fields[key] = value;
    }

    private static void SetKycFileField(IDictionary<string, string?> fields, string documentKey, string attachmentKey, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        fields[attachmentKey] = value;
        var prefix = $"{documentKey}_kyc";
        fields[$"{prefix}_status"] = "pending";
        fields.Remove($"{prefix}_remark");
        fields.Remove($"{prefix}_action_by");
        fields.Remove($"{prefix}_action_by_name");
        fields.Remove($"{prefix}_action_at");
    }
}

public sealed class CustomerFormRequestDto
{
    [FromForm(Name = "active")]
    public string? Active { get; set; }

    [FromForm(Name = "name")]
    public string? Name { get; set; }

    [FromForm(Name = "mobile")]
    public string? Mobile { get; set; }

    [FromForm(Name = "contact_number")]
    public string? ContactNumber { get; set; }

    [FromForm(Name = "email")]
    public string? Email { get; set; }

    [FromForm(Name = "customer_code")]
    public string? CustomerCode { get; set; }

    [FromForm(Name = "customer_type")]
    public ulong? CustomerType { get; set; }

    [FromForm(Name = "parent_id")]
    public ulong? ParentId { get; set; }

    [FromForm(Name = "sap_code")]
    public string? SapCode { get; set; }

    [FromForm(Name = "custom_fields")]
    public string? CustomFields { get; set; }

    [FromForm(Name = "shop_image")]
    public IFormFile? ShopImage { get; set; }

    [FromForm(Name = "profile_image")]
    public IFormFile? ProfileImage { get; set; }

    [FromForm(Name = "documents")]
    public List<IFormFile> Documents { get; set; } = [];

    [FromForm(Name = "mou_file")]
    public IFormFile? MouFile { get; set; }

    [FromForm(Name = "gst_attachment")]
    public IFormFile? GstAttachment { get; set; }

    [FromForm(Name = "pan_attachment")]
    public IFormFile? PanAttachment { get; set; }

    [FromForm(Name = "aadhar_attachment")]
    public IFormFile? AadharAttachment { get; set; }

    [FromForm(Name = "bank_proof")]
    public IFormFile? BankProof { get; set; }

    [FromForm(Name = "shop_photo")]
    public IFormFile? ShopPhoto { get; set; }
}
