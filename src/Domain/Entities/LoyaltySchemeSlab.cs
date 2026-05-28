namespace Domain.Entities;

public sealed class LoyaltySchemeSlab : BaseEntity
{
    public ulong LoyaltySchemeId { get; set; }
    public string TierName { get; set; } = string.Empty;
    public decimal ValueFrom { get; set; }
    public decimal? ValueTo { get; set; }
    public decimal RewardValue { get; set; }
    public int SortOrder { get; set; }
    public LoyaltyScheme? LoyaltyScheme { get; set; }
}
