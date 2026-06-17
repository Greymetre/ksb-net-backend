namespace Application.DTOs.MasterData;

public sealed class CountryDto
{
    public ulong Id { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string Active { get; set; } = "Y";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class StateDto
{
    public ulong Id { get; set; }
    public string StateName { get; set; } = string.Empty;
    public ulong? CountryId { get; set; }
    public string? CountryName { get; set; }
    public string? GstCode { get; set; }
    public string Active { get; set; } = "Y";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class DistrictDto
{
    public ulong Id { get; set; }
    public string DistrictName { get; set; } = string.Empty;
    public ulong? StateId { get; set; }
    public string? StateName { get; set; }
    public string Active { get; set; } = "Y";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class CityDto
{
    public ulong Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public ulong? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public ulong? StateId { get; set; }
    public string? StateName { get; set; }
    public string? Grade { get; set; }
    public string Active { get; set; } = "Y";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class PincodeDto
{
    public ulong Id { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public ulong? CityId { get; set; }
    public string? CityName { get; set; }
    public string Active { get; set; } = "Y";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class BranchDto
{
    public ulong Id { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? BranchCode { get; set; }
    public string Active { get; set; } = "Y";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class DivisionDto
{
    public ulong Id { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public string Active { get; set; } = "Y";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class DesignationDto
{
    public ulong Id { get; set; }
    public string DesignationName { get; set; } = string.Empty;
    public string Active { get; set; } = "Y";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class DepartmentDto
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Active { get; set; } = "Y";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class BranchRequestDto
{
    public string? BranchName { get; set; }
    public string? BranchCode { get; set; }
    public string? Active { get; set; }
}

public sealed class CountryRequestDto
{
    public string? CountryName { get; set; }
    public string? Active { get; set; }
}

public sealed class StateRequestDto
{
    public string? StateName { get; set; }
    public ulong? CountryId { get; set; }
    public string? GstCode { get; set; }
    public string? Active { get; set; }
}

public sealed class DistrictRequestDto
{
    public string? DistrictName { get; set; }
    public ulong? StateId { get; set; }
    public string? Active { get; set; }
}

public sealed class CityRequestDto
{
    public string? CityName { get; set; }
    public ulong? DistrictId { get; set; }
    public ulong? StateId { get; set; }
    public string? Grade { get; set; }
    public string? Active { get; set; }
}

public sealed class PincodeRequestDto
{
    public string? Pincode { get; set; }
    public ulong? CityId { get; set; }
    public string? Active { get; set; }
}

public sealed class MasterDataFileDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public byte[] Content { get; set; } = [];
}

public sealed class MasterDataImportResultDto
{
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int FailedRows { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; } = [];
}

public sealed class CountryExportRowDto
{
    public ulong Id { get; set; }
    public string CountryName { get; set; } = string.Empty;
}

public sealed class StateExportRowDto
{
    public ulong Id { get; set; }
    public string StateName { get; set; } = string.Empty;
    public ulong? CountryId { get; set; }
    public string? CountryName { get; set; }
    public string? GstCode { get; set; }
}

public sealed class DistrictExportRowDto
{
    public ulong Id { get; set; }
    public string DistrictName { get; set; } = string.Empty;
    public ulong? StateId { get; set; }
    public string? StateName { get; set; }
}

public sealed class CityExportRowDto
{
    public ulong Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public ulong? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public string? Grade { get; set; }
    public ulong? StateId { get; set; }
    public string? StateName { get; set; }
}

public sealed class PincodeExportRowDto
{
    public ulong Id { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public ulong? CityId { get; set; }
    public string? CityName { get; set; }
}

public sealed class DivisionRequestDto
{
    public string? DivisionName { get; set; }
    public string? Active { get; set; }
}

public sealed class DesignationRequestDto
{
    public string? DesignationName { get; set; }
    public string? Active { get; set; }
}

public sealed class DepartmentRequestDto
{
    public string? Name { get; set; }
    public string? Active { get; set; }
}

public sealed class ActiveStatusRequestDto
{
    public string? Active { get; set; }
}

public sealed class LocationDetailsDto
{
    public CountryDto? Country { get; set; }
    public StateDto? State { get; set; }
    public DistrictDto? District { get; set; }
    public CityDto? City { get; set; }
    public IReadOnlyCollection<PincodeDto> Pincodes { get; set; } = [];
}

public sealed class LocationDetailsRequestDto
{
    public string? Pincode { get; set; }
    public ulong? StateId { get; set; }
    public ulong? CityId { get; set; }
    public string? City { get; set; }
}
