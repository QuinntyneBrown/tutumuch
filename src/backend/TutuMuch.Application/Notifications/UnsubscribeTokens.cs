using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TutuMuch.Domain.Notifications;

namespace TutuMuch.Application.Notifications;

/// <summary>Settings of the notifications feature, from the <c>Notifications</c> section.</summary>
public sealed class NotificationOptions
{
    /// <summary>The HMAC key of unsubscribe tokens; from the secret store (L2-052).</summary>
    public string? UnsubscribeSigningKey { get; set; }
}

/// <summary>A valid unsubscribe token: whose and which category.</summary>
public sealed record UnsubscribeToken(Guid MemberId, NotificationCategory Category, DateTime IssuedAt);

/// <summary>
/// Issues and checks the token in a marketing email's unsubscribe link (L2-043). A token is the
/// base64url of member ID, category and issue time, a dot, and the base64url HMAC-SHA256 of that
/// under <c>Notifications:UnsubscribeSigningKey</c>. It holds no email address and does not expire
/// until a lifetime is decided, so an old email still unsubscribes.
/// </summary>
public sealed class UnsubscribeTokens(IOptions<NotificationOptions> options)
{
    private const int PayloadLength = 16 + 1 + 8;

    public string Issue(Guid memberId, NotificationCategory category, DateTime now)
    {
        var payload = new byte[PayloadLength];
        memberId.TryWriteBytes(payload.AsSpan(0, 16));
        payload[16] = (byte)category;
        BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(17), new DateTimeOffset(now, TimeSpan.Zero).ToUnixTimeSeconds());
        return $"{Base64Url(payload)}.{Base64Url(Sign(payload))}";
    }

    /// <summary>The token's content, or null when it is malformed, signed with another key or tampered with.</summary>
    public UnsubscribeToken? Read(string? token)
    {
        var dot = token?.IndexOf('.', StringComparison.Ordinal) ?? -1;
        if (dot < 0 || string.IsNullOrEmpty(options.Value.UnsubscribeSigningKey))
        {
            return null;
        }

        var payload = FromBase64Url(token![..dot]);
        var signature = FromBase64Url(token[(dot + 1)..]);
        if (payload is not { Length: PayloadLength } || signature is null ||
            !CryptographicOperations.FixedTimeEquals(Sign(payload), signature) ||
            !Enum.IsDefined((NotificationCategory)payload[16]))
        {
            return null;
        }

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(BinaryPrimitives.ReadInt64BigEndian(payload.AsSpan(17))).UtcDateTime;
        return new UnsubscribeToken(new Guid(payload.AsSpan(0, 16)), (NotificationCategory)payload[16], issuedAt);
    }

    private byte[] Sign(byte[] payload)
    {
        var key = options.Value.UnsubscribeSigningKey;
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("Notifications:UnsubscribeSigningKey is not configured.");
        }

        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), payload);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[]? FromBase64Url(string text)
    {
        var base64 = text.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
