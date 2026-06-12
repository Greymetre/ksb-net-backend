using Application.DTOs.Customers;
using Application.DTOs.MasterData;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface ICustomerService
{
    Task<LaravelApiResponse> GetCustomersAsync(CustomerListFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetCustomerAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateCustomerAsync(CustomerRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateCustomerAsync(ulong id, CustomerRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> ApproveKycDocumentAsync(ulong id, string documentKey, string? remark, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> RejectKycDocumentAsync(ulong id, string documentKey, string? remark, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetCustomerActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteCustomerAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportCustomersAsync(CustomerListFilterDto filter, string baseUrl, CancellationToken cancellationToken);
    Task<MasterDataFileDto> GetCustomerTemplateAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> UploadCustomersAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken);
}
