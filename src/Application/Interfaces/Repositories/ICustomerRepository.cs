using Application.DTOs.Customers;

namespace Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyCollection<CustomerDto>> GetCustomersAsync(CustomerListFilterDto filter, CancellationToken cancellationToken);
    Task<CustomerDto?> GetCustomerAsync(ulong id, CancellationToken cancellationToken);
    Task<CustomerDto> CreateCustomerAsync(CustomerRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<CustomerDto?> UpdateCustomerAsync(ulong id, CustomerRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<CustomerDto?> SetCustomerActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken);
    Task<bool> DeleteCustomerAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task EnsureDistributorLoginUserAsync(ulong customerId, ulong? actorUserId, CancellationToken cancellationToken);
    Task<bool> MobileExistsAsync(string mobile, ulong? exceptId, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, ulong? exceptId, CancellationToken cancellationToken);
}
