using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Application.Listings;

/// <summary>
/// The seller's own view of a listing, returned by every listings command. RemovalReason is
/// "Withdrawn" or "OrderCancelled" for a Removed listing. Version is not in the body: the API sends
/// it as the ETag (edit-and-withdraw-listing).
/// </summary>
public sealed record MyListingDto(
    string Reference,
    string Status,
    string Category,
    string? Subcategory,
    string Size,
    string? Title,
    string? Description,
    string? Brand,
    string? Condition,
    string? Flaws,
    decimal? Price,
    decimal? ShippingCost,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt,
    string? RemovalReason,
    IReadOnlyList<PhotoDto> Photos,
    [property: JsonIgnore] int Version)
{
    /// <summary>The listing with its photos, cover first.</summary>
    public static MyListingDto From(Listing l, IEnumerable<ListingPhoto> photos, PhotoOptions options) => new(
        l.Reference, l.Status.ToString(), l.Category, l.Subcategory, l.Size, l.Title, l.Description, l.Brand,
        l.Condition?.ToLabel(), l.Flaws, l.Price, l.ShippingCost, l.CreatedAt, l.UpdatedAt, l.PublishedAt,
        l.RemovalReason?.ToString(), photos.Select(p => PhotoDto.From(p, options)).ToList(), l.Version);
}

public sealed record PublishListingResult(string Reference, string Status, bool PayoutOnboardingRequired);

/// <summary>Creates a draft; only an accepted category is required (L2-019). The size comes from the category (L2-013).</summary>
/// <remarks>PhotoIds sets the photos in display order (L2-017); null leaves them unchanged.</remarks>
public sealed record CreateListingDraftCommand(Guid SellerId, ListingDetails Details, IReadOnlyList<Guid>? PhotoIds = null) : IRequest<Result<MyListingDto>>;

/// <summary>
/// Replaces the fields of the caller's listing. A draft takes the draft rules and needs no version.
/// An Active listing, or one removed after a cancelled order, takes the publish rules and the size
/// confirmation, and needs the version it was read at (If-Match): a missing version is 428 and a
/// stale one 412 LISTING_CHANGED. A Reserved, Sold or withdrawn listing is 409 (L2-020).
/// </summary>
public sealed record UpdateListingCommand(
    Guid SellerId,
    string Reference,
    ListingDetails Details,
    IReadOnlyList<Guid>? PhotoIds = null,
    int? ExpectedVersion = null) : IRequest<Result<MyListingDto>>;

/// <summary>Publishes the caller's draft under the L2-015 to L2-018 rules and the daily limit of L2-051.</summary>
public sealed record PublishListingCommand(Guid SellerId, string Reference) : IRequest<Result<PublishListingResult>>;

public sealed record GetMyListingQuery(Guid SellerId, string Reference) : IRequest<Result<MyListingDto>>;

