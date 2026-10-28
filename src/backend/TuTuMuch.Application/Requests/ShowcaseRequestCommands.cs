using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Application.Requests;

/// <summary>Whether the listing is now a showcase listing (L2-107).</summary>
public sealed record ShowcaseListingDto(string Reference, bool Showcase);

/// <summary>The member's own request for one showcase listing (L2-109). RequestedAt is when it was first sent.</summary>
public sealed record ShowcaseRequestDto(string ListingReference, string? Note, DateTime RequestedAt, DateTime UpdatedAt);

/// <summary>A request sent and whether it was new (201) or an update (200).</summary>
public sealed record SentShowcaseRequest(ShowcaseRequestDto Request, bool Created);

/// <summary>One request on the administrator's list (L2-110); the email reaches administrators only (L2-111). RequestedAt is when it was last sent.</summary>
public sealed record AdminShowcaseRequestDto(
    string ListingReference,
    string ListingTitle,
    string MemberDisplayName,
    string MemberEmail,
    string? Note,
    DateTime RequestedAt);

/// <summary>The refusals of the showcase endpoints.</summary>
public static class ShowcaseRefusals
{
    /// <summary>Unknown, another member's, not Active or not a showcase listing: one answer (L2-010).</summary>
    public static readonly Error ListingNotFound = Error.NotFound("LISTING_NOT_FOUND");

    public static readonly Error NotAllowed = new(409, "SHOWCASE_NOT_ALLOWED", "Only a draft or active listing can be a showcase");

    public static readonly Error OwnListing = new(422, "CANNOT_REQUEST_OWN_LISTING", "You can’t request your own listing");

    public static readonly Error RequestNotFound = Error.NotFound("SHOWCASE_REQUEST_NOT_FOUND");
}

/// <summary>Marks or unmarks the administrator's own Draft or Active listing as a showcase listing (L2-107).</summary>
public sealed record SetShowcaseCommand(Guid AdministratorId, string Reference, bool Showcase) : IRequest<Result<ShowcaseListingDto>>;

/// <summary>Creates or updates the member's one request for an Active showcase listing (L2-109).</summary>
public sealed record SendShowcaseRequestCommand(Guid MemberId, string Reference, string? Note) : IRequest<Result<SentShowcaseRequest>>;

public sealed record GetMyShowcaseRequestQuery(Guid MemberId, string Reference) : IRequest<Result<ShowcaseRequestDto>>;

/// <summary>Every showcase request of an open member, most recently sent first (L2-110, L2-111).</summary>
public sealed record GetShowcaseRequestsQuery : IRequest<IReadOnlyList<AdminShowcaseRequestDto>>;

