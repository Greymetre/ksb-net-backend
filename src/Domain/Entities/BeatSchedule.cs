namespace Domain.Entities;

public sealed class BeatSchedule : BaseEntity
{
    public string Active { get; set; } = "Y";
    public ulong? BeatId { get; set; }
    public DateTime? BeatDate { get; set; }
    public ulong? UserId { get; set; }
    public ulong? TourId { get; set; }
}
