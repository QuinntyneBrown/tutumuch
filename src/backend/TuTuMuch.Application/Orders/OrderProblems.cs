using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Listings;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Domain.Orders;
using TuTuMuch.Domain.Payouts;

namespace TuTuMuch.Application.Orders;

/// <summary>Errors of the problem endpoints (L2-036).</summary>
public static class ProblemErrors
{
    public static readonly Error WindowClosed = new(409, "PROBLEM_WINDOW_CLOSED", "Problems can be reported until 3 days after delivery");

    public static readonly Error AlreadyReported = new(409, "PROBLEM_ALREADY_REPORTED", "You already reported a problem with this order");

    public static readonly Error ResponseNotAllowed = new(409, "PROBLEM_RESPONSE_NOT_ALLOWED", "You can reply once while the problem is open");

    public static readonly Error ProblemOpen = new(409, "ORDER_PROBLEM_OPEN", "This order has an open problem; our team decides it");

    public static readonly Error AlreadyResolved = new(409, "PROBLEM_ALREADY_RESOLVED", "This problem is already resolved");

    public static Error NotFound => Error.NotFound("PROBLEM_NOT_FOUND");

    public static Error Invalid(string code, IReadOnlyDictionary<string, FieldError> errors) => Error.Invalid(code, errors);

    /// <summary>The trimmed text, null when blank or longer than <see cref="OrderProblem.MaxText"/>.</summary>
    public static string? Clean(string? text) =>
        string.IsNullOrWhiteSpace(text) || text.Trim().Length > OrderProblem.MaxText ? null : text.Trim();
}

/// <summary>One private evidence photo, served through the API to the buyer, the seller and administrators.</summary>
public sealed record ProblemPhotoDto(Guid Id, string Url);

/// <summary>A problem as the buyer, the seller and administrators see it: the claim, the seller's reply and the decision.</summary>
public sealed record ProblemDto(
    string Reference,
    string Reason,
    string ReasonLabel,
    string Description,
    IReadOnlyList<ProblemPhotoDto> Photos,
    string Status,
    DateTime OpenedAt,
    string? SellerResponse,
    DateTime? SellerRespondedAt,
    string? Outcome,
    string? ResolutionReason,
    DateTime? ResolvedAt)
{
    public static ProblemDto From(OrderProblem problem) => new(
        problem.Reference,
        problem.Reason.ToString(),
        problem.Reason.ToLabel(),
        problem.Description,
        [.. problem.Photos.OrderBy(p => p.Position).Select(p => new ProblemPhotoDto(p.Id, $"/api/orders/{problem.OrderId}/problem/photos/{p.Id}"))],
        problem.Status.ToString(),
        problem.OpenedAt,
        problem.SellerResponse,
        problem.SellerRespondedAt,
        problem.Outcome?.ToString(),
        problem.ResolutionReason,
        problem.ResolvedAt);
}

public static class ProblemViews
{
    public static async Task<ProblemDto?> ForOrderAsync(ITuTuMuchDbContext db, Guid orderId, CancellationToken cancellationToken)
    {
        var problem = await db.OrderProblems.AsNoTracking().Include(p => p.Photos).SingleOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
        return problem is null ? null : ProblemDto.From(problem);
    }

    public static Task<bool> IsOpenAsync(ITuTuMuchDbContext db, Guid orderId, CancellationToken cancellationToken) =>
        db.OrderProblems.AnyAsync(p => p.OrderId == orderId && p.Status == ProblemStatus.Open, cancellationToken);
}

/// <summary>One uploaded evidence photo before it is checked.</summary>
public sealed record ProblemPhotoUpload(byte[] Content);

/// <summary>
/// The buyer reports a problem on a Shipped order, or a Delivered order within 3 days of delivery
/// (L2-036 AC1, AC2), with a reason, a description and up to 4 photos.
/// </summary>
public sealed record ReportProblemCommand(Guid BuyerId, Guid OrderId, string? Reason, string? Description, IReadOnlyList<ProblemPhotoUpload> Photos) : IRequest<Result<ProblemDto>>;

