namespace Domain.Entities;

public sealed class Permission
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GuardName { get; set; } = "users";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
