// Acceptance Test
// Traces to: L2-036, L2-010, L2-042
// Description: The buyer reports a problem on a Shipped or Delivered order until 3 days after
// delivery, with a reason, a description and up to 4 private photos; the seller's payout is held,
// auto-completion stops, and the seller and administrators are emailed. Outside the window the API
// answers 409. The seller replies once; the buyer and administrators see the reply. An
// administrator resolves the problem: a full refund makes the order Refunded, refunds the buyer
// and cancels the payout; release completes the order and makes the payout releasable. Members
// without the Administrator role get 403 from administrator endpoints.

using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TutuMuch.Api.Tests.Support;
using TutuMuch.Application.Orders;
using TutuMuch.Domain.Notifications;
using TutuMuch.Domain.Orders;
using TutuMuch.Domain.Payouts;

namespace TutuMuch.Api.Tests;

public sealed class ResolveOrderProblemsTests : IDisposable
{
    private const string BuyerEmail = "marcus.bell@example.com";
    private const string SellerEmail = "kicks30001@example.com";

    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;

    public ResolveOrderProblemsTests()
    {
        _buyer = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task The_buyer_reports_wrong_size_on_a_delivered_order_and_the_payout_is_held()
    {
        var (orderId, seller) = await DeliveredOrderAsync();
        using var admin = _factory.CreateAdministratorClient();
        using var stranger = await StrangerAsync();
        await _factory.DispatchEmailsAsync();

        using var response = await _buyer.ReportProblemAsync(orderId, "WrongSize", "  Label inside says M, not L.  ", Images.JpegWithGps(1200, 900), Images.Png(400, 300));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemView>())!;
        Assert.Equal("P-1001", problem.Reference);
        Assert.Equal("WrongSize", problem.Reason);
        Assert.Equal("Wrong size", problem.ReasonLabel);
        Assert.Equal("Label inside says M, not L.", problem.Description);
        Assert.Equal("Open", problem.Status);
        Assert.Equal(2, problem.Photos.Count);
        _factory.InDatabase(db =>
        {
            var payout = db.Payouts.Single();
            Assert.Equal(PayoutStatus.Held, payout.Status);
            Assert.Equal(PayoutHoldReason.OpenProblem, payout.HoldReason);
            Assert.Equal(OrderStatus.Delivered, db.Orders.Single().Status);
        });

        // Photos are private, stripped of metadata and never public (L2-017, L2-036).
        Assert.Empty(_factory.Photos.Public);
        Assert.Equal(2, _factory.Photos.Originals.Keys.Count(k => k.StartsWith("problems/", StringComparison.Ordinal)));
        Assert.All(_factory.Photos.Originals.Values, o => Assert.False(Images.ContainsAscii(o.Content, "GPSLatitude")));
        using var photo = await _buyer.GetAsync(problem.Photos[0].Url);
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.Equal("image/jpeg", photo.Content.Headers.ContentType!.MediaType);
        Assert.True(photo.Headers.CacheControl!.NoStore);
        Assert.Equal(HttpStatusCode.OK, (await seller.GetAsync(problem.Photos[0].Url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(problem.Photos[0].Url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(problem.Photos[0].Url)).StatusCode);

        var sent = await _factory.DispatchEmailsAsync();
        var toSeller = Assert.Single(sent, e => e.To == SellerEmail);
        Assert.Contains("Wrong size", toSeller.Body);
        Assert.Contains($"https://tutumuch.test/sales/{orderId}", toSeller.Body);
        var toAdmin = Assert.Single(sent, e => e.To == "ana_ruiz@tutumuch.test");
        Assert.Contains("https://tutumuch.test/admin/problems/P-1001", toAdmin.Body);

        var purchase = (await _buyer.GetFromJsonAsync<PurchaseWithProblemView>($"/api/purchases/{orderId}"))!;
        Assert.False(purchase.CanReportProblem);
        Assert.Equal("P-1001", purchase.Problem!.Reference);
        var sale = (await seller.GetFromJsonAsync<SaleWithProblemView>($"/api/sales/{orderId}"))!;
        Assert.Equal("Label inside says M, not L.", sale.Problem!.Description);
    }

    [Fact]
    public async Task A_shipped_order_can_have_a_problem_and_the_window_is_shown_on_a_delivered_one()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        var paid = (await _buyer.GetFromJsonAsync<PurchaseWithProblemView>($"/api/purchases/{orderId}"))!;
        Assert.False(paid.CanReportProblem);
        using var tooEarly = await _buyer.ReportProblemAsync(orderId, "NotReceived", "Nothing yet.");
        Assert.Equal(HttpStatusCode.Conflict, tooEarly.StatusCode);
        Assert.Equal("PROBLEM_WINDOW_CLOSED", (await Problem.ReadAsync(tooEarly)).Code);

        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();
        var shipped = (await _buyer.GetFromJsonAsync<PurchaseWithProblemView>($"/api/purchases/{orderId}"))!;
        Assert.True(shipped.CanReportProblem);
        Assert.Null(shipped.ProblemDeadline);

        using var response = await _buyer.ReportProblemAsync(orderId, "NotReceived", "Tracking has not moved for a week.");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var again = await _buyer.ReportProblemAsync(orderId, "NotReceived", "Still nothing.");
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("PROBLEM_ALREADY_REPORTED", (await Problem.ReadAsync(again)).Code);
    }

    [Fact]
    public async Task After_3_days_from_delivery_the_option_is_gone_and_the_api_answers_409()
    {
        var (orderId, _) = await DeliveredOrderAsync();
        var delivered = (await _buyer.GetFromJsonAsync<PurchaseWithProblemView>($"/api/purchases/{orderId}"))!;
        Assert.True(delivered.CanReportProblem);
        Assert.Equal(Now().AddHours(-1).AddDays(3), delivered.ProblemDeadline);

        _factory.Clock.Advance(TimeSpan.FromDays(3));
        (await _buyer.SignInAsync()).EnsureSuccessStatusCode();
        var late = (await _buyer.GetFromJsonAsync<PurchaseWithProblemView>($"/api/purchases/{orderId}"))!;
        using var response = await _buyer.ReportProblemAsync(orderId, "WrongSize", "Too small.");

        Assert.False(late.CanReportProblem);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("PROBLEM_WINDOW_CLOSED", (await Problem.ReadAsync(response)).Code);
        _factory.InDatabase(db => Assert.Empty(db.OrderProblems));
    }

    [Fact]
    public async Task A_report_needs_a_reason_a_description_and_at_most_4_photos()
    {
        var (orderId, _) = await DeliveredOrderAsync();

        using var empty = await _buyer.ReportProblemAsync(orderId, null, "   ");
        using var tooMany = await _buyer.ReportProblemAsync(orderId, "WrongSize", "Too small.", Photos(5));
        using var notImage = await _buyer.ReportProblemAsync(orderId, "WrongSize", "Too small.", "not an image"u8.ToArray());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);
        var problem = await Problem.ReadAsync(empty);
        Assert.Equal("PROBLEM_INVALID", problem.Code);
        Assert.Equal("Choose what’s wrong", problem.Errors!["reason"].Message);
        Assert.Equal("Tell us what happened", problem.Errors!["description"].Message);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooMany.StatusCode);
        Assert.Equal("PHOTO_LIMIT", (await Problem.ReadAsync(tooMany)).Errors!["photos"].Code);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, notImage.StatusCode);
        _factory.InDatabase(db => Assert.Empty(db.OrderProblems));
    }

    [Fact]
    public async Task Only_the_buyer_reports_and_only_the_seller_replies()
    {
        var (orderId, seller) = await DeliveredOrderAsync();

        using var sellerReports = await seller.ReportProblemAsync(orderId, "WrongSize", "x");
        Assert.Equal(HttpStatusCode.NotFound, sellerReports.StatusCode);
        (await _buyer.ReportProblemAsync(orderId, "WrongSize", "Too small.")).EnsureSuccessStatusCode();
        using var buyerReplies = await _buyer.RespondToProblemAsync(orderId, "x");
        Assert.Equal(HttpStatusCode.NotFound, buyerReplies.StatusCode);
    }

    [Fact]
    public async Task The_seller_replies_once_and_the_buyer_and_administrator_see_it()
    {
        var (orderId, seller) = await DeliveredOrderAsync();
        var reported = await ReportAsync(orderId);
        using var admin = _factory.CreateAdministratorClient();

        using var blank = await seller.RespondToProblemAsync(orderId, "  ");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blank.StatusCode);
        using var response = await seller.RespondToProblemAsync(orderId, " My mistake, happy to take them back. ");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("My mistake, happy to take them back.", (await response.Content.ReadFromJsonAsync<SaleWithProblemView>())!.Problem!.SellerResponse);
        using var twice = await seller.RespondToProblemAsync(orderId, "And again.");
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        Assert.Equal("PROBLEM_RESPONSE_NOT_ALLOWED", (await Problem.ReadAsync(twice)).Code);

        var purchase = (await _buyer.GetFromJsonAsync<PurchaseWithProblemView>($"/api/purchases/{orderId}"))!;
        Assert.Equal("My mistake, happy to take them back.", purchase.Problem!.SellerResponse);
        var detail = (await admin.GetFromJsonAsync<ProblemDetailView>($"/api/admin/problems/{reported.Reference}"))!;
        Assert.Equal("My mistake, happy to take them back.", detail.Problem.SellerResponse);
        Assert.Equal("marcus_12", detail.BuyerDisplayName);
        Assert.Equal("kicks_30001", detail.SellerDisplayName);
        Assert.Equal("Delivered", detail.Order.Status);
        Assert.Equal("102.34", detail.Order.Total);
        Assert.Equal("Held", detail.Payout);
    }

    [Fact]
    public async Task An_open_problem_stops_auto_completion_and_confirming_receipt()
    {
        var (orderId, _) = await DeliveredOrderAsync();
        await ReportAsync(orderId);
        _factory.Clock.Advance(TimeSpan.FromDays(4));
        (await _buyer.SignInAsync()).EnsureSuccessStatusCode();

        Assert.Equal(0, await RunCompletionJobAsync());
        using var confirm = await _buyer.ConfirmReceiptAsync(orderId);

        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal("ORDER_PROBLEM_OPEN", (await Problem.ReadAsync(confirm)).Code);
        _factory.InDatabase(db => Assert.Equal(OrderStatus.Delivered, db.Orders.Single().Status));
        Assert.Empty(_factory.Completed.Events);
    }

    [Fact]
    public async Task A_full_refund_makes_the_order_refunded_refunds_the_buyer_and_cancels_the_payout()
    {
        var (orderId, seller) = await DeliveredOrderAsync();
        var reported = await ReportAsync(orderId);
        using var admin = _factory.CreateAdministratorClient();
        await _factory.DispatchEmailsAsync();

        using var response = await admin.ResolveProblemAsync(reported.Reference, "FullRefund", "  Hole by the left cuff wasn't disclosed. ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = (await response.Content.ReadFromJsonAsync<ProblemDetailView>())!;
        Assert.Equal("Resolved", detail.Problem.Status);
        Assert.Equal("FullRefund", detail.Problem.Outcome);
        Assert.Equal("Hole by the left cuff wasn't disclosed.", detail.Problem.ResolutionReason);
        Assert.Equal("Refunded", detail.Order.Status);
        Assert.Equal("Cancelled", detail.Payout);
        var refund = Assert.Single(_factory.Payments.Refunds);
        Assert.Equal(102.34m, refund.Amount);
        _factory.InDatabase(db =>
        {
            Assert.Equal(OrderStatus.Refunded, db.Orders.Single().Status);
            Assert.Equal(OrderActor.Administrator, db.Set<OrderStatusChange>().Single(c => c.To == OrderStatus.Refunded).Actor);
            Assert.Equal(RefundReason.ProblemResolved, db.Refunds.Single().Reason);
            Assert.Equal(RefundStatus.Issued, db.Refunds.Single().Status);
            Assert.Equal(PayoutStatus.Cancelled, db.Payouts.Single().Status);
            Assert.NotNull(db.OrderProblems.Single().ResolvedBy);
        });

        var sent = await _factory.DispatchEmailsAsync();
        Assert.Contains(sent, e => e.To == BuyerEmail && e.Body.Contains("Hole by the left cuff", StringComparison.Ordinal));
        Assert.Contains(sent, e => e.To == SellerEmail && e.Body.Contains("Hole by the left cuff", StringComparison.Ordinal));
        Assert.Contains(sent, e => e.To == BuyerEmail && e.Subject.Contains("refund", StringComparison.Ordinal));

        var purchase = (await _buyer.GetFromJsonAsync<PurchaseWithProblemView>($"/api/purchases/{orderId}"))!;
        Assert.Equal("Refunded", purchase.Status);
        Assert.Equal("Resolved", purchase.Problem!.Status);
        using var twice = await admin.ResolveProblemAsync(reported.Reference, "ReleaseToSeller", "Changed my mind.");
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        Assert.Equal("PROBLEM_ALREADY_RESOLVED", (await Problem.ReadAsync(twice)).Code);
        using var reply = await seller.RespondToProblemAsync(orderId, "Too late?");
        Assert.Equal(HttpStatusCode.Conflict, reply.StatusCode);
    }

    [Fact]
    public async Task Release_to_the_seller_completes_the_order_and_releases_the_payout()
    {
        var (orderId, seller) = await DeliveredOrderAsync();
        await _factory.OnboardAsync(seller);
        var reported = await ReportAsync(orderId);
        using var admin = _factory.CreateAdministratorClient();

        using var response = await admin.ResolveProblemAsync(reported.Reference, "ReleaseToSeller", "The photos show the right size.");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = (await response.Content.ReadFromJsonAsync<ProblemDetailView>())!;
        Assert.Equal("Completed", detail.Order.Status);
        Assert.Equal("ReleaseToSeller", detail.Problem.Outcome);
        Assert.Empty(_factory.Payments.Refunds);
        Assert.Equal(orderId, Assert.Single(_factory.Completed.Events).OrderId);
        _factory.InDatabase(db =>
        {
            Assert.Equal(OrderActor.Administrator, db.Set<OrderStatusChange>().Single(c => c.To == OrderStatus.Completed).Actor);
            Assert.Equal(PayoutStatus.Releasable, db.Payouts.Single().Status);
        });
    }

    [Fact]
    public async Task A_shipped_order_cannot_be_released_and_a_resolution_needs_a_reason_and_an_outcome()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();
        var reported = await ReportAsync(orderId);
        using var admin = _factory.CreateAdministratorClient();

        using var noReason = await admin.ResolveProblemAsync(reported.Reference, "FullRefund", " ");
        using var noOutcome = await admin.ResolveProblemAsync(reported.Reference, "PartialRefund", "Half.");
        using var release = await admin.ResolveProblemAsync(reported.Reference, "ReleaseToSeller", "Should arrive soon.");
        using var missing = await admin.ResolveProblemAsync("P-9999", "FullRefund", "x");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, noReason.StatusCode);
        Assert.Equal("Add a reason. Both members see it.", (await Problem.ReadAsync(noReason)).Errors!["reason"].Message);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noOutcome.StatusCode);
        Assert.Equal("OUTCOME_INVALID", (await Problem.ReadAsync(noOutcome)).Errors!["outcome"].Code);
        Assert.Equal(HttpStatusCode.Conflict, release.StatusCode);
        Assert.Equal("ORDER_INVALID_TRANSITION", (await Problem.ReadAsync(release)).Code);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        using var refund = await admin.ResolveProblemAsync(reported.Reference, "FullRefund", "Lost in transit.");
        Assert.Equal(HttpStatusCode.OK, refund.StatusCode);
    }

    [Fact]
    public async Task Administrator_endpoints_need_the_administrator_role()
    {
        var (orderId, seller) = await DeliveredOrderAsync();
        var reported = await ReportAsync(orderId);
        using var anonymous = _factory.CreateMemberClient();

        using var buyerReads = await _buyer.AdminProblemAsync(reported.Reference);
        using var sellerResolves = await seller.ResolveProblemAsync(reported.Reference, "ReleaseToSeller", "Mine.");
        using var visitorReads = await anonymous.AdminProblemAsync(reported.Reference);

        Assert.Equal(HttpStatusCode.Forbidden, buyerReads.StatusCode);
        Assert.Equal("ADMINISTRATOR_REQUIRED", (await Problem.ReadAsync(buyerReads)).Code);
        Assert.Equal(HttpStatusCode.Forbidden, sellerResolves.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, visitorReads.StatusCode);
        _factory.InDatabase(db => Assert.Equal(ProblemStatus.Open, db.OrderProblems.Single().Status));
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _factory.Dispose();
    }

    private static byte[][] Photos(int count) => [.. Enumerable.Range(0, count).Select(_ => Images.Jpeg(400, 300))];

    private async Task<(Guid OrderId, HttpClient Seller)> DeliveredOrderAsync()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DeliverAsync(orderId, seller, Now().AddHours(-1));
        return (orderId, seller);
    }

    private async Task<ProblemView> ReportAsync(Guid orderId)
    {
        using var response = await _buyer.ReportProblemAsync(orderId, "NotAsDescribed", "Pilling all over the front.", Images.Jpeg(1200, 900));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProblemView>())!;
    }

    private async Task<HttpClient> StrangerAsync()
    {
        var stranger = _factory.CreateMemberClient();
        await stranger.SignInVerifiedMemberAsync(_factory, "tall_tom", "tall.tom@example.com");
        return stranger;
    }

    private async Task<int> RunCompletionJobAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CompleteDeliveredOrdersCommand());
    }

    private DateTime Now() => _factory.Clock.GetUtcNow().UtcDateTime;
}
