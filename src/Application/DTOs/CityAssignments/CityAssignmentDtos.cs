namespace Application.DTOs.CityAssignments;

public sealed class CityAssignmentDto
{
    public ulong Id { get; init; }
    public ulong UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserDesignation { get; init; }
    public ulong? ReportingId { get; init; }
    public string? ReportingName { get; init; }
    public string? ReportingDesignation { get; init; }
    public ulong? CityId { get; init; }
    public string? CityName { get; init; }
    public string? Grade { get; init; }
    public ulong? DistrictId { get; init; }
    public string? DistrictName { get; init; }
    public ulong? StateId { get; init; }
    public string? StateName { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public sealed class CityAssignmentFilterDto
{
    public string? Search { get; init; }
    public ulong? UserId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageLength { get; init; } = 50000;
}

public sealed class CityAssignmentRequestDto
{
    public ulong? UserId { get; init; }
    public ulong? CityId { get; init; }
    public IReadOnlyCollection<ulong>? CityIds { get; init; }
}

public sealed class CityAssignmentOptionDto
{
    public ulong Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class CityAssignmentOptionsDto
{
    public IReadOnlyList<CityAssignmentOptionDto> Users { get; init; } = [];
    public IReadOnlyList<CityAssignmentOptionDto> Reportings { get; init; } = [];
    public IReadOnlyList<CityAssignmentOptionDto> Cities { get; init; } = [];
}

public sealed class CityAssignmentFileDto
{
    public string FileName { get; init; } = "user-city-assignments.xlsx";
    public string ContentType { get; init; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public byte[] Content { get; init; } = [];
}
