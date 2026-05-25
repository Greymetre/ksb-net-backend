using Api.Filters;
using Application.DTOs.MasterData;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class MasterDataController : ControllerBase
{
    private readonly IMasterDataService _masterDataService;

    public MasterDataController(IMasterDataService masterDataService)
    {
        _masterDataService = masterDataService;
    }

    [HttpGet("getcountry")]
    public async Task<IActionResult> GetCountry([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetCountriesAsync(search, cancellationToken);
        return Ok(response);
    }

    [HttpGet("getstate")]
    public async Task<IActionResult> GetState([FromQuery(Name = "country_id")] ulong? countryId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetStatesAsync(countryId, search, cancellationToken);
        return Ok(response);
    }

    [HttpGet("getdistrict")]
    public async Task<IActionResult> GetDistrict([FromQuery(Name = "state_id")] ulong? stateId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetDistrictsAsync(stateId, search, cancellationToken);
        return Ok(response);
    }

    [HttpGet("getcity")]
    public async Task<IActionResult> GetCity([FromQuery(Name = "state_id")] ulong? stateId, [FromQuery(Name = "district_id")] ulong? districtId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetCitiesAsync(stateId, districtId, search, cancellationToken);
        return Ok(response);
    }

    [HttpGet("getpincode")]
    public async Task<IActionResult> GetPincode([FromQuery(Name = "city_id")] ulong? cityId, [FromQuery] string? pincode, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetPincodesAsync(cityId, pincode, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("country_access")]
    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetCountriesAsync(search, cancellationToken, includeInactive: true);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("state_access")]
    [HttpGet("states")]
    public async Task<IActionResult> GetStates([FromQuery(Name = "country_id")] ulong? countryId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetStatesAsync(countryId, search, cancellationToken, includeInactive: true);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("district_access")]
    [HttpGet("districts")]
    public async Task<IActionResult> GetDistricts([FromQuery(Name = "state_id")] ulong? stateId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetDistrictsAsync(stateId, search, cancellationToken, includeInactive: true);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("city_access")]
    [HttpGet("cities")]
    public async Task<IActionResult> GetCities([FromQuery(Name = "state_id")] ulong? stateId, [FromQuery(Name = "district_id")] ulong? districtId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetCitiesAsync(stateId, districtId, search, cancellationToken, includeInactive: true);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("pincode_access")]
    [HttpGet("pincodes")]
    public async Task<IActionResult> GetPincodes([FromQuery(Name = "city_id")] ulong? cityId, [FromQuery] string? pincode, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetPincodesAsync(cityId, pincode, cancellationToken, includeInactive: true);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("country_download")]
    [HttpGet("country-download")]
    [HttpGet("countries/export")]
    public async Task<IActionResult> ExportCountries(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.ExportCountriesAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("country_template")]
    [HttpGet("country-template")]
    [HttpGet("countries/template")]
    public async Task<IActionResult> CountryTemplate(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.GetCountryTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("country_upload")]
    [HttpPost("country-upload")]
    [HttpPost("countries/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCountries(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        var response = await _masterDataService.UploadCountriesAsync(stream, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("state_download")]
    [HttpGet("state-download")]
    [HttpGet("states/export")]
    public async Task<IActionResult> ExportStates(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.ExportStatesAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("state_template")]
    [HttpGet("state-template")]
    [HttpGet("states/template")]
    public async Task<IActionResult> StateTemplate(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.GetStateTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("state_upload")]
    [HttpPost("state-upload")]
    [HttpPost("states/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadStates(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        var response = await _masterDataService.UploadStatesAsync(stream, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("district_download")]
    [HttpGet("district-download")]
    [HttpGet("districts/export")]
    public async Task<IActionResult> ExportDistricts(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.ExportDistrictsAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("district_template")]
    [HttpGet("district-template")]
    [HttpGet("districts/template")]
    public async Task<IActionResult> DistrictTemplate(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.GetDistrictTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("district_upload")]
    [HttpPost("district-upload")]
    [HttpPost("districts/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDistricts(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        var response = await _masterDataService.UploadDistrictsAsync(stream, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("city_download")]
    [HttpGet("city-download")]
    [HttpGet("cities/export")]
    public async Task<IActionResult> ExportCities(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.ExportCitiesAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("city_template")]
    [HttpGet("city-template")]
    [HttpGet("cities/template")]
    public async Task<IActionResult> CityTemplate(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.GetCityTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("city_upload")]
    [HttpPost("city-upload")]
    [HttpPost("cities/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCities(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        var response = await _masterDataService.UploadCitiesAsync(stream, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("pincode_download")]
    [HttpGet("pincode-download")]
    [HttpGet("pincodes/export")]
    public async Task<IActionResult> ExportPincodes(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.ExportPincodesAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("pincode_template")]
    [HttpGet("pincode-template")]
    [HttpGet("pincodes/template")]
    public async Task<IActionResult> PincodeTemplate(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.GetPincodeTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("pincode_upload")]
    [HttpPost("pincode-upload")]
    [HttpPost("pincodes/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPincodes(IFormFile import_file, CancellationToken cancellationToken)
    {
        await using var stream = import_file.OpenReadStream();
        var response = await _masterDataService.UploadPincodesAsync(stream, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("country_access")]
    [HttpGet("country/{id}")]
    [HttpGet("countries/{id}")]
    public async Task<IActionResult> GetCountryById(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetCountryAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("country_create")]
    [HttpPost("country")]
    [HttpPost("countries")]
    public async Task<IActionResult> CreateCountry([FromBody] CountryRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.CreateCountryAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [RequirePermission("country_edit")]
    [HttpPut("country/{id}")]
    [HttpPut("countries/{id}")]
    [HttpPatch("country/{id}")]
    [HttpPatch("countries/{id}")]
    public async Task<IActionResult> UpdateCountry(ulong id, [FromBody] CountryRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.UpdateCountryAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("country_edit")]
    [HttpPatch("country/{id}/status")]
    [HttpPatch("countries/{id}/status")]
    public async Task<IActionResult> SetCountryActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.SetCountryActiveAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("country_delete")]
    [HttpDelete("country/{id}")]
    [HttpDelete("countries/{id}")]
    public async Task<IActionResult> DeleteCountry(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.DeleteCountryAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("state_access")]
    [HttpGet("state/{id}")]
    [HttpGet("states/{id}")]
    public async Task<IActionResult> GetStateById(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetStateAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("state_create")]
    [HttpPost("state")]
    [HttpPost("states")]
    public async Task<IActionResult> CreateState([FromBody] StateRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.CreateStateAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [RequirePermission("state_edit")]
    [HttpPut("state/{id}")]
    [HttpPut("states/{id}")]
    [HttpPatch("state/{id}")]
    [HttpPatch("states/{id}")]
    public async Task<IActionResult> UpdateState(ulong id, [FromBody] StateRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.UpdateStateAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("state_edit")]
    [HttpPatch("state/{id}/status")]
    [HttpPatch("states/{id}/status")]
    public async Task<IActionResult> SetStateActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.SetStateActiveAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("state_delete")]
    [HttpDelete("state/{id}")]
    [HttpDelete("states/{id}")]
    public async Task<IActionResult> DeleteState(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.DeleteStateAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("district_access")]
    [HttpGet("district/{id}")]
    [HttpGet("districts/{id}")]
    public async Task<IActionResult> GetDistrictById(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetDistrictAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("district_create")]
    [HttpPost("district")]
    [HttpPost("districts")]
    public async Task<IActionResult> CreateDistrict([FromBody] DistrictRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.CreateDistrictAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [RequirePermission("district_edit")]
    [HttpPut("district/{id}")]
    [HttpPut("districts/{id}")]
    [HttpPatch("district/{id}")]
    [HttpPatch("districts/{id}")]
    public async Task<IActionResult> UpdateDistrict(ulong id, [FromBody] DistrictRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.UpdateDistrictAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("district_edit")]
    [HttpPatch("district/{id}/status")]
    [HttpPatch("districts/{id}/status")]
    public async Task<IActionResult> SetDistrictActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.SetDistrictActiveAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("district_delete")]
    [HttpDelete("district/{id}")]
    [HttpDelete("districts/{id}")]
    public async Task<IActionResult> DeleteDistrict(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.DeleteDistrictAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("city_access")]
    [HttpGet("city/{id}")]
    [HttpGet("cities/{id}")]
    public async Task<IActionResult> GetCityById(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetCityAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("city_create")]
    [HttpPost("city")]
    [HttpPost("cities")]
    public async Task<IActionResult> CreateCity([FromBody] CityRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.CreateCityAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [RequirePermission("city_edit")]
    [HttpPut("city/{id}")]
    [HttpPut("cities/{id}")]
    [HttpPatch("city/{id}")]
    [HttpPatch("cities/{id}")]
    public async Task<IActionResult> UpdateCity(ulong id, [FromBody] CityRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.UpdateCityAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("city_edit")]
    [HttpPatch("city/{id}/status")]
    [HttpPatch("cities/{id}/status")]
    public async Task<IActionResult> SetCityActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.SetCityActiveAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("city_delete")]
    [HttpDelete("city/{id}")]
    [HttpDelete("cities/{id}")]
    public async Task<IActionResult> DeleteCity(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.DeleteCityAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("pincode_access")]
    [HttpGet("pincode/{id}")]
    [HttpGet("pincodes/{id}")]
    public async Task<IActionResult> GetPincodeById(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetPincodeAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("pincode_create")]
    [HttpPost("pincode")]
    [HttpPost("pincodes")]
    public async Task<IActionResult> CreatePincode([FromBody] PincodeRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.CreatePincodeAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [RequirePermission("pincode_edit")]
    [HttpPut("pincode/{id}")]
    [HttpPut("pincodes/{id}")]
    [HttpPatch("pincode/{id}")]
    [HttpPatch("pincodes/{id}")]
    public async Task<IActionResult> UpdatePincode(ulong id, [FromBody] PincodeRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.UpdatePincodeAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("pincode_edit")]
    [HttpPatch("pincode/{id}/status")]
    [HttpPatch("pincodes/{id}/status")]
    public async Task<IActionResult> SetPincodeActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.SetPincodeActiveAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("pincode_delete")]
    [HttpDelete("pincode/{id}")]
    [HttpDelete("pincodes/{id}")]
    public async Task<IActionResult> DeletePincode(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.DeletePincodeAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("branch")]
    [HttpGet("getbranches")]
    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetBranchesAsync(search, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("branch_report_download", "branch")]
    [HttpGet("branch_report/download")]
    [HttpGet("branches/export")]
    public async Task<IActionResult> ExportBranches(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.ExportBranchesAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("branch")]
    [HttpGet("branches/{id}")]
    public async Task<IActionResult> GetBranch(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetBranchAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("branch")]
    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] BranchRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.CreateBranchAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [RequirePermission("branch")]
    [HttpPut("branches/{id}")]
    [HttpPatch("branches/{id}")]
    public async Task<IActionResult> UpdateBranch(ulong id, [FromBody] BranchRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.UpdateBranchAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("branch")]
    [HttpPatch("branches/{id}/status")]
    public async Task<IActionResult> SetBranchActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.SetBranchActiveAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("branch")]
    [HttpDelete("branches/{id}")]
    public async Task<IActionResult> DeleteBranch(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.DeleteBranchAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("division")]
    [HttpGet("getdivisions")]
    [HttpGet("division")]
    [HttpGet("divisions")]
    public async Task<IActionResult> GetDivisions([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetDivisionsAsync(search, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("division_report_download", "division")]
    [HttpGet("division_report/download")]
    [HttpGet("divisions/export")]
    public async Task<IActionResult> ExportDivisions(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.ExportDivisionsAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("division")]
    [HttpGet("division/{id}")]
    [HttpGet("divisions/{id}")]
    public async Task<IActionResult> GetDivision(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetDivisionAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("division")]
    [HttpPost("division")]
    [HttpPost("divisions")]
    public async Task<IActionResult> CreateDivision([FromBody] DivisionRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.CreateDivisionAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [RequirePermission("division")]
    [HttpPut("division/{id}")]
    [HttpPut("divisions/{id}")]
    [HttpPatch("division/{id}")]
    [HttpPatch("divisions/{id}")]
    public async Task<IActionResult> UpdateDivision(ulong id, [FromBody] DivisionRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.UpdateDivisionAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("division")]
    [HttpPatch("division/{id}/status")]
    [HttpPatch("divisions/{id}/status")]
    public async Task<IActionResult> SetDivisionActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.SetDivisionActiveAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("division")]
    [HttpDelete("division/{id}")]
    [HttpDelete("divisions/{id}")]
    public async Task<IActionResult> DeleteDivision(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.DeleteDivisionAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("designation")]
    [HttpGet("getdesignations")]
    [HttpGet("designation")]
    [HttpGet("designations")]
    public async Task<IActionResult> GetDesignations([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetDesignationsAsync(search, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("designation")]
    [HttpGet("designations/export")]
    public async Task<IActionResult> ExportDesignations(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.ExportDesignationsAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("designation")]
    [HttpGet("designation/{id}")]
    [HttpGet("designations/{id}")]
    public async Task<IActionResult> GetDesignation(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetDesignationAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("designation")]
    [HttpPost("designation")]
    [HttpPost("designations")]
    public async Task<IActionResult> CreateDesignation([FromBody] DesignationRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.CreateDesignationAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [RequirePermission("designation")]
    [HttpPut("designation/{id}")]
    [HttpPut("designations/{id}")]
    [HttpPatch("designation/{id}")]
    [HttpPatch("designations/{id}")]
    public async Task<IActionResult> UpdateDesignation(ulong id, [FromBody] DesignationRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.UpdateDesignationAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("designation")]
    [HttpPatch("designation/{id}/status")]
    [HttpPatch("designations/{id}/status")]
    public async Task<IActionResult> SetDesignationActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.SetDesignationActiveAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("designation")]
    [HttpDelete("designation/{id}")]
    [HttpDelete("designations/{id}")]
    public async Task<IActionResult> DeleteDesignation(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.DeleteDesignationAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("departments")]
    [HttpGet("getdepartments")]
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetDepartmentsAsync(search, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("department_report_download", "departments")]
    [HttpGet("department_report/download")]
    [HttpGet("departments/export")]
    public async Task<IActionResult> ExportDepartments(CancellationToken cancellationToken)
    {
        var file = await _masterDataService.ExportDepartmentsAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize]
    [RequirePermission("departments")]
    [HttpGet("departments/{id}")]
    public async Task<IActionResult> GetDepartment(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetDepartmentAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("departments")]
    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment([FromBody] DepartmentRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.CreateDepartmentAsync(request, CurrentUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [RequirePermission("departments")]
    [HttpPut("departments/{id}")]
    [HttpPatch("departments/{id}")]
    public async Task<IActionResult> UpdateDepartment(ulong id, [FromBody] DepartmentRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.UpdateDepartmentAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("departments")]
    [HttpPatch("departments/{id}/status")]
    public async Task<IActionResult> SetDepartmentActive(ulong id, [FromBody] ActiveStatusRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.SetDepartmentActiveAsync(id, request, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [RequirePermission("departments")]
    [HttpDelete("departments/{id}")]
    public async Task<IActionResult> DeleteDepartment(ulong id, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.DeleteDepartmentAsync(id, CurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("getlocationdetails")]
    public async Task<IActionResult> GetLocationDetails([FromQuery] string? pincode, [FromQuery(Name = "city_id")] ulong? cityId, [FromQuery] string? city, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetLocationDetailsAsync(pincode, cityId, city, cancellationToken);
        return Ok(response);
    }

    [HttpPost("getlocationdetails")]
    public async Task<IActionResult> GetLocationDetails([FromBody] LocationDetailsRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _masterDataService.GetLocationDetailsAsync(request.Pincode, request.CityId, request.City, cancellationToken);
        return Ok(response);
    }

    private ulong? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ulong.TryParse(subject, out var userId) ? userId : null;
    }
}
