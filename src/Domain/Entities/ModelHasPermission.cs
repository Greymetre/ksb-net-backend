namespace Domain.Entities;

public sealed class ModelHasPermission
{
    public ulong PermissionId { get; set; }
    public string ModelType { get; set; } = string.Empty;
    public ulong ModelId { get; set; }
    public Permission? Permission { get; set; }
}
