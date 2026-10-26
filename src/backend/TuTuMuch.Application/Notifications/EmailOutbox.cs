using System.Text.Json;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Application.Notifications;

/// <summary>
/// Queues an email for a member. Handlers call it before <c>SaveChangesAsync</c>, so the email
/// request commits or rolls back with the change that caused it (L2-042).
/// </summary>
public interface IEmailOutbox
{
    void Enqueue<TModel>(NotificationType type, Guid recipientMemberId, TModel model)
        where TModel : class;
}

/// <summary>Adds an <see cref="OutboxMessage"/> to the current unit of work.</summary>
public sealed class EmailOutbox(ITuTuMuchDbContext db, TimeProvider clock) : IEmailOutbox
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Enqueue<TModel>(NotificationType type, Guid recipientMemberId, TModel model)
        where TModel : class
    {
        if (EmailTemplates.ModelType(type) != typeof(TModel))
        {
            throw new ArgumentException($"{type} takes a {EmailTemplates.ModelType(type).Name}.", nameof(model));
        }

        db.OutboxMessages.Add(OutboxMessage.Create(type, recipientMemberId, JsonSerializer.Serialize(model, Json), clock.GetUtcNow().UtcDateTime));
    }
}
