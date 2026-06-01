namespace Domain.Entities;

public sealed class Beat : BaseEntity
{
    public string Active { get; set; } = "Y";
    public string BeatName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CityId { get; set; }
}
