namespace Domain.Entities;

public sealed class ModelHasRole
{
    public ulong RoleId { get; set; }
    public string ModelType { get; set; } = string.Empty;
    public ulong ModelId { get; set; }
    public Role? Role { get; set; }
}
