using Application.DTOs.NewInvoices;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface INewInvoiceRepository
{
    Task<IReadOnlyCollection<NewInvoiceDto>> GetInvoicesAsync(NewInvoiceFilterDto filter, ulong? actorUserId, CancellationToken cancellationToken);
    Task<NewInvoiceDto?> GetInvoiceAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RetailerOptionDto>> GetRetailerOptionsAsync(string? search, ulong? actorUserId, CancellationToken cancellationToken);
    Task<Customer?> GetRetailerAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, ulong? exceptId, CancellationToken cancellationToken);
    Task<NewInvoiceDto> CreateInvoiceAsync(NewInvoice invoice, CancellationToken cancellationToken);
    Task<NewInvoice?> FindInvoiceEntityAsync(ulong id, CancellationToken cancellationToken);
    Task<NewInvoiceDto> SaveInvoiceAsync(NewInvoice invoice, string statusType, int? fromStatus, int toStatus, ulong actorUserId, string? remark, CancellationToken cancellationToken);
    Task<bool> DeleteInvoiceAsync(NewInvoice invoice, CancellationToken cancellationToken);
}