/// <summary>
/// Validates, then checks each photo's size and sniffed type as listing photos do (L2-017, without
/// the 800-pixel minimum, ADR orders/0007), strips its metadata by re-encoding it as one JPEG of at
/// most 1600 pixels and stores it privately. In one save it opens the problem, holds the payout
/// and queues the emails to the seller and to every administrator.
/// </summary>
public sealed class ReportProblemCommandHandler(
    ITuTuMuchDbContext db,
    IImageInspector inspector,
    IListingPhotoProcessor processor,
    IPhotoStorage storage,
    IEmailOutbox notifications,
    IOptions<PhotoOptions> photoOptions,
    TimeProvider clock) :
    IRequestHandler<ReportProblemCommand, Result<ProblemDto>>
{
    private const int StoredSize = 1600;

    public async Task<Result<ProblemDto>> Handle(ReportProblemCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == command.OrderId && o.BuyerId == command.BuyerId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        var errors = new Dictionary<string, FieldError>();
        if (!Enum.TryParse<ProblemReason>(command.Reason, out var reason) || !Enum.IsDefined(reason) || int.TryParse(command.Reason, out _))
        {
            errors["reason"] = new FieldError("REASON_REQUIRED", "Choose what’s wrong");
        }

        var description = ProblemErrors.Clean(command.Description);
        if (description is null)
        {
            errors["description"] = string.IsNullOrWhiteSpace(command.Description)
                ? new FieldError("DESCRIPTION_REQUIRED", "Tell us what happened")
                : new FieldError("DESCRIPTION_LENGTH", $"Keep it to {OrderProblem.MaxText} characters");
        }

        if (command.Photos.Count > OrderProblem.MaxPhotos)
        {
            errors["photos"] = new FieldError("PHOTO_LIMIT", $"Add up to {OrderProblem.MaxPhotos} photos");
        }

        if (errors.Count > 0)
        {
            return ProblemErrors.Invalid("PROBLEM_INVALID", errors);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        if (await db.OrderProblems.AnyAsync(p => p.OrderId == order.Id, cancellationToken))
        {
            return ProblemErrors.AlreadyReported;
        }

        if (!order.CanReportProblem(now))
        {
            return ProblemErrors.WindowClosed;
        }

        var settings = photoOptions.Value;
        foreach (var photo in command.Photos)
        {
            if (photo.Content.LongLength > settings.MaxBytes)
            {
                return ListingPhotoHandlers.TooLarge;
            }

            if (inspector.Inspect(photo.Content) is not { } image)
            {
                return ListingPhotoHandlers.Unsupported;
            }

            if ((long)image.Width * image.Height > settings.MaxPixels)
            {
                return ListingPhotoHandlers.TooManyPixels;
            }
        }

        var number = ((await db.OrderProblems.MaxAsync(p => (long?)p.Number, cancellationToken)) ?? OrderProblem.FirstNumber - 1) + 1;
        var photoIds = command.Photos.Select(_ => Guid.NewGuid()).ToList();
        var problem = OrderProblem.Open(number, order, reason, description!, photoIds, now);
        foreach (var (photo, stored) in command.Photos.Zip(problem.Photos))
        {
            var jpeg = processor.Process(photo.Content, [StoredSize]).First(v => v.Format == PhotoFormat.Jpeg);
            await storage.SaveOriginalAsync(stored.StorageKey, jpeg.Content, stored.ContentType, cancellationToken);
        }

        db.OrderProblems.Add(problem);
        var payout = await db.Payouts.SingleOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);
        if (payout?.Status == PayoutStatus.Held)
        {
            payout.HoldForProblem();
        }

        var title = await OrderEmails.ItemTitleAsync(db, order, cancellationToken);
        var label = reason.ToLabel();
        notifications.Enqueue(NotificationType.ProblemReported, order.SellerId, new ProblemReportedEmail(order.Id, order.Reference, problem.Reference, title, label, problem.Description, ToAdministrator: false));
        var administrators = await db.Members.Where(m => m.IsAdministrator).Select(m => m.Id).ToListAsync(cancellationToken);
        foreach (var administrator in administrators)
        {
            notifications.Enqueue(NotificationType.ProblemReported, administrator, new ProblemReportedEmail(order.Id, order.Reference, problem.Reference, title, label, problem.Description, ToAdministrator: true));
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another report on the same order, or a race for the same number, won: one problem per order.
            return ProblemErrors.AlreadyReported;
        }

        return ProblemDto.From(problem);
    }
}

/// <summary>The seller replies once while the problem is open; the buyer and administrators see it (L2-036 AC5).</summary>
public sealed record RespondToProblemCommand(Guid SellerId, Guid OrderId, string? Message) : IRequest<Result<SaleDto>>;

