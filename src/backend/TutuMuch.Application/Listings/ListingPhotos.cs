using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Common;
using TutuMuch.Domain.Listings;

namespace TutuMuch.Application.Listings;

/// <summary>Photo rules and delivery settings (L2-017, L2-059).</summary>
public sealed class PhotoOptions
{
    /// <summary>10 MB, read as 10 MiB.</summary>
    public long MaxBytes { get; set; } = 10 * 1024 * 1024;

    public int MinShortSidePx { get; set; } = 800;

    /// <summary>Decompression-bomb guard: 50 megapixels.</summary>
    public long MaxPixels { get; set; } = 50_000_000;

    public List<int> VersionSizes { get; set; } = [400, 800, 1600];

    /// <summary>Origin of the public versions; "/photos" serves them from Tutu Much API until a CDN exists.</summary>
    public string PublicBaseUrl { get; set; } = "/photos";
}

public sealed record PhotoVersionDto(int Size, int Width, int Height, string Format, string Url);

/// <summary>An uploaded photo; <see cref="Url"/> is the 400 px WebP thumbnail.</summary>
public sealed record PhotoDto(Guid Id, string Url, int Width, int Height, IReadOnlyList<PhotoVersionDto> Versions)
{
    public static PhotoDto From(ListingPhoto photo, PhotoOptions options)
    {
        var baseUrl = options.PublicBaseUrl.TrimEnd('/');
        var versions = photo.Versions
            .OrderBy(v => v.Size).ThenBy(v => v.Format)
            .Select(v => new PhotoVersionDto(v.Size, v.Width, v.Height, v.Format.ToString(), $"{baseUrl}/{v.Key}"))
            .ToList();
        var thumbnail = versions.FirstOrDefault(v => v.Format == nameof(PhotoFormat.WebP))?.Url ?? string.Empty;
        return new PhotoDto(photo.Id, thumbnail, photo.Width, photo.Height, versions);
    }
}

/// <summary>Uploads one photo for the caller. It stays unattached until a listing save references it.</summary>
public sealed record UploadListingPhotoCommand(Guid OwnerId, byte[] Content) : IRequest<Result<PhotoDto>>;

public sealed record GetPhotoUploadQuery(Guid OwnerId, Guid PhotoId) : IRequest<Result<PhotoDto>>;

/// <summary>
/// Checks size, the type sniffed from the content and the shortest side, in that order, then
/// processes the photo in the request: metadata is stripped and the versions are stored before
/// the row is saved, so a saved photo is always ready (ADR listings/0001).
/// </summary>
public sealed class ListingPhotoHandlers(
    ITutuMuchDbContext db,
    IImageInspector inspector,
    IListingPhotoProcessor processor,
    IPhotoStorage storage,
    IOptions<PhotoOptions> options,
    TimeProvider clock) :
    IRequestHandler<UploadListingPhotoCommand, Result<PhotoDto>>,
    IRequestHandler<GetPhotoUploadQuery, Result<PhotoDto>>
{
    public static readonly Error TooLarge = new(413, "PHOTO_TOO_LARGE", "Photos must be 10 MB or smaller");
    public static readonly Error Unsupported = new(415, "PHOTO_TYPE_UNSUPPORTED", "Use a JPEG, PNG or WebP photo");
    public static readonly Error TooSmall = new(422, "PHOTO_TOO_SMALL", "Photos must be at least 800 pixels on the shortest side");
    public static readonly Error TooManyPixels = new(422, "PHOTO_TOO_MANY_PIXELS", "Photos must be 50 megapixels or fewer");

    public async Task<Result<PhotoDto>> Handle(UploadListingPhotoCommand command, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var content = command.Content;
        if (content.LongLength > settings.MaxBytes)
        {
            return TooLarge;
        }

        if (inspector.Inspect(content) is not { } image)
        {
            return Unsupported;
        }

        if (Math.Min(image.Width, image.Height) < settings.MinShortSidePx)
        {
            return TooSmall;
        }

        if ((long)image.Width * image.Height > settings.MaxPixels)
        {
            return TooManyPixels;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(content));
        var versions = new List<ListingPhotoVersion>();
        foreach (var version in processor.Process(content, settings.VersionSizes))
        {
            var key = ListingPhotoVersion.KeyFor(hash, version.Size, version.Format);
            await storage.SavePublicAsync(key, version.Content, ListingPhotoVersion.ContentTypeOf(version.Format), cancellationToken);
            versions.Add(new ListingPhotoVersion(version.Size, version.Format, version.Width, version.Height, key, version.Content.LongLength));
        }

        var id = Guid.NewGuid();
        await storage.SaveOriginalAsync(ListingPhoto.OriginalKeyFor(id), content, image.ContentType, cancellationToken);
        var photo = ListingPhoto.Create(
            id, command.OwnerId, image.ContentType, content.LongLength, hash, image.Width, image.Height, versions, clock.GetUtcNow().UtcDateTime);
        db.ListingPhotos.Add(photo);
        await db.SaveChangesAsync(cancellationToken);
        return PhotoDto.From(photo, settings);
    }

    /// <summary>Another member's photo answers as missing (L2-010).</summary>
    public async Task<Result<PhotoDto>> Handle(GetPhotoUploadQuery query, CancellationToken cancellationToken)
    {
        var photo = await db.ListingPhotos.SingleOrDefaultAsync(p => p.Id == query.PhotoId && p.OwnerId == query.OwnerId, cancellationToken);
        return photo is null ? Error.NotFound("PHOTO_NOT_FOUND") : PhotoDto.From(photo, options.Value);
    }
}

