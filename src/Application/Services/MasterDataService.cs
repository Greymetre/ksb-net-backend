using Application.Common;
using Application.DTOs.MasterData;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using System.Globalization;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Services;

public sealed class MasterDataService : IMasterDataService
{
    private readonly IMasterDataRepository _repository;

    public MasterDataService(IMasterDataRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetCountriesAsync(string? search, CancellationToken cancellationToken, bool includeInactive = false) =>
        LaravelApiResponse.Success("countries", await _repository.GetCountriesAsync(search, cancellationToken, includeInactive));

    public async Task<MasterDataFileDto> ExportCountriesAsync(CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportCountriesAsync(cancellationToken);
        return CreateWorkbook(
            "countrys.xlsx",
            ["id", "country_name"],
            rows.Select(x => new object?[] { x.Id, x.CountryName }));
    }

    public Task<MasterDataFileDto> GetCountryTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(CreateWorkbook("countrys-template.xlsx", ["country_name"], []));

    public async Task<LaravelApiResponse> UploadCountriesAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await ImportRowsAsync(fileStream, async row =>
        {
            await CreateCountryAsync(new CountryRequestDto
            {
                CountryName = ToTitleCase(row.Value("country_name"))
            }, actorUserId, cancellationToken);
            return false;
        }, cancellationToken);

        return LaravelApiResponse.Success("import", result, "Country import completed");
    }

    public async Task<LaravelApiResponse> GetCountryAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("country", await GetOrThrowAsync(_repository.GetCountryAsync(id, cancellationToken), "Country not found"));

    public async Task<LaravelApiResponse> CreateCountryAsync(CountryRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireValue(request.CountryName, "Country name is required.");
        return LaravelApiResponse.Success("country", await _repository.CreateCountryAsync(request, actorUserId, cancellationToken), "Country Store Successfully");
    }

    public async Task<LaravelApiResponse> UpdateCountryAsync(ulong id, CountryRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var country = await _repository.UpdateCountryAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("country", country ?? throw NotFound("Country not found"), "Country updated successfully");
    }