public sealed class ListingCommandHandlers(
    ITuTuMuchDbContext db,
    ISellerPayoutAccountReader payouts,
    ListingPhotoAttacher photos,
    IOptions<SellingOptions> options,
    IOptions<PhotoOptions> photoOptions,
    TimeProvider clock) :
    IRequestHandler<CreateListingDraftCommand, Result<MyListingDto>>,
    IRequestHandler<UpdateListingCommand, Result<MyListingDto>>,
    IRequestHandler<PublishListingCommand, Result<PublishListingResult>>,
    IRequestHandler<GetMyListingQuery, Result<MyListingDto>>
{
    /// <summary>First listing number; references start at L-10001.</summary>
    public const long FirstNumber = 10001;

    public async Task<Result<MyListingDto>> Handle(CreateListingDraftCommand command, CancellationToken cancellationToken)
    {
        if (ToError(Listing.CheckDraft(command.Details)) is { } error)
        {
            return error;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var last = await db.Listings.MaxAsync(l => (long?)l.Number, cancellationToken);
        var listing = Listing.CreateDraft(command.SellerId, (last ?? FirstNumber - 1) + 1, command.Details, now);
        if (await photos.AttachAsync(command.SellerId, listing.Id, command.PhotoIds, cancellationToken) is { } photoError)
        {
            return photoError;
        }

        db.Listings.Add(listing);
        await db.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(listing, cancellationToken);
    }

    public async Task<Result<MyListingDto>> Handle(UpdateListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await FindOwnAsync(command.SellerId, command.Reference, cancellationToken);
        if (listing is null)
        {
            return Error.NotFound("LISTING_NOT_FOUND");
        }

        if (listing.Status != ListingStatus.Draft)
        {
            return await EditAsync(listing, command, cancellationToken);
        }

        if (ToError(Listing.CheckDraft(command.Details)) is { } error)
        {
            return error;
        }

        if (await photos.AttachAsync(command.SellerId, listing.Id, command.PhotoIds, cancellationToken) is { } photoError)
        {
            return photoError;
        }

        listing.UpdateDetails(command.Details, clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(listing, cancellationToken);
    }

    public async Task<Result<PublishListingResult>> Handle(PublishListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await FindOwnAsync(command.SellerId, command.Reference, cancellationToken);
        if (listing is null)
        {
            return Error.NotFound("LISTING_NOT_FOUND");
        }

        if (listing.Status != ListingStatus.Draft)
        {
            return NotDraft;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var dayStart = now.Date;
        var publishedToday = await db.Listings.CountAsync(
            l => l.SellerId == command.SellerId && l.PublishedAt >= dayStart, cancellationToken);
        if (publishedToday >= options.Value.PublishesPerDay)
        {
            return Error.TooManyRequests(dayStart.AddDays(1) - now, $"That's {options.Value.PublishesPerDay} listings today");
        }

        var photoCount = await db.ListingPhotos.CountAsync(p => p.ListingId == listing.Id, cancellationToken);
        if (ToError(listing.Publish(now, photoCount)) is { } error)
        {
            return error;
        }

        await db.SaveChangesAsync(cancellationToken);
        var onboarded = await payouts.IsOnboardedAsync(command.SellerId, cancellationToken);
        return new PublishListingResult(listing.Reference, listing.Status.ToString(), !onboarded);
    }

    public async Task<Result<MyListingDto>> Handle(GetMyListingQuery query, CancellationToken cancellationToken)
    {
        var listing = await FindOwnAsync(query.SellerId, query.Reference, cancellationToken);
        return listing is null ? Error.NotFound("LISTING_NOT_FOUND") : await ToDtoAsync(listing, cancellationToken);
    }

    /// <summary>
    /// Edits a published listing. The save carries the version as its concurrency token, so a
    /// checkout that reserved the listing after it was read wins and the edit is refused (L2-029).
    /// </summary>
    private async Task<Result<MyListingDto>> EditAsync(Listing listing, UpdateListingCommand command, CancellationToken cancellationToken)
    {
        if (ListingRefusals.For(listing) is { } refusal)
        {
            return refusal;
        }

        if (command.ExpectedVersion is not { } expected)
        {
            return ListingRefusals.PreconditionRequired;
        }

        if (expected != listing.Version)
        {
            return ListingRefusals.Changed;
        }

        if (await photos.AttachAsync(command.SellerId, listing.Id, command.PhotoIds, cancellationToken) is { } photoError)
        {
            return photoError;
        }

        var photoCount = command.PhotoIds?.Count ?? await db.ListingPhotos.CountAsync(p => p.ListingId == listing.Id, cancellationToken);
        if (ToError(listing.Edit(command.Details, photoCount, clock.GetUtcNow().UtcDateTime)) is { } error)
        {
            return error;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await ListingRefusals.AfterConflictAsync(db, listing.Id, cancellationToken);
        }

        return await ToDtoAsync(listing, cancellationToken);
    }

    private async Task<MyListingDto> ToDtoAsync(Listing listing, CancellationToken cancellationToken) =>
        MyListingDto.From(listing, await photos.PhotosOfAsync(listing.Id, cancellationToken), photoOptions.Value);

    private static readonly Error NotDraft = new(409, "LISTING_NOT_DRAFT", "This listing is not a draft");

    /// <summary>Another member's listing answers as missing so its existence is not revealed (L2-010).</summary>
    private Task<Listing?> FindOwnAsync(Guid sellerId, string reference, CancellationToken cancellationToken) =>
        db.Listings.SingleOrDefaultAsync(l => l.Reference == reference && l.SellerId == sellerId, cancellationToken);

    /// <summary>
    /// Maps rule violations to a 422. The top-level code is CATEGORY_INVALID or SIZE_NOT_ACCEPTED
    /// when those rules fail, the field's own code when only one field fails, else LISTING_INVALID.
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
            : "LISTING_INVALID";
        return Error.Invalid(code, errors);
    }
}
