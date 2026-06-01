namespace Domain.Entities;

public sealed class TourDetail : BaseEntity
{
    public ulong? TourId { get; set; }
    public ulong? CityId { get; set; }
    public DateTime? VisitedDate { get; set; }
    public ulong? VisitedCityId { get; set; }
    public DateTime? LastVisited { get; set; }
}
