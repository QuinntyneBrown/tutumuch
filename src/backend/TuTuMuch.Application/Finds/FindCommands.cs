using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Finds;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Application.Finds;

/// <summary>The refusals of the shelf find endpoints.</summary>
public static class FindRefusals
{
    /// <summary>Unknown, withdrawn, another member's (for poster actions) or a closed poster's find: one answer (L2-010).</summary>
    public static readonly Error NotFound = Error.NotFound("FIND_NOT_FOUND");

    public static readonly Error GrabOfferNotAllowed = new(422, "GRAB_OFFER_NOT_ALLOWED", "Only TuTuMuch can offer to grab a find");

    public static readonly Error GrabInProgress = new(409, "GRAB_IN_PROGRESS", "TuTuMuch is grabbing this one for a shopper");
}

/// <summary>Posts a find for the poster from the session (L2-113). The CanSell policy has already checked the phase and the email.</summary>
public sealed record PostFindCommand(Guid PosterId, ShelfFindDetails Details) : IRequest<Result<FindDetailDto>>;

/// <summary>A page of live finds, newest first (L2-114).</summary>
public sealed record BrowseFindsQuery(int Page, int? PageSize) : IRequest<Result<FindPageDto>>;

/// <summary>One find; the viewer is null for a visitor (L2-115).</summary>
public sealed record GetFindQuery(string Reference, Guid? ViewerId) : IRequest<Result<FindDetailDto>>;

/// <summary>The poster marks their Active find gone (L2-116).</summary>
public sealed record MarkFindGoneCommand(Guid PosterId, string Reference) : IRequest<Result<FindDetailDto>>;

/// <summary>The poster withdraws their find (L2-116).</summary>
public sealed record WithdrawFindCommand(Guid PosterId, string Reference) : IRequest<Result<bool>>;

