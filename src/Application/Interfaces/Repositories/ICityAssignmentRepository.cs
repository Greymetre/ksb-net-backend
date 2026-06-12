using Application.DTOs.CityAssignments;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ICityAssignmentRepository
{
    Task<IReadOnlyList<CityAssignmentDto>> GetAssignmentsAsync(CityAssignmentFilterDto filter, CancellationToken cancellationToken);
    Task<CityAssignmentOptionsDto> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(ulong userId, CancellationToken cancellationToken);
    Task<UserCityAssign?> GetAssignmentEntityAsync(ulong id, CancellationToken cancellationToken);
    Task<UserCityAssign?> GetAssignmentByUserCityAsync(ulong userId, ulong cityId, CancellationToken cancellationToken);
    Task AddAssignmentAsync(UserCityAssign assignment, CancellationToken cancellationToken);
    Task DeleteAssignmentAsync(UserCityAssign assignment, CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(ulong userId, CancellationToken cancellationToken);
    Task<bool> CityExistsAsync(ulong cityId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
