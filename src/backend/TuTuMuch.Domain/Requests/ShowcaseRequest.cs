using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Domain.Requests;

/// <summary>
/// One member's request for one showcase listing (L2-109): an optional note, at most one per member
/// and listing, so sending again updates it. Not an item request, which is a saved search (L1-023).
/// </summary>
public sealed class ShowcaseRequest
{
    /// <summary>The longest note after trimming (L2-109).</summary>
    public const int MaxNote = 500;

    /// <summary>New requests a member may make per UTC day; updates are not counted (L2-109, L2-051).</summary>
    public const int NewPerDay = 20;

    private ShowcaseRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public Guid ListingId { get; private set; }

    public string? Note { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>Every broken rule of the note; plain text is stored as sent, apart from trimming (L2-049).</summary>
    public static IReadOnlyList<RuleViolation> Check(string? note) =>
        Clean(note)?.Length > MaxNote ? [NoteLength] : [];

    /// <summary>Creates a request. Call <see cref="Check"/> first.</summary>
    public static ShowcaseRequest Create(Guid memberId, Guid listingId, string? note, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        MemberId = memberId,
        ListingId = listingId,
        Note = Clean(note),
        CreatedAt = now,
        UpdatedAt = now,
    };

    /// <summary>Replaces the note; the request now reads as sent at <paramref name="now"/>.</summary>
    public void Update(string? note, DateTime now)
    {
        Note = Clean(note);
        UpdatedAt = now;
    }

    private static readonly RuleViolation NoteLength = new("note", "SHOWCASE_NOTE_LENGTH", "Keep the note to 500 characters");

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
