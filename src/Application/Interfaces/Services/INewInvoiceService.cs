using Application.DTOs.NewInvoices;
using Application.DTOs.MasterData;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface INewInvoiceService
{
    Task<LaravelApiResponse> GetInvoicesAsync(NewInvoiceFilterDto filter, ulong? actorUserId, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportInvoicesAsync(NewInvoiceFilterDto filter, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetInvoiceAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetRetailersAsync(string? search, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateInvoiceAsync(NewInvoiceRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateInvoiceAsync(ulong id, NewInvoiceRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteInvoiceAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> ApproveInvoiceAsync(ulong id, string level, string? remark, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> RejectInvoiceAsync(ulong id, string? remark, ulong? actorUserId, CancellationToken cancellationToken);
}
