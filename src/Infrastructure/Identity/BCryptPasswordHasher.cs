using Application.Interfaces.Services;

namespace Infrastructure.Identity;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
}
