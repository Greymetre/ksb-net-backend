using Application.DTOs.Redemptions;
using Application.DTOs.MasterData;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IRedemptionService
{
    Task<LaravelApiResponse> GetRedemptionsAsync(RedemptionFilterDto filter, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportRedemptionsAsync(RedemptionFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetCustomerOptionsAsync(string? search, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateRedemptionAsync(RedemptionCreateRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
}