/// <summary>
/// Links photos to a listing in the order of a save (L2-017). Called by the listing save
/// handlers; photos the save leaves out become unattached.
/// </summary>
public sealed class ListingPhotoAttacher(ITutuMuchDbContext db)
{
    public static readonly Error Limit = Invalid("PHOTO_LIMIT", "A listing can have up to 8 photos");
    public static readonly Error NotAvailable = Invalid("PHOTO_NOT_FOUND", "That photo is no longer available. Upload it again.");

    /// <summary>
    /// Sets the listing's photos to <paramref name="photoIds"/> in order, position 0 being the
    /// cover. Every photo must belong to the owner and be unattached or already on this listing.
    /// Null leaves the photos as they are.
    /// </summary>
    public async Task<Error?> AttachAsync(Guid ownerId, Guid listingId, IReadOnlyList<Guid>? photoIds, CancellationToken cancellationToken)
    {
        if (photoIds is null)
        {
            return null;
        }

        if (photoIds.Count > ListingPhoto.MaxPerListing)
        {
            return Limit;
        }

        var requested = photoIds.ToHashSet();
        var candidates = await db.ListingPhotos
            .Where(p => p.OwnerId == ownerId && (requested.Contains(p.Id) || p.ListingId == listingId))
            .ToListAsync(cancellationToken);
        var usable = candidates.Where(p => p.ListingId is null || p.ListingId == listingId).ToDictionary(p => p.Id);
        if (requested.Count != photoIds.Count || !requested.All(usable.ContainsKey))
        {
            return NotAvailable;
        }

        foreach (var photo in candidates.Where(p => p.ListingId == listingId && !requested.Contains(p.Id)))
        {
            photo.Detach();
        }

        for (var position = 0; position < photoIds.Count; position++)
        {
            usable[photoIds[position]].AttachTo(listingId, position);
        }

        return null;
    }

    /// <summary>The listing's photos, cover first.</summary>
    public async Task<IReadOnlyList<ListingPhoto>> PhotosOfAsync(Guid listingId, CancellationToken cancellationToken) =>
        await db.ListingPhotos.Where(p => p.ListingId == listingId).OrderBy(p => p.Position).ToListAsync(cancellationToken);

    private static Error Invalid(string code, string message) =>
        Error.Invalid(code, new Dictionary<string, FieldError> { ["photos"] = new(code, message) });
}
