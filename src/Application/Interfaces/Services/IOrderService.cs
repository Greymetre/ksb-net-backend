using Application.DTOs.Orders;
using Application.DTOs.MasterData;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IOrderService
{
    Task<LaravelApiResponse> GetOrdersAsync(OrderFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetOrderAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetProductsByFamilyAsync(ulong familyId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateOrderAsync(OrderRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportOrdersAsync(OrderFilterDto filter, CancellationToken cancellationToken);
}
