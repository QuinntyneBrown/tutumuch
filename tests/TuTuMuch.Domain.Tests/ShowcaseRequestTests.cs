// Acceptance Test
// Traces to: L2-107, L2-109
// Description: A showcase request keeps an optional, trimmed note of at most 500 characters, and
// only a Draft or Active listing can be marked or unmarked as a showcase listing.

using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Domain.Tests;

public sealed class ShowcaseRequestTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_note_is_trimmed_and_an_empty_one_is_none()
    {
        var request = ShowcaseRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "  Size 7  ", Now);
        var empty = ShowcaseRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "   ", Now);

        Assert.Equal("Size 7", request.Note);
        Assert.Null(empty.Note);
        Assert.Equal(Now, request.CreatedAt);
        Assert.Equal(Now, request.UpdatedAt);
    }

    [Fact]
    public void A_note_of_500_characters_is_accepted_and_501_refused()
    {
        Assert.Empty(ShowcaseRequest.Check(new string('a', 500)));
        Assert.Empty(ShowcaseRequest.Check(null));
        Assert.Empty(ShowcaseRequest.Check("  " + new string('a', 500) + "  "));
        var violation = Assert.Single(ShowcaseRequest.Check(new string('a', 501)));
        Assert.Equal(new RuleViolation("note", "SHOWCASE_NOTE_LENGTH", "Keep the note to 500 characters"), violation);
    }

    [Fact]
    public void Updating_replaces_the_note_and_the_date_but_not_the_creation()
    {
        var request = ShowcaseRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "first", Now);

        request.Update(null, Now.AddHours(1));

        Assert.Null(request.Note);
        Assert.Equal(Now, request.CreatedAt);
        Assert.Equal(Now.AddHours(1), request.UpdatedAt);
    }

    [Fact]
    public void A_draft_or_active_listing_can_be_marked_and_the_version_moves()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, new ListingDetails("Dance", null, null, null, null, null, null, null, null, null), Now);
        var version = listing.Version;

        listing.SetShowcase(true, Now);

        Assert.True(listing.IsShowcase);
        Assert.True(listing.CanBeShowcase);
        Assert.Equal(version + 1, listing.Version);
        listing.SetShowcase(false, Now);
        Assert.False(listing.IsShowcase);
    }

    [Fact]
    public void A_withdrawn_listing_cannot_be_marked()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, new ListingDetails("Dance", null, null, null, null, null, null, null, null, null), Now);
        typeof(Listing).GetProperty(nameof(Listing.Status))!.SetValue(listing, ListingStatus.Active);
        listing.Withdraw(Now);

        Assert.False(listing.CanBeShowcase);
        Assert.Throws<InvalidOperationException>(() => listing.SetShowcase(true, Now));
    }
}
