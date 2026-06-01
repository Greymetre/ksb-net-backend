namespace Domain.Entities;

public sealed class CompOffLeave : BaseEntity
{
    public long? UserId { get; set; }
    public string? LeaveId { get; set; }
    public DateTime? CompOffDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsUsed { get; set; }
    public decimal Balance { get; set; } = 1m;
}
