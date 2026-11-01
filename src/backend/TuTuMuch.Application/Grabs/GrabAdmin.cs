using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Finds;

namespace TuTuMuch.Application.Grabs;

/// <summary>What an administrator does to a grab (L2-118).</summary>
public enum GrabAction
{
    Accept,
    Decline,
    Bought,
    Unavailable,
}

/// <summary>Every grab past payment: open ones first by their next deadline, then ended ones, newest first (L2-118).</summary>
public sealed record GetAdminGrabsQuery : IRequest<IReadOnlyList<AdminGrabDto>>;

/// <summary>Moves a grab; <paramref name="Reason"/> is needed for Unavailable only.</summary>
public sealed record ActOnGrabCommand(Guid GrabId, GrabAction Action, GrabUnavailableReason? Reason = null) : IRequest<Result<AdminGrabDto>>;

/// <summary>TuTuMuch's side of grabs, behind the Administrator policy (L2-118, ADR orders/0009).</summary>
public sealed class GrabAdminHandlers(ITuTuMuchDbContext db, IPaymentProvider provider, IEmailOutbox outbox, TimeProvider clock) :
    IRequestHandler<GetAdminGrabsQuery, IReadOnlyList<AdminGrabDto>>,
    IRequestHandler<ActOnGrabCommand, Result<AdminGrabDto>>
{
    public static readonly Error ReasonRequired = Error.Invalid(
        "UNAVAILABLE_REASON_REQUIRED",
        new Dictionary<string, FieldError> { ["reason"] = new("UNAVAILABLE_REASON_REQUIRED", "Choose why you couldn't get it") },
        "Choose why you couldn't get it");

    public async Task<IReadOnlyList<AdminGrabDto>> Handle(GetAdminGrabsQuery query, CancellationToken cancellationToken)
    {
        var rows = await Rows().ToListAsync(cancellationToken);
        return rows
            .OrderByDescending(x => x.Grab.IsOpen)
            .ThenBy(x => x.Grab.IsOpen ? x.Grab.Deadline ?? DateTime.MaxValue : DateTime.MaxValue)
            .ThenByDescending(x => x.Grab.CreatedAt)
            .Select(x => ToDto(x.Grab, x.Find, x.DisplayName, x.Email))
            .ToList();
    }

    public async Task<Result<AdminGrabDto>> Handle(ActOnGrabCommand command, CancellationToken cancellationToken)
    {
        var grab = await db.Grabs.SingleOrDefaultAsync(g => g.Id == command.GrabId && g.Status != GrabStatus.PaymentPending && g.Status != GrabStatus.PaymentFailed, cancellationToken);
        if (grab is null)
        {
            return GrabRefusals.NotFound;
        }

        if (command.Action == GrabAction.Unavailable && command.Reason is null)
        {
            return ReasonRequired;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var allowed = command.Action switch
        {
            GrabAction.Accept => grab.CanAccept(now),
            GrabAction.Decline => grab.Status == GrabStatus.Requested,
            _ => grab.Status == GrabStatus.Accepted,
        };
        if (!allowed)
        {
            return GrabRefusals.StateChanged;
        }

        var find = await db.ShelfFinds.SingleAsync(f => f.Id == grab.FindId, cancellationToken);
        decimal refundAmount = 0m;
        switch (command.Action)
        {
            case GrabAction.Accept:
                grab.Accept(now);
                break;
            case GrabAction.Decline:
                refundAmount = grab.Decline(now);
                break;
            case GrabAction.Bought:
                grab.MarkBought(now);
                find.MarkGone(now);
                break;
            default:
                refundAmount = grab.MarkUnavailable(command.Reason!.Value, now);
                find.MarkGone(now);
                break;
        }

        var refund = await GrabSupport.AddRefundAsync(db, grab, refundAmount, now, cancellationToken);
        GrabSupport.EmailShopper(outbox, grab, find.Title);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The deadline job or the shopper moved it first.
            return GrabRefusals.StateChanged;
        }

        await GrabSupport.IssueAsync(db, provider, refund, cancellationToken);
        var shopper = await db.Members.AsNoTracking().Where(m => m.Id == grab.ShopperId).Select(m => new { m.DisplayName, m.Email }).SingleAsync(cancellationToken);
        return ToDto(grab, find, shopper.DisplayName, shopper.Email);
    }

    private IQueryable<AdminGrabRow> Rows() =>
        from g in db.Grabs.AsNoTracking()
        where g.Status != GrabStatus.PaymentPending && g.Status != GrabStatus.PaymentFailed
        join f in db.ShelfFinds on g.FindId equals f.Id
        join m in db.Members on g.ShopperId equals m.Id
        select new AdminGrabRow(g, f, m.DisplayName, m.Email);

    private static AdminGrabDto ToDto(Grab grab, ShelfFind find, string displayName, string email) => new(
        grab.Id,
        grab.Reference,
        grab.Status.ToString(),
        find.Reference,
        find.Title,
        find.StoreName,
        displayName,
        email,
        CheckoutMoney.Format(grab.Total),
        CheckoutMoney.Format(grab.Deposit),
        CheckoutMoney.Format(grab.RefundedAmount),
        grab.UnavailableReason?.ToLabel(),
        grab.Deadline,
        grab.Status is GrabStatus.Accepted or GrabStatus.Bought ? GrabSupport.ToDto(grab.Address) : null,
        grab.CreatedAt);

    private sealed record AdminGrabRow(Grab Grab, ShelfFind Find, string DisplayName, string Email);
}
