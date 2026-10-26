using Microsoft.AspNetCore.Identity;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Infrastructure.Accounts;

/// <summary>PBKDF2 password hashing from ASP.NET Core Identity.</summary>
internal sealed class IdentityPasswordHasher : Application.Abstractions.IPasswordHasher
{
    private static readonly object User = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(User, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(User, hash, password) != PasswordVerificationResult.Failed;
}

/// <summary>
/// A bundled list of the most common breached passwords. A k-anonymity breach API is an open
/// question of the sign-up-and-sign-in design.
/// </summary>
internal sealed class BundledBreachedPasswords : IBreachedPasswords
{
    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456789012", "password1234", "password12345", "qwertyuiop12", "qwertyuiopasdf", "1234567890123",
        "iloveyou1234", "passwordpassword", "letmein12345", "welcome12345", "administrator", "football1234",
        "baseball1234", "sunshine1234", "princess1234", "trustno11234", "abc123456789", "111111111111",
        "000000000000", "aaaaaaaaaaaa", "changeme1234", "monkey123456", "dragon123456", "superman1234",
    };

    public bool Contains(string password) => Known.Contains(password);
}
