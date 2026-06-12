using Application.DTOs.UserTargets;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IUserTargetRepository
{
    Task<IReadOnlyCollection<UserTargetDto>> GetTargetsAsync(UserTargetFilterDto filter, CancellationToken cancellationToken);
    Task<UserTargetDto?> GetTargetDtoAsync(ulong id, CancellationToken cancellationToken);
    Task<SalesTargetUser?> GetTargetAsync(ulong id, CancellationToken cancellationToken);
    Task<UserTargetOptionsDto> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken);
    Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken);
    Task<SalesTargetUser?> FindTargetAsync(ulong userId, ulong? branchId, string month, int year, CancellationToken cancellationToken);
    Task AddTargetAsync(SalesTargetUser target, CancellationToken cancellationToken);
    Task<bool> DeleteTargetAsync(ulong id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