public sealed class RespondToProblemCommandHandler(ITuTuMuchDbContext db, TimeProvider clock, IOptions<PhotoOptions> photoOptions) :
    IRequestHandler<RespondToProblemCommand, Result<SaleDto>>
{
    public async Task<Result<SaleDto>> Handle(RespondToProblemCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.StatusChanges)
            .SingleOrDefaultAsync(o => o.Id == command.OrderId && o.SellerId == command.SellerId, cancellationToken);
        var problem = order is null ? null : await db.OrderProblems.SingleOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);
        if (order is null || problem is null)
        {
            return OrderErrors.NotFound;
        }

        var message = ProblemErrors.Clean(command.Message);
        if (message is null)
        {
            return ProblemErrors.Invalid("PROBLEM_RESPONSE_INVALID", new Dictionary<string, FieldError>
            {
                ["message"] = string.IsNullOrWhiteSpace(command.Message)
                    ? new FieldError("MESSAGE_REQUIRED", "Write your reply")
                    : new FieldError("MESSAGE_LENGTH", $"Keep it to {OrderProblem.MaxText} characters"),
            });
        }

        if (!problem.CanSellerRespond)
        {
            return ProblemErrors.ResponseNotAllowed;
        }

        problem.RecordSellerResponse(message, clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return await OrderViews.SaleAsync(db, order, photoOptions.Value, cancellationToken);
    }
}

/// <summary>A private problem photo for the order's buyer or seller, or an administrator; 404 for anyone else (L2-010).</summary>
public sealed record GetProblemPhotoQuery(Guid MemberId, Guid OrderId, Guid PhotoId) : IRequest<Result<StoredObject>>;

public sealed class GetProblemPhotoQueryHandler(ITuTuMuchDbContext db, IPhotoStorage storage) :
    IRequestHandler<GetProblemPhotoQuery, Result<StoredObject>>
{
    public async Task<Result<StoredObject>> Handle(GetProblemPhotoQuery query, CancellationToken cancellationToken)
    {
        var allowed = await db.Orders.AnyAsync(o => o.Id == query.OrderId && (o.BuyerId == query.MemberId || o.SellerId == query.MemberId), cancellationToken)
            || await db.Members.AnyAsync(m => m.Id == query.MemberId && m.IsAdministrator, cancellationToken);
        var photo = !allowed ? null : await db.OrderProblems.Where(p => p.OrderId == query.OrderId)
            .SelectMany(p => p.Photos).SingleOrDefaultAsync(p => p.Id == query.PhotoId, cancellationToken);
        var stored = photo is null ? null : await storage.ReadOriginalAsync(photo.StorageKey, cancellationToken);
        return stored is null ? Error.NotFound("PHOTO_NOT_FOUND") : stored;
    }
}

/// <summary>The order facts an administrator weighs: status, total, delivery and the item.</summary>
public sealed record ProblemOrderDto(Guid Id, string Reference, string Status, string Total, DateTime? DeliveredAt, CheckoutItemDto Item);

/// <summary>
/// The administrator's problem page (mocks pages/problem-detail): the claim with photos, the
/// seller's reply, both members' names, the order and where the payout is (Held, Releasable,
/// Cancelled...).
/// </summary>
public sealed record ProblemDetailDto(ProblemDto Problem, string BuyerDisplayName, string SellerDisplayName, ProblemOrderDto Order, string Payout);

public sealed record GetProblemDetailQuery(string Reference) : IRequest<Result<ProblemDetailDto>>;

public sealed class GetProblemDetailQueryHandler(ITuTuMuchDbContext db, IOptions<PhotoOptions> photoOptions) :
    IRequestHandler<GetProblemDetailQuery, Result<ProblemDetailDto>>
{
    public async Task<Result<ProblemDetailDto>> Handle(GetProblemDetailQuery query, CancellationToken cancellationToken)
    {
        var problem = await db.OrderProblems.AsNoTracking().Include(p => p.Photos).SingleOrDefaultAsync(p => p.Reference == query.Reference, cancellationToken);
        if (problem is null)
        {
            return ProblemErrors.NotFound;
        }

        return await ProblemDetails.ForAsync(db, problem, photoOptions.Value, cancellationToken);
    }
}

internal static class ProblemDetails
{
    public static async Task<ProblemDetailDto> ForAsync(ITuTuMuchDbContext db, OrderProblem problem, PhotoOptions photos, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == problem.OrderId, cancellationToken);
        var names = await db.Members.AsNoTracking()
            .Where(m => m.Id == order.BuyerId || m.Id == order.SellerId)
            .ToDictionaryAsync(m => m.Id, m => m.DisplayName, cancellationToken);
        var payout = await db.Payouts.AsNoTracking().Where(p => p.OrderId == order.Id).Select(p => (PayoutStatus?)p.Status).SingleOrDefaultAsync(cancellationToken);
        return new ProblemDetailDto(
            ProblemDto.From(problem),
            names[order.BuyerId],
            names[order.SellerId],
            new ProblemOrderDto(order.Id, order.Reference, order.Status.ToString(), CheckoutMoney.Format(order.Total), order.DeliveredAt, await OrderViews.ItemAsync(db, order.ListingId, photos, cancellationToken)),
            payout?.ToString() ?? "None");
    }
}

