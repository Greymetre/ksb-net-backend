using Application.DTOs.Redemptions;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IRedemptionRepository
{
    Task<IReadOnlyCollection<RedemptionDto>> GetRedemptionsAsync(RedemptionFilterDto filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LoyaltyRedemption>> GetCustomerRedemptionsAsync(ulong customerId, CancellationToken cancellationToken);
    Task<LoyaltyRedemption> CreateAsync(LoyaltyRedemption redemption, CancellationToken cancellationToken);
    Task<bool> TransactionNoExistsAsync(string transactionNo, CancellationToken cancellationToken);
}
