namespace Application.DTOs.LoyaltySchemes;

public sealed class LoyaltySchemeDto
{
    public ulong Id { get; set; }
    public string Active { get; set; } = "Y";
    public string SchemeName { get; set; } = string.Empty;
    public string SchemeCode { get; set; } = string.Empty;
    public string? SchemeDescription { get; set; }
    public string SchemeTag { get; set; } = "Regular";
    public string CustomerType { get; set; } = string.Empty;
    public string AreaScope { get; set; } = "All";
    public string[] AreaValues { get; set; } = [];
    public string AreaDisplay { get; set; } = "All India";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string SchemeType { get; set; } = "Invoice";
    public string BasedOn { get; set; } = "Value";
    public string Status { get; set; } = "Draft";
    public ulong? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<LoyaltySchemeSlabDto> Slabs { get; set; } = [];
}

public sealed class LoyaltySchemeSlabDto
{
    public ulong Id { get; set; }
    public string TierName { get; set; } = string.Empty;
    public decimal ValueFrom { get; set; }
    public decimal? ValueTo { get; set; }
    public decimal RewardValue { get; set; }
    public int SortOrder { get; set; }
}

public sealed class LoyaltySchemeRequestDto
{
    public string? Active { get; set; }
    public string? SchemeName { get; set; }
    public string? SchemeCode { get; set; }
    public string? SchemeDescription { get; set; }
    public string? SchemeTag { get; set; }
    public string? CustomerType { get; set; }
    public string? AreaScope { get; set; }
    public string[]? AreaValues { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? SchemeType { get; set; }
    public string? BasedOn { get; set; }
    public string? Status { get; set; }
    public List<LoyaltySchemeSlabRequestDto> Slabs { get; set; } = [];
}

public sealed class LoyaltySchemeSlabRequestDto
{
    public string? TierName { get; set; }
    public decimal? ValueFrom { get; set; }
    public decimal? ValueTo { get; set; }
    public decimal? RewardValue { get; set; }
}

public sealed class LoyaltySchemeFilterDto
{
    public string? Search { get; set; }
    public string? Status { get; set; }
}

public sealed class LoyaltySchemeOptionsDto
{
    public List<LoyaltySchemeOptionDto> Branches { get; set; } = [];
    public List<LoyaltySchemeOptionDto> Zones { get; set; } = [];
    public List<LoyaltySchemeOptionDto> States { get; set; } = [];
    public List<LoyaltySchemeOptionDto> Customers { get; set; } = [];
}

public sealed class LoyaltySchemeOptionDto
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
