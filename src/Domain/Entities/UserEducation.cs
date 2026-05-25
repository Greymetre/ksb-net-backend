namespace Domain.Entities;

public sealed class UserEducation : BaseEntity
{
    public ulong? UserId { get; set; }
    public ulong? EducationTypeId { get; set; }
    public string? DegreeName { get; set; }
    public string? BoardName { get; set; }
    public string? Percentage { get; set; }
    public string? Grade { get; set; }
}
