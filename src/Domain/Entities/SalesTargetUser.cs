namespace Domain.Entities;

public sealed class SalesTargetUser : BaseEntity
{
    public ulong? UserId { get; set; }
    public ulong? BranchId { get; set; }
    public string? Type { get; set; }
    public string? Month { get; set; }
    public int? Year { get; set; }
    public decimal? Target { get; set; }
    public decimal? Achievement { get; set; }
    public decimal? AchievementPercent { get; set; }
    public decimal? QuantityTarget { get; set; }
    public decimal? QuantityAchievement { get; set; }
    public decimal? QuantityAchievementPercent { get; set; }
}
