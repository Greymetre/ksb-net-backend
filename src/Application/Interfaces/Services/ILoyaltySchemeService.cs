using Application.DTOs.LoyaltySchemes;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface ILoyaltySchemeService
{
    Task<LaravelApiResponse> GetSchemesAsync(LoyaltySchemeFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetSchemeAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetOptionsAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> GenerateSchemeCodeAsync(string? schemeName, string? schemeTag, string? basedOn, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateSchemeAsync(LoyaltySchemeRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateSchemeAsync(ulong id, LoyaltySchemeRequestDto request, ulong? actorUserId, bool isSuperAdmin, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SendToDraftAsync(ulong id, ulong? actorUserId, bool isSuperAdmin, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SubmitSchemeAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> ApproveSchemeAsync(ulong id, string? remark, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> RejectSchemeAsync(ulong id, string? remark, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> PublishSchemeAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetBrochureAsync(ulong id, string brochurePath, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteSchemeAsync(ulong id, ulong? actorUserId, bool isSuperAdmin, CancellationToken cancellationToken);
}
