using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Catalog;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Listings;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Application.Requests;

/// <summary>
/// One of the member's item requests (L2-099). Status is "Open" or "Expired"; MatchCount counts
/// every match ever recorded, Matches holds the matched listings still Active, newest match first
/// (L2-102), so a request whose matches all went says "no longer available".
/// </summary>
public sealed record ItemRequestDto(
    Guid Id,
    string Category,
    string Size,
    string Keywords,
    decimal? MaxPrice,
    string? MinimumCondition,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime ExpiresAt,
    string Status,
    int MatchCount,
    IReadOnlyList<ListingCardDto> Matches);

/// <summary>The member's requests newest first, how many are open and the limit (L2-098).</summary>
public sealed record MyItemRequestsDto(IReadOnlyList<ItemRequestDto> Items, int OpenCount, int Limit);

/// <summary>The refusals of the item request endpoints.</summary>
public static class ItemRequestRefusals
{
    /// <summary>Unknown, or another member's: the same answer, so a request's existence is not revealed (L2-099).</summary>
    public static readonly Error NotFound = Error.NotFound("REQUEST_NOT_FOUND");

    public static readonly Error LimitReached = new(409, "REQUEST_LIMIT_REACHED", $"You have {ItemRequest.MaxOpenPerMember} open requests");
}

/// <summary>Saves a new open request (L2-097) when the member has fewer than 20 open (L2-098).</summary>
public sealed record CreateItemRequestCommand(Guid MemberId, ItemRequestDetails Details) : IRequest<Result<ItemRequestDto>>;

/// <summary>Replaces the fields of the member's request and renews it for 90 days; renewing an expired one counts toward the limit (L2-098).</summary>
public sealed record UpdateItemRequestCommand(Guid MemberId, Guid Id, ItemRequestDetails Details) : IRequest<Result<ItemRequestDto>>;

/// <summary>Deletes the member's request with its matches (L2-099).</summary>
public sealed record DeleteItemRequestCommand(Guid MemberId, Guid Id) : IRequest<Result<bool>>;

public sealed record GetMyItemRequestsQuery(Guid MemberId) : IRequest<MyItemRequestsDto>;

public sealed record GetMyItemRequestQuery(Guid MemberId, Guid Id) : IRequest<Result<ItemRequestDto>>;

