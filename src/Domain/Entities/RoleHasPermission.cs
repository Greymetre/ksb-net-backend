namespace Domain.Entities;

public sealed class RoleHasPermission
{
    public ulong PermissionId { get; set; }
    public ulong RoleId { get; set; }
}
