using Application.DTOs.MasterData;
using Application.DTOs.Products;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IProductService
{
    Task<LaravelApiResponse> GetSegmentsAsync(string? search, bool includeInactive, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportSegmentsAsync(CancellationToken cancellationToken);
    Task<MasterDataFileDto> GetSegmentTemplateAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> UploadSegmentsAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateSegmentAsync(ProductSegmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateSegmentAsync(ulong id, ProductSegmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetSegmentActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteSegmentAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);

    Task<LaravelApiResponse> GetFamiliesAsync(ulong? segmentId, string? search, bool includeInactive, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportFamiliesAsync(CancellationToken cancellationToken);
    Task<MasterDataFileDto> GetFamilyTemplateAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> UploadFamiliesAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateFamilyAsync(ProductFamilyRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateFamilyAsync(ulong id, ProductFamilyRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetFamilyActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteFamilyAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);

    Task<LaravelApiResponse> GetProductsAsync(ulong? segmentId, ulong? familyId, string? search, bool includeInactive, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportProductsAsync(CancellationToken cancellationToken);
    Task<MasterDataFileDto> GetProductTemplateAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> UploadProductsAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateProductAsync(ProductRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateProductAsync(ulong id, ProductRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetProductActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteProductAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
}