/// <summary>
/// The member's own item requests (L1-023). Every lookup is by the member's ID from the session as
/// well as the request ID (L2-010), so another member's request answers 404.
/// </summary>
public sealed class ItemRequestCommandHandlers(ITuTuMuchDbContext db, IOptions<PhotoOptions> photoOptions, TimeProvider clock) :
    IRequestHandler<CreateItemRequestCommand, Result<ItemRequestDto>>,
    IRequestHandler<UpdateItemRequestCommand, Result<ItemRequestDto>>,
    IRequestHandler<DeleteItemRequestCommand, Result<bool>>,
    IRequestHandler<GetMyItemRequestsQuery, MyItemRequestsDto>,
    IRequestHandler<GetMyItemRequestQuery, Result<ItemRequestDto>>
{
    public async Task<Result<ItemRequestDto>> Handle(CreateItemRequestCommand command, CancellationToken cancellationToken)
    {
        if (ToError(ItemRequest.Check(command.Details)) is { } error)
        {
            return error;
        }

        var now = Now();
        if (await CountOpenAsync(command.MemberId, now, cancellationToken) >= ItemRequest.MaxOpenPerMember)
        {
            return ItemRequestRefusals.LimitReached;
        }

        var request = ItemRequest.Create(command.MemberId, command.Details, now);
        db.ItemRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
        return (await ToDtosAsync([request], now, cancellationToken))[0];
    }

    public async Task<Result<ItemRequestDto>> Handle(UpdateItemRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await FindOwnAsync(command.MemberId, command.Id, cancellationToken);
        if (request is null)
        {
            return ItemRequestRefusals.NotFound;
        }

        if (ToError(ItemRequest.Check(command.Details)) is { } error)
        {
            return error;
        }

        var now = Now();
        if (!request.IsOpen(now) && await CountOpenAsync(command.MemberId, now, cancellationToken) >= ItemRequest.MaxOpenPerMember)
        {
            return ItemRequestRefusals.LimitReached;
        }

        request.Update(command.Details, now);
        await db.SaveChangesAsync(cancellationToken);
        return (await ToDtosAsync([request], now, cancellationToken))[0];
    }

    public async Task<Result<bool>> Handle(DeleteItemRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await FindOwnAsync(command.MemberId, command.Id, cancellationToken);
        if (request is null)
        {
            return ItemRequestRefusals.NotFound;
        }

        db.ItemRequestMatches.RemoveRange(await db.ItemRequestMatches.Where(m => m.ItemRequestId == request.Id).ToListAsync(cancellationToken));
        db.ItemRequests.Remove(request);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MyItemRequestsDto> Handle(GetMyItemRequestsQuery query, CancellationToken cancellationToken)
    {
        var now = Now();
        var requests = await db.ItemRequests.AsNoTracking()
            .Where(r => r.MemberId == query.MemberId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .ToListAsync(cancellationToken);
        var items = await ToDtosAsync(requests, now, cancellationToken);
        return new MyItemRequestsDto(items, requests.Count(r => r.IsOpen(now)), ItemRequest.MaxOpenPerMember);
    }

    public async Task<Result<ItemRequestDto>> Handle(GetMyItemRequestQuery query, CancellationToken cancellationToken)
    {
        var request = await FindOwnAsync(query.MemberId, query.Id, cancellationToken);
        return request is null ? ItemRequestRefusals.NotFound : (await ToDtosAsync([request], Now(), cancellationToken))[0];
    }

    /// <summary>Each request with its match count and the cards of its matched listings that are still Active.</summary>
    private async Task<List<ItemRequestDto>> ToDtosAsync(IReadOnlyList<ItemRequest> requests, DateTime now, CancellationToken cancellationToken)
    {
        var ids = requests.Select(r => r.Id).ToList();
        var matches = await db.ItemRequestMatches.AsNoTracking()
            .Where(m => ids.Contains(m.ItemRequestId))
            .ToListAsync(cancellationToken);
        var listingIds = matches.Select(m => m.ListingId).Distinct().ToList();
        var active = await db.Listings.AsNoTracking()
            .Where(l => listingIds.Contains(l.Id) && l.Status == ListingStatus.Active)
            .ToListAsync(cancellationToken);
        var cards = (await CatalogCards.ToCardsAsync(db, active, photoOptions.Value, cancellationToken)).ToDictionary(c => c.Id);
        return requests.Select(r =>
        {
            var own = matches.Where(m => m.ItemRequestId == r.Id).ToList();
            var shown = own.OrderByDescending(m => m.MatchedAt)
                .Where(m => cards.ContainsKey(m.ListingId))
                .Select(m => cards[m.ListingId])
                .ToList();
            return new ItemRequestDto(
                r.Id, r.Category, r.Size, r.Keywords, r.MaxPrice, r.MinimumCondition?.ToLabel(), r.CreatedAt, r.UpdatedAt, r.ExpiresAt,
                r.IsOpen(now) ? "Open" : "Expired", own.Count, shown);
        }).ToList();
    }

    private Task<int> CountOpenAsync(Guid memberId, DateTime now, CancellationToken cancellationToken) =>
        db.ItemRequests.CountAsync(r => r.MemberId == memberId && r.ExpiresAt > now, cancellationToken);

    private Task<ItemRequest?> FindOwnAsync(Guid memberId, Guid id, CancellationToken cancellationToken) =>
        db.ItemRequests.SingleOrDefaultAsync(r => r.Id == id && r.MemberId == memberId, cancellationToken);

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Maps rule violations to a 422 as the listing form does: CATEGORY_INVALID or SIZE_NOT_ACCEPTED
    /// when those rules fail, the field's own code when only one field fails, else REQUEST_INVALID.
    /// </summary>
    private static Error? ToError(IReadOnlyList<RuleViolation> violations)
    {
        if (violations.Count == 0)
        {
            return null;
        }

        var errors = violations.ToDictionary(v => v.Field, v => new FieldError(v.Code, v.Message));
        var code = violations.Any(v => v.Code == "CATEGORY_INVALID") ? "CATEGORY_INVALID"
            : violations.Any(v => v.Code == "SIZE_NOT_ACCEPTED") ? "SIZE_NOT_ACCEPTED"
            : violations.Count == 1 ? violations[0].Code
            : "REQUEST_INVALID";
        return Error.Invalid(code, errors);
    }
}
