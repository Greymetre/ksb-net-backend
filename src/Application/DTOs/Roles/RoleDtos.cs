namespace Application.DTOs.Roles;

public sealed class PermissionDto
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GuardName { get; set; } = "users";
}

public sealed class RoleDto
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GuardName { get; set; } = "users";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyCollection<PermissionDto> Permissions { get; set; } = [];
}

public sealed class RoleRequestDto
{
    public string? Name { get; set; }
    public string? GuardName { get; set; }
    public IReadOnlyCollection<ulong>? Permissions { get; set; }
}

public sealed class SaveRolePermissionsRequestDto
{
    public IDictionary<string, IReadOnlyCollection<ulong>> Permissions { get; set; } = new Dictionary<string, IReadOnlyCollection<ulong>>();
}

public sealed class RolePermissionsRequestDto
{
    public IReadOnlyCollection<ulong> Permissions { get; set; } = [];
}
