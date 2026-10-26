using System.Net.Http.Headers;
using System.Net.Http.Json;
using TutuMuch.Domain.Members;

namespace TutuMuch.Api.Tests.Support;

/// <summary>Calls for the order problem endpoints of resolve-order-problems (L2-036).</summary>
public static class OrderProblems
{
    /// <summary>The buyer reports a problem as multipart/form-data with up to 4 photos.</summary>
    public static Task<HttpResponseMessage> ReportProblemAsync(this HttpClient client, Guid orderId, string? reason, string? description, params byte[][] photos)
    {
        var form = new MultipartFormDataContent();
        if (reason is not null)
        {
            form.Add(new StringContent(reason), "reason");
        }

        if (description is not null)
        {
            form.Add(new StringContent(description), "description");
        }

        foreach (var (photo, index) in photos.Select((p, i) => (p, i)))
        {
            var file = new ByteArrayContent(photo);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(file, "photos", $"photo{index}.jpg");
        }

        return client.PostAsync($"/api/orders/{orderId}/problem", form);
    }

    public static Task<HttpResponseMessage> RespondToProblemAsync(this HttpClient client, Guid orderId, string? message) =>
        client.PostAsJsonAsync($"/api/orders/{orderId}/problem/response", new { message });

    public static Task<HttpResponseMessage> AdminProblemAsync(this HttpClient client, string reference) =>
        client.GetAsync($"/api/admin/problems/{reference}");

    public static Task<HttpResponseMessage> ResolveProblemAsync(this HttpClient client, string reference, string? outcome, string? reason) =>
        client.PostAsJsonAsync($"/api/admin/problems/{reference}/resolution", new { outcome, reason });

    /// <summary>A client signed in as a new verified member who holds the Administrator role.</summary>
    public static HttpClient CreateAdministratorClient(this TutuMuchApiFactory factory, string displayName = "ana_ruiz")
    {
        Member admin = null!;
        factory.InDatabase(db =>
        {
            admin = Member.Register(displayName, $"{displayName}@tutumuch.test", "hash", DateTime.UtcNow);
            admin.MarkEmailVerified(DateTime.UtcNow);
            admin.GrantAdministrator();
            db.Members.Add(admin);
            db.SaveChanges();
        });
        return factory.CreateMemberClient().WithSession(factory, admin);
    }
}

public sealed record ProblemPhotoView(Guid Id, string Url);

public sealed record ProblemView(
    string Reference,
    string Reason,
    string ReasonLabel,
    string Description,
    IReadOnlyList<ProblemPhotoView> Photos,
    string Status,
    DateTime OpenedAt,
    string? SellerResponse,
    DateTime? SellerRespondedAt,
    string? Outcome,
    string? ResolutionReason,
    DateTime? ResolvedAt);

public sealed record PurchaseWithProblemView(Guid Id, string Status, bool CanReportProblem, DateTime? ProblemDeadline, ProblemView? Problem);

public sealed record SaleWithProblemView(Guid Id, string Status, ProblemView? Problem, SalePayoutView? Payout);

public sealed record ProblemOrderView(Guid Id, string Reference, string Status, string Total, DateTime? DeliveredAt, CheckoutItemView Item);

public sealed record ProblemDetailView(
    ProblemView Problem,
    string BuyerDisplayName,
    string SellerDisplayName,
    ProblemOrderView Order,
    string Payout);
