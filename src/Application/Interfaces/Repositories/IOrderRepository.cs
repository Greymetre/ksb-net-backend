using Application.DTOs.Orders;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<IReadOnlyCollection<OrderDto>> GetOrdersAsync(OrderFilterDto filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OrderExportRowDto>> GetOrderExportRowsAsync(OrderFilterDto filter, CancellationToken cancellationToken);
    Task<OrderDto?> GetOrderAsync(ulong id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OrderDetailDto>> GetOrderDetailsAsync(ulong orderId, CancellationToken cancellationToken);
    Task<OrderOptionsDto> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OrderProductOptionDto>> GetProductsByFamilyAsync(ulong familyId, CancellationToken cancellationToken);
    Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken);
    Task<Order?> GetOrderEntityAsync(ulong id, CancellationToken cancellationToken);
    Task AddOrderAsync(Order order, CancellationToken cancellationToken);
    Task AddOrderDetailsAsync(IReadOnlyCollection<OrderDetail> details, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
