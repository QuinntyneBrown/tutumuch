namespace TutuMuch.Domain.Listings;

/// <summary>Encodings of a photo version (L2-059): WebP with a JPEG fallback.</summary>
public enum PhotoFormat
{
    WebP,
    Jpeg,
}

/// <summary>
/// One re-encoded, metadata-free copy of a photo. <see cref="Size"/> is the target width of 400,
/// 800 or 1600 px; <see cref="Width"/> is smaller when the original is narrower (never upscaled).
/// The key is "{hash}/{size}.{ext}", where hash is the SHA-256 of the original.
/// </summary>
public sealed record ListingPhotoVersion(int Size, PhotoFormat Format, int Width, int Height, string Key, long Bytes)
{
    public static string KeyFor(string hash, int size, PhotoFormat format) =>
        $"{hash}/{size}.{(format == PhotoFormat.WebP ? "webp" : "jpg")}";

    public static string ContentTypeOf(PhotoFormat format) => format == PhotoFormat.WebP ? "image/webp" : "image/jpeg";
}

/// <summary>
/// One photo uploaded by a member (L2-017). It is unattached until a listing save references it;
/// position 0 of a listing is its cover.
/// </summary>
public sealed class ListingPhoto
{
    public const int MaxPerListing = 8;

    private readonly List<ListingPhotoVersion> _versions = [];

    private ListingPhoto()
    {
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid? ListingId { get; private set; }

    public int? Position { get; private set; }

    public string OriginalKey { get; private set; } = string.Empty;

    /// <summary>The type sniffed from the content, never the one the client declared.</summary>
    public string OriginalContentType { get; private set; } = string.Empty;

    public long OriginalBytes { get; private set; }

    /// <summary>Lower-case hex SHA-256 of the original; the prefix of every version key.</summary>
    public string OriginalHash { get; private set; } = string.Empty;

    public int Width { get; private set; }

    public int Height { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<ListingPhotoVersion> Versions => _versions;

    public static ListingPhoto Create(
        Guid id, Guid ownerId, string contentType, long bytes, string hash, int width, int height,
        IEnumerable<ListingPhotoVersion> versions, DateTime now)
    {
        var photo = new ListingPhoto
        {
            Id = id,
            OwnerId = ownerId,
            OriginalKey = OriginalKeyFor(id),
            OriginalContentType = contentType,
            OriginalBytes = bytes,
            OriginalHash = hash,
            Width = width,
            Height = height,
            CreatedAt = now,
        };
        photo._versions.AddRange(versions);
        return photo;
    }

    public static string OriginalKeyFor(Guid id) => $"originals/{id:N}";

    public void AttachTo(Guid listingId, int position)
    {
        if (position is < 0 or >= MaxPerListing)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        ListingId = listingId;
        Position = position;
    }

    public void Detach()
    {
        ListingId = null;
        Position = null;
    }
}
