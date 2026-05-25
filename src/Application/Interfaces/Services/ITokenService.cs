using Domain.Entities;

namespace Application.Interfaces.Services;

public interface ITokenService
{
    string CreateAccessToken(string provider, ulong subjectId, string name, IEnumerable<string> roles, out string tokenId, DateTime? expiresAt = null);
}
