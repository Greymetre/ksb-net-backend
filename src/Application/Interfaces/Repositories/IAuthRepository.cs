using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IAuthRepository
{
    Task<User?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<Customer?> FindCustomerByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<bool> UserMobileExistsAsync(string mobile, CancellationToken cancellationToken);
    Task<bool> UserEmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<User> AddUserAsync(User user, CancellationToken cancellationToken);
    Task AddUserDetailsAsync(UserDetails userDetails, CancellationToken cancellationToken);
    Task AddUserCityAssignsAsync(IEnumerable<UserCityAssign> cityAssigns, CancellationToken cancellationToken);
    Task AddUserEducationAsync(IEnumerable<UserEducation> education, CancellationToken cancellationToken);
    Task SyncUserRolesAndRolePermissionsAsync(ulong userId, IEnumerable<ulong> roleIds, CancellationToken cancellationToken);
    Task<Customer> AddCustomerAsync(Customer customer, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Role>> GetUserRolesAsync(ulong userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Permission>> GetUserPermissionsAsync(ulong userId, CancellationToken cancellationToken);
    Task<MobileUserLoginDetail?> GetUserLoginDetailAsync(ulong userId, CancellationToken cancellationToken);
    Task UpsertLoginDetailAsync(MobileUserLoginDetail detail, CancellationToken cancellationToken);
    Task StoreTokenAsync(OAuthAccessToken token, CancellationToken cancellationToken);
    Task RevokeTokenAsync(string tokenId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
