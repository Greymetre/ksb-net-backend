using Application.DTOs.MasterData;
using Application.DTOs.Users;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IUserService
{
    Task<LaravelApiResponse> GetUsersAsync(UserListFiltersDto filters, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetUserAsync(ulong id, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetUserOptionsAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> CreateUserAsync(UserRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> UpdateUserAsync(ulong id, UserRequestDto request, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SetUserActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteUserAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken);
    Task<MasterDataFileDto> ExportUsersAsync(UserExportFiltersDto filters, CancellationToken cancellationToken);
    Task<MasterDataFileDto> GetUserTemplateAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> UploadUsersAsync(Stream fileStream, ulong? actorUserId, CancellationToken cancellationToken);
}