/// <summary>Shelf finds (L1-025, ADR catalog/0009). The poster and viewer come from the session, never the request (L2-010).</summary>
public sealed class FindCommandHandlers(ITuTuMuchDbContext db, TimeProvider clock) :
    IRequestHandler<PostFindCommand, Result<FindDetailDto>>,
    IRequestHandler<BrowseFindsQuery, Result<FindPageDto>>,
    IRequestHandler<GetFindQuery, Result<FindDetailDto>>,
    IRequestHandler<MarkFindGoneCommand, Result<FindDetailDto>>,
    IRequestHandler<WithdrawFindCommand, Result<bool>>
{
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 48;

    /// <summary>First find number; references start at F-1001.</summary>
    public const long FirstNumber = 1001;

    public async Task<Result<FindDetailDto>> Handle(PostFindCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var violations = ShelfFind.Check(command.Details, now);
        if (violations.Count > 0)
        {
            var errors = violations.ToDictionary(v => v.Field, v => new FieldError(v.Code, v.Message));
            var code = violations.Any(v => v.Code == "CATEGORY_NOT_SUPPORTED") ? "CATEGORY_NOT_SUPPORTED"
                : violations.Count == 1 ? violations[0].Code
                : "FIND_INVALID";
            return Error.Invalid(code, errors);
        }

        var poster = await db.Members.AsNoTracking().SingleAsync(m => m.Id == command.PosterId, cancellationToken);
        if (command.Details.GrabFee is not null && !poster.IsAdministrator)
        {
            return FindRefusals.GrabOfferNotAllowed;
        }

        var dayStart = now.Date;
        var today = await db.ShelfFinds.CountAsync(f => f.PosterId == command.PosterId && f.CreatedAt >= dayStart, cancellationToken);
        if (today >= ShelfFind.PostsPerDay)
        {
            return Error.TooManyRequests(dayStart.AddDays(1) - now, $"That's {ShelfFind.PostsPerDay} finds today");
        }

        var last = await db.ShelfFinds.MaxAsync(f => (long?)f.Number, cancellationToken);
        var find = ShelfFind.Post(command.PosterId, (last ?? FirstNumber - 1) + 1, command.Details, now);
        db.ShelfFinds.Add(find);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(find, poster.DisplayName, grabOpen: false, mine: true, now);
    }

    public async Task<Result<FindPageDto>> Handle(BrowseFindsQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1 || query.PageSize is < 1)
        {
            return new Error(400, "PAGE_INVALID", "Choose a page from 1");
        }

        var pageSize = Math.Min(query.PageSize ?? DefaultPageSize, MaxPageSize);
        var now = clock.GetUtcNow().UtcDateTime;
        var firstShown = DateOnly.FromDateTime(now).AddDays(-ShelfFind.ShownForDays);
        var live = db.ShelfFinds.AsNoTracking()
            .Where(f => f.Status == ShelfFindStatus.Active && f.SeenOn >= firstShown)
            .Where(f => db.Members.Any(m => m.Id == f.PosterId && m.ClosedAt == null));
        var total = await live.CountAsync(cancellationToken);
        var rows = await live
            .OrderByDescending(f => f.CreatedAt).ThenByDescending(f => f.Number)
            .Skip((query.Page - 1) * pageSize).Take(pageSize)
            .Select(f => new
            {
                Find = f,
                GrabOpen = db.Grabs.Any(g => g.FindId == f.Id && (g.Status == GrabStatus.PaymentPending || g.Status == GrabStatus.Requested || g.Status == GrabStatus.Accepted || g.Status == GrabStatus.Bought)),
            })
            .ToListAsync(cancellationToken);
        var items = rows
            .Select(r => new FindCardDto(
                r.Find.Reference,
                r.Find.Title,
                r.Find.Category,
                CheckoutMoney.Format(r.Find.ShelfPrice),
                r.Find.StoreName,
                r.Find.SeenOn,
                r.Find.OffersGrab && !r.GrabOpen))
            .ToList();
        return new FindPageDto(items, query.Page, pageSize, total);
    }

    public async Task<Result<FindDetailDto>> Handle(GetFindQuery query, CancellationToken cancellationToken)
    {
        var row = await db.ShelfFinds.AsNoTracking()
            .Where(f => f.Reference == query.Reference && f.Status != ShelfFindStatus.Withdrawn)
            .Join(db.Members.Where(m => m.ClosedAt == null), f => f.PosterId, m => m.Id, (f, m) => new { Find = f, m.DisplayName })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return FindRefusals.NotFound;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        return ToDto(row.Find, row.DisplayName, await HasOpenGrabAsync(row.Find.Id, cancellationToken), row.Find.PosterId == query.ViewerId, now);
    }

    public async Task<Result<FindDetailDto>> Handle(MarkFindGoneCommand command, CancellationToken cancellationToken)
    {
        var find = await FindOwnAsync(command.PosterId, command.Reference, cancellationToken);
        if (find is null)
        {
            return FindRefusals.NotFound;
        }

        if (await HasOpenGrabAsync(find.Id, cancellationToken))
        {
            return FindRefusals.GrabInProgress;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        find.MarkGone(now);
        await db.SaveChangesAsync(cancellationToken);
        var poster = await db.Members.AsNoTracking().Where(m => m.Id == find.PosterId).Select(m => m.DisplayName).SingleAsync(cancellationToken);
        return ToDto(find, poster, grabOpen: false, mine: true, now);
    }

    public async Task<Result<bool>> Handle(WithdrawFindCommand command, CancellationToken cancellationToken)
    {
        var find = await FindOwnAsync(command.PosterId, command.Reference, cancellationToken);
        if (find is null)
        {
            return FindRefusals.NotFound;
        }

        if (await HasOpenGrabAsync(find.Id, cancellationToken))
        {
            return FindRefusals.GrabInProgress;
        }

        find.Withdraw(clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<ShelfFind?> FindOwnAsync(Guid posterId, string reference, CancellationToken cancellationToken) =>
        db.ShelfFinds.SingleOrDefaultAsync(f => f.Reference == reference && f.PosterId == posterId && f.Status != ShelfFindStatus.Withdrawn, cancellationToken);

    private Task<bool> HasOpenGrabAsync(Guid findId, CancellationToken cancellationToken) =>
        db.Grabs.AnyAsync(g => g.FindId == findId && (g.Status == GrabStatus.PaymentPending || g.Status == GrabStatus.Requested || g.Status == GrabStatus.Accepted || g.Status == GrabStatus.Bought), cancellationToken);

    private static FindDetailDto ToDto(ShelfFind find, string posterDisplayName, bool grabOpen, bool mine, DateTime now) => new(
        find.Reference,
        find.Title,
        find.Category,
        CheckoutMoney.Format(find.ShelfPrice),
        find.StoreName,
        find.StoreAddress,
        find.SeenOn,
        find.ExpiresOn,
        find.Note,
        find.Status == ShelfFindStatus.Gone ? "Gone" : find.IsExpired(now) ? "Expired" : "Active",
        posterDisplayName,
        CheckoutMoney.Format(find.GrabFee),
        grabOpen,
        mine);
}