/// <summary>An administrator decides a problem with an outcome and a reason both members see (L2-036 AC3, AC4).</summary>
public sealed record ResolveProblemCommand(Guid AdministratorId, string Reference, string? Outcome, string? Reason) : IRequest<Result<ProblemDetailDto>>;

/// <summary>
/// In one save: FullRefund moves the order to Refunded, records a full refund and cancels the
/// payout; ReleaseToSeller completes a Delivered order, whose OrderCompleted event makes the
/// payout releasable. Either way the problem is resolved and both members are emailed the
/// decision; a refund is then issued through the provider with its ID as the key.
/// </summary>
public sealed class ResolveProblemCommandHandler(
    ITuTuMuchDbContext db,
    OrderCancellation refunds,
    IEmailOutbox notifications,
    IPublisher publisher,
    TimeProvider clock,
    IOptions<PhotoOptions> photoOptions,
    ILogger<ResolveProblemCommandHandler> logger) :
    IRequestHandler<ResolveProblemCommand, Result<ProblemDetailDto>>
{
    public async Task<Result<ProblemDetailDto>> Handle(ResolveProblemCommand command, CancellationToken cancellationToken)
    {
        var problem = await db.OrderProblems.Include(p => p.Photos).SingleOrDefaultAsync(p => p.Reference == command.Reference, cancellationToken);
        if (problem is null)
        {
            return ProblemErrors.NotFound;
        }

        var errors = new Dictionary<string, FieldError>();
        if (!Enum.TryParse<ProblemOutcome>(command.Outcome, out var outcome) || !Enum.IsDefined(outcome) || int.TryParse(command.Outcome, out _))
        {
            errors["outcome"] = new FieldError("OUTCOME_INVALID", "Choose a decision");
        }

        var reason = ProblemErrors.Clean(command.Reason);
        if (reason is null)
        {
            errors["reason"] = new FieldError("REASON_REQUIRED", "Add a reason. Both members see it.");
        }

        if (errors.Count > 0)
        {
            return ProblemErrors.Invalid("PROBLEM_RESOLUTION_INVALID", errors);
        }

        if (problem.Status != ProblemStatus.Open)
        {
            return ProblemErrors.AlreadyResolved;
        }

        var order = await db.Orders.Include(o => o.StatusChanges).SingleAsync(o => o.Id == problem.OrderId, cancellationToken);
        var payout = await db.Payouts.SingleOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);
        var payment = await db.Payments.SingleAsync(p => p.Id == order.PaymentId, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        Refund? refund = null;
        try
        {
            if (outcome == ProblemOutcome.FullRefund)
            {
                order.RefundForProblem(now);
                refund = Refund.ForProblem(payment, now);
                db.Refunds.Add(refund);
                if (payout?.Status == PayoutStatus.Held)
                {
                    payout.Cancel();
                }
            }
            else
            {
                order.Complete(OrderActor.Administrator, now);
            }

            problem.Resolve(outcome, refund?.Amount, reason!, command.AdministratorId, now);
            var title = await OrderEmails.ItemTitleAsync(db, order, cancellationToken);
            var decision = outcome == ProblemOutcome.FullRefund ? "refund the buyer in full" : "release the payment to the seller";
            notifications.Enqueue(NotificationType.ProblemResolved, order.BuyerId, new ProblemResolvedEmail(order.Id, order.Reference, title, decision, reason!, ToSeller: false));
            notifications.Enqueue(NotificationType.ProblemResolved, order.SellerId, new ProblemResolvedEmail(order.Id, order.Reference, title, decision, reason!, ToSeller: true));
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOrderTransitionException or DbUpdateConcurrencyException)
        {
            // Release of a Shipped order (L2-032 has no Shipped to Completed), or the order changed meanwhile.
            return OrderErrors.InvalidTransition;
        }

        logger.LogInformation(
            "Administrator {AdministratorId} resolved problem {ProblemReference} on order {OrderReference} with {Outcome}",
            command.AdministratorId,
            problem.Reference,
            order.Reference,
            outcome);
        if (refund is not null)
        {
            await refunds.IssueRefundAsync(order, refund, payment, cancellationToken);
        }
        else
        {
            await publisher.Publish(OrderCompleted.From(order), cancellationToken);
        }

        return await ProblemDetails.ForAsync(db, problem, photoOptions.Value, cancellationToken);
    }
}
