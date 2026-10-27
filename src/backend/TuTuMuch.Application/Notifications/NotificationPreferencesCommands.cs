using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Application.Notifications;

/// <summary>One category on the settings page; locked categories cannot be turned off (L2-043).</summary>
public sealed record NotificationCategoryDto(string Key, bool Enabled, bool Locked);

/// <summary>The member's 5 categories in page order, and the version to send back in If-Match.</summary>
public sealed record NotificationPreferencesDto(IReadOnlyList<NotificationCategoryDto> Categories)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public int Version { get; init; }

    public static NotificationPreferencesDto From(NotificationPreferences preferences) => new(
    [
        new("accountSecurity", true, true),
        new("orders", true, true),
        new("itemRequests", preferences.ItemRequestEmails, false),
        new("reviews", preferences.ReviewEmails, false),
        new("marketing", preferences.MarketingEmails, false),
    ])
    { Version = preferences.Version };
}

/// <summary>The refusals of the settings and unsubscribe endpoints.</summary>
public static class NotificationRefusals
{
    public static readonly Error Changed = new(412, "NOTIFICATION_PREFERENCES_CHANGED", "Your email settings changed since you opened them");
    public static readonly Error PreconditionRequired = new(428, "PRECONDITION_REQUIRED", "Send the version you read in If-Match");
    public static readonly Error UnsubscribeInvalid = new(400, "UNSUBSCRIBE_LINK_INVALID", "This unsubscribe link isn't valid");
}

/// <summary>Reads whether a member wants email of a category; the dispatcher asks before every send (L2-043).</summary>
public interface INotificationPreferenceReader
{
    Task<bool> IsAllowedAsync(Guid memberId, NotificationCategory category, CancellationToken cancellationToken);
}

/// <summary>Reads the stored row; a member without one has the defaults.</summary>
public sealed class NotificationPreferenceReader(ITuTuMuchDbContext db, TimeProvider clock) : INotificationPreferenceReader
{
    public async Task<bool> IsAllowedAsync(Guid memberId, NotificationCategory category, CancellationToken cancellationToken)
    {
        var preferences = await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(p => p.MemberId == memberId, cancellationToken)
            ?? NotificationPreferences.CreateDefault(memberId, clock.GetUtcNow().UtcDateTime);
        return preferences.IsAllowed(category);
    }
}

/// <summary>The calling member's settings; the defaults when they never saved any.</summary>
public sealed record GetNotificationPreferencesQuery(Guid MemberId) : IRequest<NotificationPreferencesDto>;

public sealed class GetNotificationPreferencesQueryHandler(ITuTuMuchDbContext db, TimeProvider clock)
    : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferencesDto>
{
    public async Task<NotificationPreferencesDto> Handle(GetNotificationPreferencesQuery query, CancellationToken cancellationToken)
    {
        var preferences = await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(p => p.MemberId == query.MemberId, cancellationToken)
            ?? NotificationPreferences.CreateDefault(query.MemberId, clock.GetUtcNow().UtcDateTime);
        return NotificationPreferencesDto.From(preferences);
    }
}

/// <summary>
/// Saves the optional choices under the version they were read at (If-Match): a missing version
/// is 428, an old one 412. There is no field for the always-on categories, so no request can turn
/// them off; review and marketing are required (422), item requests is kept when absent (L2-103).
/// </summary>
public sealed record UpdateNotificationPreferencesCommand(Guid MemberId, bool? ReviewEmails, bool? MarketingEmails, int? ExpectedVersion, bool? ItemRequestEmails = null)
    : IRequest<Result<NotificationPreferencesDto>>;

public sealed class UpdateNotificationPreferencesCommandHandler(ITuTuMuchDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateNotificationPreferencesCommand, Result<NotificationPreferencesDto>>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(UpdateNotificationPreferencesCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, FieldError>();
        if (command.ReviewEmails is null)
        {
            errors["reviewEmails"] = new("REQUIRED", "Choose on or off");
        }

        if (command.MarketingEmails is null)
        {
            errors["marketingEmails"] = new("REQUIRED", "Choose on or off");
        }

        if (errors.Count > 0)
        {
            return Error.Invalid("NOTIFICATION_PREFERENCES_INVALID", errors);
        }

        if (command.ExpectedVersion is not { } expected)
        {
            return NotificationRefusals.PreconditionRequired;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var preferences = await db.NotificationPreferences.SingleOrDefaultAsync(p => p.MemberId == command.MemberId, cancellationToken);
        if (preferences is null)
        {
            preferences = NotificationPreferences.CreateDefault(command.MemberId, now);
            db.NotificationPreferences.Add(preferences);
        }

        if (preferences.Version != expected)
        {
            return NotificationRefusals.Changed;
        }

        preferences.Update(command.ReviewEmails!.Value, command.MarketingEmails!.Value, now, command.ItemRequestEmails);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another save won the race: a changed version, or a first row inserted twice.
            return NotificationRefusals.Changed;
        }

        return NotificationPreferencesDto.From(preferences);
    }
}

public sealed record UnsubscribeResult(string Category, bool Unsubscribed);

/// <summary>
/// The one-click unsubscribe (L2-043, L2-103): a valid token turns the member's marketing or item
/// request emails off, whichever category it names, with no session. It answers the same for a member already unsubscribed, or one who no longer exists, so
/// it is idempotent and never says whose token it was.
/// </summary>
public sealed record UnsubscribeCommand(string? Token) : IRequest<Result<UnsubscribeResult>>;

public sealed class UnsubscribeCommandHandler(ITuTuMuchDbContext db, UnsubscribeTokens tokens, TimeProvider clock)
    : IRequestHandler<UnsubscribeCommand, Result<UnsubscribeResult>>
{
    public async Task<Result<UnsubscribeResult>> Handle(UnsubscribeCommand command, CancellationToken cancellationToken)
    {
        if (tokens.Read(command.Token) is not { Category: NotificationCategory.Marketing or NotificationCategory.ItemRequests } token)
        {
            return NotificationRefusals.UnsubscribeInvalid;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var preferences = await db.NotificationPreferences.SingleOrDefaultAsync(p => p.MemberId == token.MemberId, cancellationToken);
        if (preferences is null && token.Category == NotificationCategory.ItemRequests && await db.Members.AnyAsync(m => m.Id == token.MemberId, cancellationToken))
        {
            // Item request emails default to on, so a member with no row needs one to turn them off.
            preferences = NotificationPreferences.CreateDefault(token.MemberId, now);
            db.NotificationPreferences.Add(preferences);
        }

        if (preferences is not null && preferences.IsAllowed(token.Category))
        {
            preferences.Unsubscribe(token.Category, now);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A settings save landed in between, or a first row was inserted twice; unsubscribing wins.
                db.ChangeTracker.Clear();
                preferences = await db.NotificationPreferences.SingleAsync(p => p.MemberId == token.MemberId, cancellationToken);
                preferences.Unsubscribe(token.Category, now);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return new UnsubscribeResult(token.Category.ToString(), true);
    }
}
