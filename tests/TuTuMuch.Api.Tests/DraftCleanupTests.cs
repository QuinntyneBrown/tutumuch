// Acceptance Test
// Traces to: L2-019, L2-042, L2-043
// Description: A draft not edited for 90 days is deleted with its photos and their stored files.
// 7 days before, the seller gets one reminder email that links to the draft; it is a listing
// notice that the marketing setting does not turn off. Editing the draft restarts its 90 days.

using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Application.Listings;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Api.Tests;

public sealed class DraftCleanupTests : IDisposable
{
    private const string Seller = "kicks30001@example.com";

    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;

    public DraftCleanupTests()
    {
        _buyer = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task A_draft_untouched_for_83_days_earns_one_reminder_that_links_to_it()
    {
        var (seller, draft) = await SellerWithPhotoDraftAsync();

        _factory.Clock.Advance(TimeSpan.FromDays(82.9));
        Assert.Equal(0, await CleanUpAsync());
        _factory.Clock.Advance(TimeSpan.FromDays(0.2));
        Assert.Equal(1, await CleanUpAsync());
        Assert.Equal(0, await CleanUpAsync());
        var sent = await _factory.DispatchEmailsAsync();

        var reminder = Assert.Single(sent);
        Assert.Equal(Seller, reminder.To);
        Assert.Contains("draft", reminder.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Smocked romper, yellow", reminder.Body);
        Assert.Contains($"https://tutumuch.test/sell/{draft}", reminder.Body);
        _factory.InDatabase(db => Assert.NotNull(db.Listings.Single(l => l.Reference == draft).DraftReminderSentAt));
        seller.Dispose();
    }

    [Fact]
    public async Task A_draft_90_days_old_and_reminded_7_days_ago_is_deleted_with_its_photos_and_files()
    {
        var (seller, draft) = await SellerWithPhotoDraftAsync();
        var activePhoto = await UploadOtherPhotoAsync(seller);
        var active = await seller.CreateDraftAsync(Listings.Complete().With("photoIds", new[] { activePhoto }));
        (await seller.PublishAsync(active)).EnsureSuccessStatusCode();
        var keys = StoredKeysOf(draft);
        Assert.NotEmpty(keys);

        _factory.Clock.Advance(TimeSpan.FromDays(83.1));
        await CleanUpAsync();
        _factory.Clock.Advance(TimeSpan.FromDays(6.9));
        await CleanUpAsync();
        _factory.InDatabase(db => Assert.True(db.Listings.Any(l => l.Reference == draft)));
        _factory.Clock.Advance(TimeSpan.FromDays(0.2));
        Assert.Equal(1, await CleanUpAsync());

        _factory.InDatabase(db =>
        {
            Assert.False(db.Listings.Any(l => l.Reference == draft));
            Assert.True(db.Listings.Any(l => l.Reference == active));
            Assert.Single(db.ListingPhotos);
        });
        Assert.All(keys, key => Assert.False(_factory.Photos.Originals.ContainsKey(key) || _factory.Photos.Public.ContainsKey(key), key));
        Assert.All(StoredKeysOf(active), key => Assert.True(_factory.Photos.Originals.ContainsKey(key) || _factory.Photos.Public.ContainsKey(key), key));
        seller.Dispose();
    }

    [Fact]
    public async Task A_draft_that_missed_its_reminder_is_reminded_first_and_kept_7_more_days()
    {
        var (seller, draft) = await SellerWithPhotoDraftAsync();

        _factory.Clock.Advance(TimeSpan.FromDays(120));
        Assert.Equal(1, await CleanUpAsync());
        _factory.InDatabase(db => Assert.True(db.Listings.Any(l => l.Reference == draft)));
        _factory.Clock.Advance(TimeSpan.FromDays(7.1));
        Assert.Equal(1, await CleanUpAsync());
        _factory.InDatabase(db => Assert.False(db.Listings.Any(l => l.Reference == draft)));
        seller.Dispose();
    }

    [Fact]
    public async Task Editing_a_reminded_draft_restarts_its_90_days_and_its_reminder()
    {
        var (seller, draft) = await SellerWithPhotoDraftAsync();
        _factory.Clock.Advance(TimeSpan.FromDays(84));
        await CleanUpAsync();

        _factory.InDatabase(db =>
        {
            // The save the edit endpoint makes; the seller's session has long expired by day 84.
            var listing = db.Listings.Single(l => l.Reference == draft);
            listing.UpdateDetails(ListingDetails.Empty with { Category = "Clothing", Title = "Smocked romper, yellow and white" }, _factory.Clock.GetUtcNow().UtcDateTime);
            db.SaveChanges();
            Assert.Null(listing.DraftReminderSentAt);
        });
        _factory.Clock.Advance(TimeSpan.FromDays(10));

        Assert.Equal(0, await CleanUpAsync());
        _factory.InDatabase(db => Assert.True(db.Listings.Any(l => l.Reference == draft)));
        seller.Dispose();
    }

    [Fact]
    public async Task The_reminder_is_sent_even_when_marketing_emails_are_off()
    {
        var (seller, _) = await SellerWithPhotoDraftAsync();
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/me/notification-preferences")
        {
            Content = JsonContent.Create(new { reviewEmails = false, marketingEmails = false }),
        };
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var off = await seller.SendAsync(request);
        off.EnsureSuccessStatusCode();

        _factory.Clock.Advance(TimeSpan.FromDays(84));
        await CleanUpAsync();

        Assert.Single(await _factory.DispatchEmailsAsync());
        seller.Dispose();
    }

    private async Task<(HttpClient Seller, string Draft)> SellerWithPhotoDraftAsync()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (_, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        var draft = await seller.CreateDraftWithPhotoAsync(new Dictionary<string, object?> { ["category"] = "Clothing", ["title"] = "Smocked romper, yellow" });
        await _factory.DispatchEmailsAsync();
        return (seller, draft);
    }

    [Fact]
    public async Task Versions_another_photo_shares_by_content_are_kept_when_a_draft_is_deleted()
    {
        var (seller, draft) = await SellerWithPhotoDraftAsync();
        var active = await seller.CreateDraftWithPhotoAsync(Listings.Complete());
        (await seller.PublishAsync(active)).EnsureSuccessStatusCode();
        var draftKeys = StoredKeysOf(draft);

        _factory.Clock.Advance(TimeSpan.FromDays(84));
        await CleanUpAsync();
        _factory.Clock.Advance(TimeSpan.FromDays(8));
        Assert.Equal(1, await CleanUpAsync());

        Assert.False(_factory.Photos.Originals.ContainsKey(draftKeys[0]));
        Assert.All(StoredKeysOf(active), key => Assert.True(_factory.Photos.Originals.ContainsKey(key) || _factory.Photos.Public.ContainsKey(key), key));
        seller.Dispose();
    }

    private static async Task<Guid> UploadOtherPhotoAsync(HttpClient seller)
    {
        using var response = await seller.UploadPhotoAsync(Images.Jpeg(1000, 1400));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PhotoUpload>())!.Id;
    }

    private List<string> StoredKeysOf(string reference)
    {
        var keys = new List<string>();
        _factory.InDatabase(db =>
        {
            var id = db.Listings.Single(l => l.Reference == reference).Id;
            foreach (var photo in db.ListingPhotos.Where(p => p.ListingId == id).ToList())
            {
                keys.Add(photo.OriginalKey);
                keys.AddRange(photo.Versions.Select(v => v.Key));
            }
        });
        return keys;
    }

    private async Task<int> CleanUpAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CleanUpDraftsCommand());
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _factory.Dispose();
    }
}
