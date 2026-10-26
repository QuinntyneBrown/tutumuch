using System.Collections.Concurrent;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Records every email instead of sending it, or fails every send with <see cref="FailWith"/>.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    /// <summary>When set, every send throws it, as the provider would.</summary>
    public Exception? FailWith { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (FailWith is not null)
        {
            return Task.FromException(FailWith);
        }

        Sent.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>The verification token from the last email sent to <paramref name="to"/>.</summary>
    public string LastVerificationToken(string to)
    {
        var message = Sent.Last(m => string.Equals(m.To, to, StringComparison.OrdinalIgnoreCase));
        var start = message.Body.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
        var end = message.Body.IndexOfAny([' ', '\n', '\r', '"'], start);
        return Uri.UnescapeDataString(end < 0 ? message.Body[start..] : message.Body[start..end]);
    }
}
