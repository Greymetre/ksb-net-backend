namespace Domain.Entities;

public sealed class UserCityAssign : BaseEntity
{
    public ulong UserId { get; set; }
    public ulong? ReportingId { get; set; }
    public ulong? CityId { get; set; }
}
