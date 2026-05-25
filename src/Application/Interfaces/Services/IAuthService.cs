using Application.DTOs.Auth;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface IAuthService
{
    Task<LaravelApiResponse> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SignupAsync(SignupRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> CustomerSignupAsync(CustomerSignupRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> LogoutAsync(string tokenId, string provider, ulong subjectId, CancellationToken cancellationToken);
}
