using Application.DTOs.Products;

namespace Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyCollection<ProductSegmentDto>> GetSegmentsAsync(string? search, bool includeInactive, CancellationToken cancellationToken);
    Task<ProductSegmentDto?> GetSegmentAsync(ulong id, CancellationToken cancellationToken);
    Task<ProductSegmentDto> CreateSegmentAsync(ProductSegmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<ProductSegmentDto?> UpdateSegmentAsync(ulong id, ProductSegmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<ProductSegmentDto?> SetSegmentActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken);
    Task<bool> DeleteSegmentAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductFamilyDto>> GetFamiliesAsync(ulong? segmentId, string? search, bool includeInactive, CancellationToken cancellationToken);
    Task<ProductFamilyDto?> GetFamilyAsync(ulong id, CancellationToken cancellationToken);
    Task<ProductFamilyDto> CreateFamilyAsync(ProductFamilyRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<ProductFamilyDto?> UpdateFamilyAsync(ulong id, ProductFamilyRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<ProductFamilyDto?> SetFamilyActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken);
    Task<bool> DeleteFamilyAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(ulong? segmentId, ulong? familyId, string? search, bool includeInactive, CancellationToken cancellationToken);
    Task<ProductDto?> GetProductAsync(ulong id, CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(ProductRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<ProductDto?> UpdateProductAsync(ulong id, ProductRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<ProductDto?> SetProductActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken);
    Task<bool> DeleteProductAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
}