/// <summary>
/// Showcase listings and their requests (L1-024, ADR catalog/0008). The administrator and member
/// come from the session, never the request (L2-010).
/// </summary>
public sealed class ShowcaseRequestCommandHandlers(ITuTuMuchDbContext db, TimeProvider clock) :
    IRequestHandler<SetShowcaseCommand, Result<ShowcaseListingDto>>,
    IRequestHandler<SendShowcaseRequestCommand, Result<SentShowcaseRequest>>,
    IRequestHandler<GetMyShowcaseRequestQuery, Result<ShowcaseRequestDto>>,
    IRequestHandler<GetShowcaseRequestsQuery, IReadOnlyList<AdminShowcaseRequestDto>>
{
    public async Task<Result<ShowcaseListingDto>> Handle(SetShowcaseCommand command, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.SingleOrDefaultAsync(l => l.Reference == command.Reference && l.SellerId == command.AdministratorId, cancellationToken);
        if (listing is null)
        {
            return ShowcaseRefusals.ListingNotFound;
        }

        if (!listing.CanBeShowcase)
        {
            return ShowcaseRefusals.NotAllowed;
        }

        listing.SetShowcase(command.Showcase, clock.GetUtcNow().UtcDateTime);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A checkout reserved it, or the seller changed it, after it was read.
            return ShowcaseRefusals.NotAllowed;
        }

        return new ShowcaseListingDto(listing.Reference, listing.IsShowcase);
    }

    public async Task<Result<SentShowcaseRequest>> Handle(SendShowcaseRequestCommand command, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.AsNoTracking()
            .SingleOrDefaultAsync(l => l.Reference == command.Reference && l.Status == ListingStatus.Active && l.IsShowcase, cancellationToken);
        if (listing is null)
        {
            return ShowcaseRefusals.ListingNotFound;
        }

        if (listing.SellerId == command.MemberId)
        {
            return ShowcaseRefusals.OwnListing;
        }

        if (ShowcaseRequest.Check(command.Note) is [var violation])
        {
            return Error.Invalid(violation.Code, new Dictionary<string, FieldError> { [violation.Field] = new(violation.Code, violation.Message) }, violation.Message);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var request = await db.ShowcaseRequests.SingleOrDefaultAsync(r => r.MemberId == command.MemberId && r.ListingId == listing.Id, cancellationToken);
        if (request is not null)
        {
            request.Update(command.Note, now);
            await db.SaveChangesAsync(cancellationToken);
            return new SentShowcaseRequest(ToDto(request, listing.Reference), Created: false);
        }

        var dayStart = now.Date;
        var today = await db.ShowcaseRequests.CountAsync(r => r.MemberId == command.MemberId && r.CreatedAt >= dayStart, cancellationToken);
        if (today >= ShowcaseRequest.NewPerDay)
        {
            return Error.TooManyRequests(dayStart.AddDays(1) - now, $"That’s {ShowcaseRequest.NewPerDay} requests today");
        }

        request = ShowcaseRequest.Create(command.MemberId, listing.Id, command.Note, now);
        db.ShowcaseRequests.Add(request);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique index on member and listing: a second send won the race; update that one.
            db.ChangeTracker.Clear();
            var existing = await db.ShowcaseRequests.SingleAsync(r => r.MemberId == command.MemberId && r.ListingId == listing.Id, cancellationToken);
            existing.Update(command.Note, now);
            await db.SaveChangesAsync(cancellationToken);
            return new SentShowcaseRequest(ToDto(existing, listing.Reference), Created: false);
        }

        return new SentShowcaseRequest(ToDto(request, listing.Reference), Created: true);
    }

    public async Task<Result<ShowcaseRequestDto>> Handle(GetMyShowcaseRequestQuery query, CancellationToken cancellationToken)
    {
        var found = await db.ShowcaseRequests.AsNoTracking()
            .Join(db.Listings, r => r.ListingId, l => l.Id, (r, l) => new { Request = r, l.Reference })
            .SingleOrDefaultAsync(x => x.Request.MemberId == query.MemberId && x.Reference == query.Reference, cancellationToken);
        return found is null ? ShowcaseRefusals.RequestNotFound : ToDto(found.Request, found.Reference);
    }

    public async Task<IReadOnlyList<AdminShowcaseRequestDto>> Handle(GetShowcaseRequestsQuery query, CancellationToken cancellationToken)
    {
        var rows = await (
            from r in db.ShowcaseRequests.AsNoTracking()
            join l in db.Listings on r.ListingId equals l.Id
            join m in db.Members on r.MemberId equals m.Id
            where m.ClosedAt == null
            orderby r.UpdatedAt descending
            select new { r.UpdatedAt, l.Reference, l.Title, m.DisplayName, m.Email, r.Note })
            .ToListAsync(cancellationToken);
        return rows
            .Select(x => new AdminShowcaseRequestDto(x.Reference, x.Title ?? string.Empty, x.DisplayName, x.Email, x.Note, x.UpdatedAt))
            .ToList();
    }

    private static ShowcaseRequestDto ToDto(ShowcaseRequest request, string reference) =>
        new(reference, request.Note, request.CreatedAt, request.UpdatedAt);
}
