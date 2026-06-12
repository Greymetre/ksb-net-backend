using Application.DTOs.MasterData;
using Application.DTOs.UserTargets;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IUserTargetService
{
    Task<LaravelApiResponse> GetTargetsAsync(UserTargetFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetTargetAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateTargetAsync(UserTargetRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateTargetAsync(ulong id, UserTargetRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteTargetAsync(ulong id, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportTargetsAsync(UserTargetFilterDto filter, CancellationToken cancellationToken);
    Task<MasterDataFileDto> GetTemplateAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> UploadTargetsAsync(Stream fileStream, CancellationToken cancellationToken);
}