    public async Task<LaravelApiResponse> SetCountryActiveAsync(ulong id, ActiveStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var country = await _repository.SetCountryActiveAsync(id, request.Active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("country", country ?? throw NotFound("Country not found"), "Country status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteCountryAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteCountryAsync(id, actorUserId, cancellationToken)) throw NotFound("Country not found");
        return LaravelApiResponse.MessageOnly("success", "Country deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetStatesAsync(ulong? countryId, string? search, CancellationToken cancellationToken, bool includeInactive = false) =>
        LaravelApiResponse.Success("states", await _repository.GetStatesAsync(countryId, search, cancellationToken, includeInactive));

    public async Task<MasterDataFileDto> ExportStatesAsync(CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportStatesAsync(cancellationToken);
        return CreateWorkbook(
            "states.xlsx",
            ["id", "state_name", "country_id", "country_name", "gst_code"],
            rows.Select(x => new object?[] { x.Id, x.StateName, x.CountryId, x.CountryName, x.GstCode }));
    }

    public Task<MasterDataFileDto> GetStateTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(CreateWorkbook("states-template.xlsx", ["state_name", "country_id", "gst_code"], []));

    public async Task<LaravelApiResponse> UploadStatesAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await ImportRowsAsync(fileStream, async row =>
        {
            var request = new StateRequestDto
            {
                StateName = ToTitleCase(row.Value("state_name")),
                CountryId = row.ULong("country_id"),
                GstCode = row.Value("gst_code")
            };

            if (row.ULong("id") is { } id)
            {
                await UpdateStateAsync(id, request, actorUserId, cancellationToken);
                return true;
            }

            await CreateStateAsync(request, actorUserId, cancellationToken);
            return false;
        }, cancellationToken);

        return LaravelApiResponse.Success("import", result, "State import completed");
    }

    public async Task<LaravelApiResponse> GetStateAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("state", await GetOrThrowAsync(_repository.GetStateAsync(id, cancellationToken), "State not found"));

    public async Task<LaravelApiResponse> CreateStateAsync(StateRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireValue(request.StateName, "State name is required.");
        RequireId(request.CountryId, "Country is required.");
        await RequireCountryExistsAsync(request.CountryId!.Value, cancellationToken);
        return LaravelApiResponse.Success("state", await _repository.CreateStateAsync(request, actorUserId, cancellationToken), "State Store Successfully");
    }

    public async Task<LaravelApiResponse> UpdateStateAsync(ulong id, StateRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (request.CountryId.HasValue) await RequireCountryExistsAsync(request.CountryId.Value, cancellationToken);
        var state = await _repository.UpdateStateAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("state", state ?? throw NotFound("State not found"), "State updated successfully");
    }

    public async Task<LaravelApiResponse> SetStateActiveAsync(ulong id, ActiveStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var state = await _repository.SetStateActiveAsync(id, request.Active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("state", state ?? throw NotFound("State not found"), "State status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteStateAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteStateAsync(id, actorUserId, cancellationToken)) throw NotFound("State not found");
        return LaravelApiResponse.MessageOnly("success", "State deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetDistrictsAsync(ulong? stateId, string? search, CancellationToken cancellationToken, bool includeInactive = false) =>
        LaravelApiResponse.Success("districts", await _repository.GetDistrictsAsync(stateId, search, cancellationToken, includeInactive));

    public async Task<MasterDataFileDto> ExportDistrictsAsync(CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportDistrictsAsync(cancellationToken);
        return CreateWorkbook(
            "districts.xlsx",
            ["id", "district_name", "state_id", "state_name"],
            rows.Select(x => new object?[] { x.Id, x.DistrictName, x.StateId, x.StateName }));
    }

    public Task<MasterDataFileDto> GetDistrictTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(CreateWorkbook("districts-template.xlsx", ["district_name", "state_id"], []));

    public async Task<LaravelApiResponse> UploadDistrictsAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await ImportRowsAsync(fileStream, async row =>
        {
            await CreateDistrictAsync(new DistrictRequestDto
            {
                DistrictName = ToFirstUpper(row.Value("district_name")),
                StateId = row.ULong("state_id")
            }, actorUserId, cancellationToken);
            return false;
        }, cancellationToken);

        return LaravelApiResponse.Success("import", result, "District import completed");
    }

    public async Task<LaravelApiResponse> GetDistrictAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("district", await GetOrThrowAsync(_repository.GetDistrictAsync(id, cancellationToken), "District not found"));

    public async Task<LaravelApiResponse> CreateDistrictAsync(DistrictRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireValue(request.DistrictName, "District name is required.");
        RequireId(request.StateId, "State is required.");
        await RequireStateExistsAsync(request.StateId!.Value, cancellationToken);
        return LaravelApiResponse.Success("district", await _repository.CreateDistrictAsync(request, actorUserId, cancellationToken), "District Store Successfully");
    }

    public async Task<LaravelApiResponse> UpdateDistrictAsync(ulong id, DistrictRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (request.StateId.HasValue) await RequireStateExistsAsync(request.StateId.Value, cancellationToken);
        var district = await _repository.UpdateDistrictAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("district", district ?? throw NotFound("District not found"), "District updated successfully");
    }

    public async Task<LaravelApiResponse> SetDistrictActiveAsync(ulong id, ActiveStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var district = await _repository.SetDistrictActiveAsync(id, request.Active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("district", district ?? throw NotFound("District not found"), "District status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteDistrictAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteDistrictAsync(id, actorUserId, cancellationToken)) throw NotFound("District not found");
        return LaravelApiResponse.MessageOnly("success", "District deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetCitiesAsync(ulong? stateId, ulong? districtId, string? search, CancellationToken cancellationToken, bool includeInactive = false) =>
        LaravelApiResponse.Success("cities", await _repository.GetCitiesAsync(stateId, districtId, search, cancellationToken, includeInactive));

    public async Task<MasterDataFileDto> ExportCitiesAsync(CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportCitiesAsync(cancellationToken);
        return CreateWorkbook(
            "cities.xlsx",
            ["id", "city_name", "district_id", "district_name", "grade", "state_id", "state_name"],
            rows.Select(x => new object?[] { x.Id, x.CityName, x.DistrictId, x.DistrictName, x.Grade, x.StateId, x.StateName }));
    }

    public Task<MasterDataFileDto> GetCityTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(CreateWorkbook("cities-template.xlsx", ["city_name", "district_id"], []));

    public async Task<LaravelApiResponse> UploadCitiesAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await ImportRowsAsync(fileStream, async row =>
        {
            var districtId = row.ULong("district_id");
            var district = districtId.HasValue
                ? await _repository.GetDistrictAsync(districtId.Value, cancellationToken)
                : null;

            var request = new CityRequestDto
            {
                CityName = row.Value("city_name"),
                DistrictId = districtId,
                Grade = row.Value("grade"),
                StateId = district?.StateId ?? row.ULong("state_id")
            };

            if (row.ULong("id") is { } id)
            {
                if (await _repository.GetCityAsync(id, cancellationToken) is null)
                {
                    await CreateCityAsync(request, actorUserId, cancellationToken);
                    return false;
                }

                await UpdateCityAsync(id, request, actorUserId, cancellationToken);
                return true;
            }

            await CreateCityAsync(request, actorUserId, cancellationToken);
            return false;
        }, cancellationToken);

        return LaravelApiResponse.Success("import", result, "City import completed");
    }

    public async Task<LaravelApiResponse> GetCityAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("city", await GetOrThrowAsync(_repository.GetCityAsync(id, cancellationToken), "City not found"));

    public async Task<LaravelApiResponse> CreateCityAsync(CityRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireValue(request.CityName, "City name is required.");
        RequireId(request.DistrictId, "District is required.");
        await RequireDistrictExistsAsync(request.DistrictId!.Value, cancellationToken);
        if (request.StateId.HasValue) await RequireStateExistsAsync(request.StateId.Value, cancellationToken);
        return LaravelApiResponse.Success("city", await _repository.CreateCityAsync(request, actorUserId, cancellationToken), "City Store Successfully");
    }

    public async Task<LaravelApiResponse> UpdateCityAsync(ulong id, CityRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (request.DistrictId.HasValue) await RequireDistrictExistsAsync(request.DistrictId.Value, cancellationToken);
        if (request.StateId.HasValue) await RequireStateExistsAsync(request.StateId.Value, cancellationToken);
        var city = await _repository.UpdateCityAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("city", city ?? throw NotFound("City not found"), "City updated successfully");
    }

    public async Task<LaravelApiResponse> SetCityActiveAsync(ulong id, ActiveStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var city = await _repository.SetCityActiveAsync(id, request.Active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("city", city ?? throw NotFound("City not found"), "City status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteCityAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteCityAsync(id, actorUserId, cancellationToken)) throw NotFound("City not found");
        return LaravelApiResponse.MessageOnly("success", "City deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetPincodesAsync(ulong? cityId, string? pincode, CancellationToken cancellationToken, bool includeInactive = false) =>
        LaravelApiResponse.Success("pincodes", await _repository.GetPincodesAsync(cityId, pincode, cancellationToken, includeInactive));

    public async Task<MasterDataFileDto> ExportPincodesAsync(CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportPincodesAsync(cancellationToken);
        return CreateWorkbook(
            "pincodes.xlsx",
            ["id", "pincode", "city_id", "city_name"],
            rows.Select(x => new object?[] { x.Id, x.Pincode, x.CityId, x.CityName }));
    }

    public Task<MasterDataFileDto> GetPincodeTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(CreateWorkbook("pincodes-template.xlsx", ["pincode", "city_id"], []));

    public async Task<LaravelApiResponse> UploadPincodesAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var result = await ImportRowsAsync(fileStream, async row =>
        {
            await CreatePincodeAsync(new PincodeRequestDto
            {
                Pincode = row.Value("pincode"),
                CityId = row.ULong("city_id")
            }, actorUserId, cancellationToken);
            return false;
        }, cancellationToken);

        return LaravelApiResponse.Success("import", result, "Pincode import completed");
    }

    public async Task<LaravelApiResponse> GetPincodeAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("pincode", await GetOrThrowAsync(_repository.GetPincodeAsync(id, cancellationToken), "Pincode not found"));

    public async Task<LaravelApiResponse> CreatePincodeAsync(PincodeRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireValue(request.Pincode, "Pincode is required.");
        RequireId(request.CityId, "City is required.");
        await RequireCityExistsAsync(request.CityId!.Value, cancellationToken);
        return LaravelApiResponse.Success("pincode", await _repository.CreatePincodeAsync(request, actorUserId, cancellationToken), "Pincode stored successfully");
    }

    public async Task<LaravelApiResponse> UpdatePincodeAsync(ulong id, PincodeRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (request.CityId.HasValue) await RequireCityExistsAsync(request.CityId.Value, cancellationToken);
        var pincode = await _repository.UpdatePincodeAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("pincode", pincode ?? throw NotFound("Pincode not found"), "Pincode updated successfully");
    }

    public async Task<LaravelApiResponse> SetPincodeActiveAsync(ulong id, ActiveStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var pincode = await _repository.SetPincodeActiveAsync(id, request.Active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("pincode", pincode ?? throw NotFound("Pincode not found"), "Pincode status changed successfully");
    }

    public async Task<LaravelApiResponse> DeletePincodeAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeletePincodeAsync(id, actorUserId, cancellationToken)) throw NotFound("Pincode not found");
        return LaravelApiResponse.MessageOnly("success", "Pincode deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetBranchesAsync(string? search, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("branches", await _repository.GetBranchesAsync(search, cancellationToken));

    public async Task<MasterDataFileDto> ExportBranchesAsync(CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportBranchesAsync(cancellationToken);
        return CreateWorkbook(
            "branch.xlsx",
            ["id", "branch_name", "branch_code", "warehouse_id", "active", "created_by", "created_at"],
            rows.Select(x => new object?[] { x.Id, x.BranchName, x.BranchCode, x.WarehouseId, x.Active, x.CreatedByName ?? x.CreatedBy?.ToString(), x.CreatedAt }));
    }

    public async Task<LaravelApiResponse> GetBranchAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("branch", await GetOrThrowAsync(_repository.GetBranchAsync(id, cancellationToken), "Branch not found"));

    public async Task<LaravelApiResponse> CreateBranchAsync(BranchRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireValue(request.BranchName, "Branch name is required.");
        return LaravelApiResponse.Success("branch", await _repository.CreateBranchAsync(request, actorUserId, cancellationToken), "Branch Store Successfully");
    }

    public async Task<LaravelApiResponse> UpdateBranchAsync(ulong id, BranchRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var branch = await _repository.UpdateBranchAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("branch", branch ?? throw NotFound("Branch not found"), "Branch updated successfully");
    }

    public async Task<LaravelApiResponse> SetBranchActiveAsync(ulong id, ActiveStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var branch = await _repository.SetBranchActiveAsync(id, request.Active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("branch", branch ?? throw NotFound("Branch not found"), "Branch status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteBranchAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteBranchAsync(id, actorUserId, cancellationToken)) throw NotFound("Branch not found");
        return LaravelApiResponse.MessageOnly("success", "Branch deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetDivisionsAsync(string? search, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("divisions", await _repository.GetDivisionsAsync(search, cancellationToken));

    public async Task<MasterDataFileDto> ExportDivisionsAsync(CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportDivisionsAsync(cancellationToken);
        return CreateWorkbook(
            "divisions.xlsx",
            ["id", "division_name", "active", "created_by", "created_at"],
            rows.Select(x => new object?[] { x.Id, x.DivisionName, x.Active, x.CreatedByName ?? x.CreatedBy?.ToString(), x.CreatedAt }));
    }

    public async Task<LaravelApiResponse> GetDivisionAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("division", await GetOrThrowAsync(_repository.GetDivisionAsync(id, cancellationToken), "Division not found"));

    public async Task<LaravelApiResponse> CreateDivisionAsync(DivisionRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireValue(request.DivisionName, "Division name is required.");
        return LaravelApiResponse.Success("division", await _repository.CreateDivisionAsync(request, actorUserId, cancellationToken), "Division Store Successfully");
    }

    public async Task<LaravelApiResponse> UpdateDivisionAsync(ulong id, DivisionRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var division = await _repository.UpdateDivisionAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("division", division ?? throw NotFound("Division not found"), "Division updated successfully");
    }

    public async Task<LaravelApiResponse> SetDivisionActiveAsync(ulong id, ActiveStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var division = await _repository.SetDivisionActiveAsync(id, request.Active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("division", division ?? throw NotFound("Division not found"), "Division status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteDivisionAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteDivisionAsync(id, actorUserId, cancellationToken)) throw NotFound("Division not found");
        return LaravelApiResponse.MessageOnly("success", "Division deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetDesignationsAsync(string? search, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("designations", await _repository.GetDesignationsAsync(search, cancellationToken));

    public async Task<MasterDataFileDto> ExportDesignationsAsync(CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportDesignationsAsync(cancellationToken);
        return CreateWorkbook(
            "designations.xlsx",
            ["id", "designation_name", "active", "created_by", "created_at"],
            rows.Select(x => new object?[] { x.Id, x.DesignationName, x.Active, x.CreatedByName ?? x.CreatedBy?.ToString(), x.CreatedAt }));
    }

    public async Task<LaravelApiResponse> GetDesignationAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("designation", await GetOrThrowAsync(_repository.GetDesignationAsync(id, cancellationToken), "Designation not found"));

    public async Task<LaravelApiResponse> CreateDesignationAsync(DesignationRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireValue(request.DesignationName, "Designation name is required.");
        return LaravelApiResponse.Success("designation", await _repository.CreateDesignationAsync(request, actorUserId, cancellationToken), "Designation Store Successfully");
    }

    public async Task<LaravelApiResponse> UpdateDesignationAsync(ulong id, DesignationRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var designation = await _repository.UpdateDesignationAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("designation", designation ?? throw NotFound("Designation not found"), "Designation updated successfully");
    }

    public async Task<LaravelApiResponse> SetDesignationActiveAsync(ulong id, ActiveStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var designation = await _repository.SetDesignationActiveAsync(id, request.Active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("designation", designation ?? throw NotFound("Designation not found"), "Designation status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteDesignationAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteDesignationAsync(id, actorUserId, cancellationToken)) throw NotFound("Designation not found");
        return LaravelApiResponse.MessageOnly("success", "Designation deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetDepartmentsAsync(string? search, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("departments", await _repository.GetDepartmentsAsync(search, cancellationToken));

    public async Task<MasterDataFileDto> ExportDepartmentsAsync(CancellationToken cancellationToken)
    {
        var rows = await _repository.ExportDepartmentsAsync(cancellationToken);
        return CreateWorkbook(
            "departments.xlsx",
            ["id", "name", "active", "created_by", "created_at"],
            rows.Select(x => new object?[] { x.Id, x.Name, x.Active, x.CreatedByName ?? x.CreatedBy?.ToString(), x.CreatedAt }));
    }

    public async Task<LaravelApiResponse> GetDepartmentAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("department", await GetOrThrowAsync(_repository.GetDepartmentAsync(id, cancellationToken), "Department not found"));

    public async Task<LaravelApiResponse> CreateDepartmentAsync(DepartmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        RequireValue(request.Name, "Department name is required.");
        return LaravelApiResponse.Success("department", await _repository.CreateDepartmentAsync(request, actorUserId, cancellationToken), "Department added successfully");
    }

    public async Task<LaravelApiResponse> UpdateDepartmentAsync(ulong id, DepartmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var department = await _repository.UpdateDepartmentAsync(id, request, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("department", department ?? throw NotFound("Department not found"), "Department updated successfully");
    }

    public async Task<LaravelApiResponse> SetDepartmentActiveAsync(ulong id, ActiveStatusRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var department = await _repository.SetDepartmentActiveAsync(id, request.Active, actorUserId, cancellationToken);
        return LaravelApiResponse.Success("department", department ?? throw NotFound("Department not found"), "Department status changed successfully");
    }

    public async Task<LaravelApiResponse> DeleteDepartmentAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.DeleteDepartmentAsync(id, actorUserId, cancellationToken)) throw NotFound("Department not found");
        return LaravelApiResponse.MessageOnly("success", "Department deleted successfully!");
    }

    public async Task<LaravelApiResponse> GetLocationDetailsAsync(string? pincode, ulong? cityId, string? city, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pincode) && cityId is null && string.IsNullOrWhiteSpace(city))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Please provide pincode, city_id, or city.");
        }

        var details = await _repository.GetLocationDetailsAsync(pincode, cityId, city, cancellationToken);
        return LaravelApiResponse.Success("locations", details);
    }

    private static MasterDataFileDto CreateWorkbook(string fileName, string[] headings, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");

        for (var column = 0; column < headings.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = TitleCaseHeading(headings[column]);
            worksheet.Cell(1, column + 1).Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < row.Length; column++)
            {
                worksheet.Cell(rowNumber, column + 1).Value = XLCellValue.FromObject(FormatExportValue(headings[column], row[column]));
            }

            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new MasterDataFileDto
        {
            FileName = fileName,
            Content = stream.ToArray()
        };
    }

    private static async Task<MasterDataImportResultDto> ImportRowsAsync(Stream fileStream, Func<ExcelRow, Task<bool>> importRow, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed() ?? throw new LaravelHttpException(LaravelStatusCodes.BadRequest, "Import file is empty.");
        var headings = headerRow.CellsUsed()
            .ToDictionary(cell => NormalizeHeading(cell.GetString()), cell => cell.Address.ColumnNumber);

        var totalRows = 0;
        var importedRows = 0;
        var updatedRows = 0;
        var errors = new List<string>();

        foreach (var worksheetRow in worksheet.RowsUsed().Where(row => row.RowNumber() > headerRow.RowNumber()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (worksheetRow.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetString())))
            {
                continue;
            }

            totalRows++;
            try
            {
                var updated = await importRow(new ExcelRow(worksheetRow, headings));
                if (updated)
                {
                    updatedRows++;
                }
                else
                {
                    importedRows++;
                }
            }
            catch (Exception exception) when (exception is LaravelHttpException or FormatException or InvalidOperationException)
            {
                errors.Add($"Row {worksheetRow.RowNumber()}: {exception.Message}");
            }
        }

        return new MasterDataImportResultDto
        {
            TotalRows = totalRows,
            ImportedRows = importedRows,
            UpdatedRows = updatedRows,
            FailedRows = errors.Count,
            Errors = errors
        };
    }

    private static string NormalizeHeading(string heading) =>
        heading.Trim().ToLowerInvariant().Replace(" ", "_");

    private static string TitleCaseHeading(string heading) => ToTitleCase(heading.Replace("_", " ")) ?? heading;

    private static object? FormatExportValue(string heading, object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return value;
        var normalized = NormalizeHeading(heading);
        if (normalized is "id" or "country_id" or "state_id" or "district_id" or "city_id" or "pincode" or "gst_code" or "branch_code" or "warehouse_id" or "active" or "created_at") return value;
        return ToTitleCase(text);
    }

    private static string? ToTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());
    }

    private static string? ToFirstUpper(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var trimmed = value.Trim();
        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }

    private static async Task<T> GetOrThrowAsync<T>(Task<T?> task, string message)
    {
        var value = await task;
        return value ?? throw NotFound(message);
    }

    private static void RequireValue(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, message);
        }
    }

    private static void RequireId(ulong? value, string message)
    {
        if (value is null or 0)
        {
            throw new LaravelHttpException(LaravelStatusCodes.BadRequest, message);
        }
    }

    private async Task RequireCountryExistsAsync(ulong countryId, CancellationToken cancellationToken)
    {
        if (await _repository.GetCountryAsync(countryId, cancellationToken) is null)
        {
            throw NotFound("Country not found");
        }
    }

    private async Task RequireStateExistsAsync(ulong stateId, CancellationToken cancellationToken)
    {
        if (await _repository.GetStateAsync(stateId, cancellationToken) is null)
        {
            throw NotFound("State not found");
        }
    }

    private async Task RequireDistrictExistsAsync(ulong districtId, CancellationToken cancellationToken)
    {
        if (await _repository.GetDistrictAsync(districtId, cancellationToken) is null)
        {
            throw NotFound("District not found");
        }
    }

    private async Task RequireCityExistsAsync(ulong cityId, CancellationToken cancellationToken)
    {
        if (await _repository.GetCityAsync(cityId, cancellationToken) is null)
        {
            throw NotFound("City not found");
        }
    }

    private static LaravelHttpException NotFound(string message) =>
        new(LaravelStatusCodes.NotFound, message);

    private sealed class ExcelRow
    {
        private readonly IXLRow _row;
        private readonly IReadOnlyDictionary<string, int> _headings;

        public ExcelRow(IXLRow row, IReadOnlyDictionary<string, int> headings)
        {
            _row = row;
            _headings = headings;
        }

        public string? Value(string heading)
        {
            return _headings.TryGetValue(NormalizeHeading(heading), out var column)
                ? NormalizeText(_row.Cell(column).GetFormattedString())
                : null;
        }

        public ulong? ULong(string heading)
        {
            var value = Value(heading);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return ulong.TryParse(value, out var parsed)
                ? parsed
                : throw new FormatException($"{heading} must be numeric.");
        }
    }

    private static string? NormalizeText(string? value)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
