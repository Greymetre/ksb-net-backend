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
    Task<LaravelApiResponse> UpdateOrderAsync(ulong id, OrderRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteOrderAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetActiveAsync(ulong id, OrderActiveRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetStatusAsync(ulong id, OrderStatusRequestDto request, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportOrdersAsync(OrderFilterDto filter, CancellationToken cancellationToken);
}
