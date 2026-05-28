using Application.DTOs.LoyaltySchemes;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ILoyaltySchemeRepository
{
    Task<IReadOnlyCollection<LoyaltySchemeDto>> GetSchemesAsync(LoyaltySchemeFilterDto filter, CancellationToken cancellationToken);
    Task<LoyaltySchemeDto?> GetSchemeAsync(ulong id, CancellationToken cancellationToken);
    Task<LoyaltyScheme?> FindSchemeEntityAsync(ulong id, CancellationToken cancellationToken);
    Task<bool> SchemeCodeExistsAsync(string code, ulong? exceptId, CancellationToken cancellationToken);
    Task<string?> GetLastSchemeCodeAsync(string prefix, CancellationToken cancellationToken);
    Task<LoyaltySchemeDto> CreateSchemeAsync(LoyaltyScheme scheme, CancellationToken cancellationToken);
    Task<LoyaltySchemeDto> SaveSchemeAsync(LoyaltyScheme scheme, CancellationToken cancellationToken);
    Task<bool> DeleteSchemeAsync(LoyaltyScheme scheme, CancellationToken cancellationToken);
    Task<LoyaltySchemeOptionsDto> GetOptionsAsync(CancellationToken cancellationToken);
}
