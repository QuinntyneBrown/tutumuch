// Acceptance Test
// Traces to: L2-113, L2-114, L2-115, L2-116, L2-117, L2-118, L2-119, L2-120
// Description: A seller publishes an auction; verified members bid under the minimum and ceiling
// rules and the database decides races; the close job reserves it for the winner or removes it as
// unsold; only the winner checks out at the winning bid before the payment window ends; emails go
// to the outbid, the winner and the seller; and no bidder is ever named.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Api.Tests;

public sealed class AuctionTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly Member _seller;
    private readonly HttpClient _sellerClient;
    private readonly Member _ann;
    private readonly HttpClient _annClient;
    private readonly Member _bea;
    private readonly HttpClient _beaClient;

    public AuctionTests()
    {
        _factory.EnsureDatabase();
        (_seller, _sellerClient) = _factory.VerifiedClient("tutu_mom");
        (_ann, _annClient) = _factory.VerifiedClient("ann_b");
        (_bea, _beaClient) = _factory.VerifiedClient("bea_c");
    }

    /// <summary>Moves the clock and mints fresh session cookies, which the old clock would have expired.</summary>
    private void Advance(TimeSpan by)
    {
        _factory.Clock.Advance(by);
        foreach (var (client, member) in new[] { (_sellerClient, _seller), (_annClient, _ann), (_beaClient, _bea) })
        {
            client.DefaultRequestHeaders.Remove("Cookie");
            client.WithSession(_factory, member);
        }
    }

    [Fact]
    public async Task A_published_auction_ends_its_length_after_publishing()
    {
        using var seller = _factory.CreateMemberClient();
        await seller.SignInVerifiedMemberAsync(_factory);
        var reference = await seller.CreateDraftWithPhotoAsync(Listings.Complete().With("format", "Auction").With("auctionDays", 5).With("price", 12.00m));
        var now = _factory.Clock.GetUtcNow().UtcDateTime;

        using var published = await seller.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        var mine = await seller.GetFromJsonAsync<Dictionary<string, object>>($"/api/me/listings/{reference}");
        Assert.Equal("Auction", mine!["format"].ToString());
        Assert.Equal(now.AddDays(5), DateTime.Parse(mine["auctionEndsAt"].ToString()!, null, System.Globalization.DateTimeStyles.AdjustToUniversal), TimeSpan.FromSeconds(1));
        var detail = await _annClient.GetFromJsonAsync<AuctionDetailView>($"/api/catalog/listings/{reference}");
        Assert.Equal(("Auction", "12.00", 0), (detail!.Format, detail.Auction!.StartingBid, detail.Auction.BidCount));
    }

    [Fact]
    public async Task An_auction_without_a_length_cannot_be_published_and_a_wrong_length_cannot_be_saved()
    {
        using var seller = _factory.CreateMemberClient();
        await seller.SignInVerifiedMemberAsync(_factory);
        var reference = await seller.CreateDraftWithPhotoAsync(Listings.Complete().With("format", "Auction"));

        using var publish = await seller.PublishAsync(reference);
        using var save = await seller.PostAsJsonAsync("/api/listings", Listings.Complete().With("format", "Auction").With("auctionDays", 4));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, publish.StatusCode);
        var problem = await Problem.ReadAsync(publish);
        Assert.Equal("AUCTION_LENGTH_REQUIRED", problem.Code);
        Assert.Equal("Choose how long the auction runs", problem.Errors!["auctionDays"].Message);
        Assert.Equal("AUCTION_LENGTH_INVALID", (await Problem.ReadAsync(save)).Code);
    }

    [Fact]
    public async Task A_bid_at_the_minimum_is_accepted_and_the_bidder_is_the_highest()
    {
        var reference = _factory.SeedAuction(_seller.Id);

        using var response = await _annClient.BidAsync(reference, 12.00m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auction = (await response.Content.ReadFromJsonAsync<AuctionView>())!;
        Assert.Equal(("12.00", 1, "13.00", true), (auction.CurrentPrice, auction.BidCount, auction.MinimumBid, auction.ViewerIsHighestBidder));
    }

    [Fact]
    public async Task Bids_below_the_minimum_off_cents_or_over_the_ceiling_are_refused()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _annClient.BidAsync(reference, 18.00m)).Dispose();

        using var low = await _beaClient.BidAsync(reference, 18.50m);
        using var cents = await _beaClient.BidAsync(reference, 19.345m);
        using var ceiling = await _beaClient.BidAsync(reference, 5000.01m);
        using var enough = await _beaClient.BidAsync(reference, 19.00m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, low.StatusCode);
        var problem = await Problem.ReadAsync(low);
        Assert.Equal(("BID_TOO_LOW", "Bid $19.00 or more"), (problem.Code, problem.Errors!["amount"].Message));
        Assert.Equal("BID_INVALID", (await Problem.ReadAsync(cents)).Code);
        Assert.Equal("BID_INVALID", (await Problem.ReadAsync(ceiling)).Code);
        Assert.Equal(HttpStatusCode.Created, enough.StatusCode);
    }

    [Fact]
    public async Task The_seller_the_highest_bidder_a_visitor_and_an_unverified_member_cannot_bid()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _annClient.BidAsync(reference, 12.00m)).Dispose();
        using var visitor = _factory.CreateMemberClient();
        using var unverified = _factory.CreateMemberClient();
        await unverified.SignInUnverifiedMemberAsync("new_dad", "new.dad@example.com");

        using var own = await _sellerClient.BidAsync(reference, 20m);
        using var again = await _annClient.BidAsync(reference, 20m);
        using var anonymous = await visitor.BidAsync(reference, 20m);
        using var notVerified = await unverified.BidAsync(reference, 20m);

        Assert.Equal(("CANNOT_BID_OWN_LISTING", HttpStatusCode.UnprocessableEntity), ((await Problem.ReadAsync(own)).Code, own.StatusCode));
        Assert.Equal(("ALREADY_HIGHEST_BIDDER", HttpStatusCode.Conflict), ((await Problem.ReadAsync(again)).Code, again.StatusCode));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, notVerified.StatusCode);
    }

    [Fact]
    public async Task A_late_bid_a_fixed_price_listing_and_an_unknown_reference_are_refused()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        var fixedPrice = _factory.SeedActiveListing(_seller.Id);
        Advance(TimeSpan.FromDays(5));

        using var late = await _annClient.BidAsync(reference, 20m);
        using var notAuction = await _annClient.BidAsync(fixedPrice, 20m);
        using var unknown = await _annClient.BidAsync("L-99999", 20m);

        Assert.Equal("AUCTION_ENDED", (await Problem.ReadAsync(late)).Code);
        Assert.Equal("NOT_AN_AUCTION", (await Problem.ReadAsync(notAuction)).Code);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Racing_bids_store_exactly_one()
    {
        using var factory = new TuTuMuchApiFactory(concurrent: true);
        factory.EnsureDatabase();
        var (seller, _) = factory.VerifiedClient("race_seller");
        var reference = factory.SeedAuction(seller.Id);
        var bidders = Enumerable.Range(0, 20).Select(i => factory.VerifiedClient($"bidder_{i}").Client).ToList();

        var responses = await Task.WhenAll(bidders.Select(b => b.BidAsync(reference, 12.00m)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        var codes = responses.Where(r => r.StatusCode != HttpStatusCode.Created).Select(r => Problem.ReadAsync(r).Result.Code).ToList();
        Assert.Equal(19, codes.Count);
        Assert.All(codes, code => Assert.Contains(code, new[] { "OUTBID", "BID_TOO_LOW" }));
        factory.InDatabase(db => Assert.Single(db.Bids));
        Assert.Equal(1, factory.ListingOf(reference).BidCount);
        foreach (var disposable in responses.Cast<IDisposable>().Concat(bidders))
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public async Task The_outbid_member_is_emailed_and_the_new_highest_bidder_is_not()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _annClient.BidAsync(reference, 12.00m)).Dispose();
        (await _beaClient.BidAsync(reference, 15.00m)).Dispose();

        var sent = await _factory.DispatchEmailsAsync();

        var email = Assert.Single(sent);
        Assert.Equal("ann_b@example.com", email.To);
        Assert.Equal("You've been outbid on Rainbow tulle tutu, pink", email.Subject);
        Assert.Contains($"/items/{reference}", email.Body);
        Assert.DoesNotContain("bea_c", email.Body);
    }

    [Fact]
    public async Task The_detail_page_shows_each_viewer_only_their_own_standing()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _annClient.BidAsync(reference, 13.00m)).Dispose();
        (await _beaClient.BidAsync(reference, 18.00m)).Dispose();

        var ann = (await _annClient.GetFromJsonAsync<AuctionDetailView>($"/api/catalog/listings/{reference}"))!.Auction!;
        var bea = (await _beaClient.GetFromJsonAsync<AuctionDetailView>($"/api/catalog/listings/{reference}"))!.Auction!;
        var raw = await _sellerClient.GetStringAsync($"/api/catalog/listings/{reference}");

        Assert.Equal(("18.00", 2, false, true), (ann.CurrentPrice, ann.BidCount, ann.ViewerIsHighestBidder, ann.ViewerHasBid));
        Assert.Equal((true, true), (bea.ViewerIsHighestBidder, bea.ViewerHasBid));
        Assert.DoesNotContain(_ann.Id.ToString(), raw);
        Assert.DoesNotContain(_bea.Id.ToString(), raw);
        Assert.DoesNotContain("ann_b", raw);
        Assert.DoesNotContain("13.00", raw);
    }

    [Fact]
    public async Task The_catalog_price_filter_uses_the_current_bid()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _annClient.BidAsync(reference, 18.00m)).Dispose();

        var page = await _beaClient.GetFromJsonAsync<Dictionary<string, object>>("/api/catalog/listings?minPrice=15&maxPrice=20");

        Assert.Contains(reference, page!["items"].ToString());
        Assert.Contains("\"price\":\"18.00\"", page["items"].ToString()!.Replace(" ", string.Empty));
    }

    [Fact]
    public async Task Closing_with_bids_reserves_it_for_the_winner_and_emails_the_winner_and_seller()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _annClient.BidAsync(reference, 12.00m)).Dispose();
        (await _beaClient.BidAsync(reference, 18.00m)).Dispose();
        await _factory.DispatchEmailsAsync();
        Advance(TimeSpan.FromDays(5).Add(TimeSpan.FromMinutes(1)));

        Assert.Equal(1, await _factory.CloseAuctionsAsync());
        Assert.Equal(0, await _factory.CloseAuctionsAsync());

        var listing = _factory.ListingOf(reference);
        Assert.Equal((ListingStatus.Reserved, _bea.Id), (listing.Status, listing.WinnerId));
        Assert.Equal(listing.AuctionEndsAt!.Value.AddHours(48), listing.PayBy);
        var sent = await _factory.DispatchEmailsAsync();
        Assert.Equal(2, sent.Count);
        Assert.Contains(sent, e => e.To == "bea_c@example.com" && e.Subject == "You won Rainbow tulle tutu, pink" && e.Body.Contains("$18.00"));
        Assert.Contains(sent, e => e.To == "tutu_mom@example.com" && e.Subject == "Your auction for Rainbow tulle tutu, pink ended" && e.Body.Contains("$18.00"));
        var ann = (await _annClient.GetFromJsonAsync<AuctionDetailView>($"/api/catalog/listings/{reference}"))!.Auction!;
        var bea = (await _beaClient.GetFromJsonAsync<AuctionDetailView>($"/api/catalog/listings/{reference}"))!.Auction!;
        Assert.Equal((true, false), (ann.Ended, ann.ViewerIsWinner));
        Assert.Equal((true, listing.PayBy), (bea.ViewerIsWinner, bea.PayBy));
    }

    [Fact]
    public async Task Closing_without_bids_removes_it_as_unsold_and_relisting_starts_a_new_run()
    {
        var reference = _factory.SeedAuction(_seller.Id, days: 3);
        Advance(TimeSpan.FromDays(3));

        await _factory.CloseAuctionsAsync();

        var listing = _factory.ListingOf(reference);
        Assert.Equal((ListingStatus.Removed, ListingRemovalReason.AuctionUnsold), (listing.Status, listing.RemovalReason));
        var email = Assert.Single(await _factory.DispatchEmailsAsync());
        Assert.Contains("ended with no bids", email.Body);
        var rows = await _sellerClient.GetStringAsync("/api/me/listings");
        Assert.Contains("\"canRelist\":true", rows);

        using var relist = await _sellerClient.PostAsync($"/api/listings/{reference}/relist", content: null);

        Assert.Equal(HttpStatusCode.NoContent, relist.StatusCode);
        var relisted = _factory.ListingOf(reference);
        Assert.Equal(ListingStatus.Active, relisted.Status);
        Assert.Equal(_factory.Clock.GetUtcNow().UtcDateTime.AddDays(3), relisted.AuctionEndsAt!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Only_the_winner_checks_out_at_the_winning_bid()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _annClient.BidAsync(reference, 12.00m)).Dispose();

        using var running = await _beaClient.StartCheckoutAsync(reference);
        Assert.Equal("AUCTION_BIDS_ONLY", (await Problem.ReadAsync(running)).Code);

        (await _beaClient.BidAsync(reference, 18.00m)).Dispose();
        Advance(TimeSpan.FromDays(5));
        await _factory.CloseAuctionsAsync();

        using var loser = await _annClient.StartCheckoutAsync(reference);
        Assert.Equal("AUCTION_ENDED", (await Problem.ReadAsync(loser)).Code);

        var checkout = await _beaClient.StartedCheckoutAsync(reference);
        Assert.Equal(("18.00", "5.00"), (checkout.ItemPrice, checkout.ShippingCost));
    }

    [Fact]
    public async Task A_lapsed_winner_checkout_can_be_started_again_before_the_window_ends()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _beaClient.BidAsync(reference, 18.00m)).Dispose();
        Advance(TimeSpan.FromDays(5));
        await _factory.CloseAuctionsAsync();
        var first = await _beaClient.StartedCheckoutAsync(reference);

        Advance(TimeSpan.FromMinutes(11));
        await _factory.ExpireReservationsAsync();

        var listing = _factory.ListingOf(reference);
        Assert.Equal((ListingStatus.Reserved, (Guid?)null), (listing.Status, listing.ReservedByCheckoutId));
        var second = await _beaClient.StartedCheckoutAsync(reference);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("18.00", second.ItemPrice);
    }

    [Fact]
    public async Task An_unpaid_window_removes_it_and_tells_the_winner_and_seller()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _beaClient.BidAsync(reference, 18.00m)).Dispose();
        Advance(TimeSpan.FromDays(5));
        await _factory.CloseAuctionsAsync();
        await _factory.DispatchEmailsAsync();
        Advance(TimeSpan.FromHours(48));

        await _factory.CloseAuctionsAsync();

        var listing = _factory.ListingOf(reference);
        Assert.Equal((ListingStatus.Removed, ListingRemovalReason.AuctionUnpaid), (listing.Status, listing.RemovalReason));
        var sent = await _factory.DispatchEmailsAsync();
        Assert.Contains(sent, e => e.To == "bea_c@example.com" && e.Subject == "Your time to pay for Rainbow tulle tutu, pink ran out");
        Assert.Contains(sent, e => e.To == "tutu_mom@example.com" && e.Subject == "The winner didn't pay for Rainbow tulle tutu, pink");
        using var late = await _beaClient.StartCheckoutAsync(reference);
        Assert.Equal(HttpStatusCode.NotFound, late.StatusCode);
    }

    public void Dispose()
    {
        _sellerClient.Dispose();
        _annClient.Dispose();
        _beaClient.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task The_winner_pays_and_the_listing_is_sold()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _beaClient.BidAsync(reference, 18.00m)).Dispose();
        Advance(TimeSpan.FromDays(5));
        await _factory.CloseAuctionsAsync();

        var (checkout, payment) = await _beaClient.CheckoutAndPayAsync(reference);
        using var webhook = await _beaClient.PostWebhookAsync(FakePaymentProvider.Webhook(payment.Id));

        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
        Assert.Equal(ListingStatus.Sold, _factory.ListingOf(reference).Status);
        _factory.InDatabase(db => Assert.Single(db.Orders, o => o.CheckoutId == checkout.Id));
    }

    [Fact]
    public async Task With_bids_the_seller_can_neither_edit_nor_withdraw_it()
    {
        var reference = _factory.SeedAuction(_seller.Id);
        (await _annClient.BidAsync(reference, 12.00m)).Dispose();
        var version = _factory.ListingOf(reference).Version;
        var edit = new HttpRequestMessage(HttpMethod.Put, $"/api/listings/{reference}") { Content = JsonContent.Create(Listings.Complete("Dance", "Tutus")) };
        edit.Headers.IfMatch.ParseAdd($"\"{version}\"");

        using var edited = await _sellerClient.SendAsync(edit);
        using var withdrawn = await _sellerClient.PostAsync($"/api/listings/{reference}/withdraw", content: null);

        Assert.Equal(("AUCTION_HAS_BIDS", HttpStatusCode.Conflict), ((await Problem.ReadAsync(edited)).Code, edited.StatusCode));
        Assert.Equal(("AUCTION_HAS_BIDS", HttpStatusCode.Conflict), ((await Problem.ReadAsync(withdrawn)).Code, withdrawn.StatusCode));
        Assert.Equal(ListingStatus.Active, _factory.ListingOf(reference).Status);
    }

    [Fact]
    public async Task Without_bids_the_seller_can_change_the_starting_bid_but_not_the_format()
    {
        var reference = await _sellerClient.CreateDraftWithPhotoAsync(Listings.Complete("Dance", "Tutus").With("format", "Auction").With("auctionDays", 5));
        (await _sellerClient.PublishAsync(reference)).EnsureSuccessStatusCode();
        var before = _factory.ListingOf(reference);
        var edit = new HttpRequestMessage(HttpMethod.Put, $"/api/listings/{reference}")
        {
            Content = JsonContent.Create(Listings.Complete("Dance", "Tutus").With("price", 20.00m).With("format", "FixedPrice").With("auctionDays", 7)),
        };
        edit.Headers.IfMatch.ParseAdd($"\"{before.Version}\"");

        using var edited = await _sellerClient.SendAsync(edit);

        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        var after = _factory.ListingOf(reference);
        Assert.Equal((ListingFormat.Auction, 5, before.AuctionEndsAt, 20.00m), (after.Format, after.AuctionDays, after.AuctionEndsAt, after.Price));
    }
}
