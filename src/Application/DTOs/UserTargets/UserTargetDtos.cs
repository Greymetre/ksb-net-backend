using Application.DTOs.Users;

namespace Application.DTOs.UserTargets;

public sealed class UserTargetFilterDto
{
    public ulong? BranchId { get; set; }
    public ulong? UserId { get; set; }
    public ulong? DivisionId { get; set; }
    public string? Type { get; set; }
    public string? Month { get; set; }
    public string? FinancialYear { get; set; }
    public string? Search { get; set; }
}

public sealed class UserTargetDto
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; }
    public ulong? BranchId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? UserName { get; set; }
    public string? DesignationName { get; set; }
    public string? BranchName { get; set; }
    public ulong? DivisionId { get; set; }
    public string? DivisionName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public decimal Target { get; set; }
    public decimal Achievement { get; set; }
    public string AchievementPercent { get; set; } = string.Empty;
    public decimal? QuantityTarget { get; set; }
    public decimal? QuantityAchievement { get; set; }
    public string QuantityAchievementPercent { get; set; } = string.Empty;
}

public sealed class UserTargetRequestDto
{
    public ulong? UserId { get; set; }
    public ulong? BranchId { get; set; }
    public string? Type { get; set; }
    public string? Month { get; set; }
    public string? Year { get; set; }
    public decimal? Target { get; set; }
    public decimal? QuantityTarget { get; set; }
}

public sealed class UserTargetOptionsDto
{
    public IReadOnlyCollection<OptionDto> Users { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Branches { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Divisions { get; set; } = [];
    public IReadOnlyCollection<int> Years { get; set; } = [];
}

public sealed class UserTargetImportResultDto
{
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int FailedRows { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; } = [];
}

