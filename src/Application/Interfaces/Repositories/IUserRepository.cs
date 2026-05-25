using Application.DTOs.Users;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyCollection<UserDto>> GetUsersAsync(UserListFiltersDto filters, CancellationToken cancellationToken);
    Task<UserDto?> GetUserDtoAsync(ulong id, CancellationToken cancellationToken);
    Task<UserOptionsDto> GetUserOptionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserExcelRowDto>> ExportUsersAsync(UserExportFiltersDto filters, CancellationToken cancellationToken);
    Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken);
    Task<bool> UserEmailExistsAsync(string email, ulong? exceptUserId, CancellationToken cancellationToken);
    Task<bool> UserMobileExistsAsync(string mobile, ulong? exceptUserId, CancellationToken cancellationToken);
    Task<UserDetails?> GetUserDetailsAsync(ulong userId, CancellationToken cancellationToken);
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken);
    Task AddUserAsync(User user, CancellationToken cancellationToken);
    Task AddUserDetailsAsync(UserDetails userDetails, CancellationToken cancellationToken);
    Task AddUserEducationAsync(UserEducation userEducation, CancellationToken cancellationToken);
    Task SyncUserRolesAsync(ulong userId, IEnumerable<ulong> roleIds, CancellationToken cancellationToken);
    Task<bool> DeleteUserAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
