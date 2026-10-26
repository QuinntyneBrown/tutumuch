using Microsoft.Extensions.Logging;
using TutuMuch.Application.Abstractions;

namespace TutuMuch.Infrastructure.Email;

/// <summary>
/// Writes emails to the log instead of sending them. The send-email-notifications feature
/// replaces this adapter with the email provider.
/// </summary>
internal sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Log(logger, message.To, message.Subject, message.Body);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email to {To}: {Subject}\n{Body}")]
    private static partial void Log(ILogger logger, string to, string subject, string body);
}
