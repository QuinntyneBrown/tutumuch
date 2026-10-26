using System.Security.Cryptography;
using System.Text;

namespace TuTuMuch.Application.Accounts;

/// <summary>Random URL-safe tokens and their SHA-256 hashes; only hashes are stored.</summary>
public static class SecureTokens
{
    public static string Create() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
