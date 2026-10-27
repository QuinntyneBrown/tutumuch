using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Domain.Orders;
using TuTuMuch.Domain.Payouts;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Application.Abstractions;

/// <summary>The unit of work over TuTuMuch database. Implemented by TuTuMuchDbContext in Infrastructure.</summary>
public interface ITuTuMuchDbContext
{
    DbSet<Member> Members { get; }

    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }

    DbSet<Session> Sessions { get; }

    DbSet<ExternalLogin> ExternalLogins { get; }

    DbSet<PendingExternalJoin> PendingExternalJoins { get; }

    DbSet<Listing> Listings { get; }

    DbSet<ListingPhoto> ListingPhotos { get; }

    DbSet<Checkout> Checkouts { get; }

    DbSet<Payment> Payments { get; }

    DbSet<Refund> Refunds { get; }

    DbSet<Order> Orders { get; }

    DbSet<SavedAddress> SavedAddresses { get; }

    DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; }

    DbSet<Payout> Payouts { get; }

    DbSet<SellerPayoutAccount> SellerPayoutAccounts { get; }

    DbSet<OutboxMessage> OutboxMessages { get; }

    DbSet<OrderProblem> OrderProblems { get; }

    DbSet<NotificationPreferences> NotificationPreferences { get; }

    DbSet<ItemRequest> ItemRequests { get; }

    DbSet<ItemRequestMatch> ItemRequestMatches { get; }

    DbSet<ShowcaseRequest> ShowcaseRequests { get; }

    /// <summary>Transactions, for the steps of checkout that must commit together.</summary>
    DatabaseFacade Database { get; }

    /// <summary>Lets a batch job drop the changes of one item that lost a concurrency check.</summary>
    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Hashes and checks member passwords.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}

/// <summary>
/// The identity Google asserted in a validated ID token: its stable subject, the email and whether
/// Google verified it, and the nonce the token carries (L2-088).
/// </summary>
public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? Nonce);

/// <summary>
/// Google as an OpenID Connect provider (L2-088, ADR accounts/0001). The adapter exchanges the
/// authorization code with the PKCE verifier and validates the ID token's signature, issuer,
/// audience and expiry against Google's published keys; tests use a fake.
/// </summary>
public interface IGoogleIdentityProvider
{
    /// <summary>The OAuth client ID, or null when no Google client is configured and the feature is off.</summary>
    string? ClientId { get; }

    /// <summary>The validated identity, or null when Google refuses the code or the token is invalid.</summary>
    Task<GoogleIdentity?> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken);
}

/// <summary>Known breached passwords (L2-001).</summary>
public interface IBreachedPasswords
{
    bool Contains(string password);
}

/// <summary>
/// The external email provider (L2-042), still to be chosen; tests use a fake. A send throws
/// <see cref="HttpRequestException"/> or <see cref="TimeoutException"/> for a failure worth
/// retrying (timeout, 429, 5xx) and <see cref="EmailRejectedException"/> for one that is not.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// One email; the provider sends a repeated key once. <paramref name="Headers"/> are extra mail
/// headers, such as List-Unsubscribe on marketing email (L2-043).
/// </summary>
public sealed record EmailMessage(string To, string Subject, string Body, Guid IdempotencyKey, IReadOnlyDictionary<string, string>? Headers = null);

/// <summary>The provider refused the email for good, for example a rejected address (HTTP 4xx other than 429).</summary>
public sealed class EmailRejectedException(string reason) : Exception(reason);

/// <summary>Reads whether a seller finished payout onboarding (L2-031). Owned by seller-payouts.</summary>
public interface ISellerPayoutAccountReader
{
    Task<bool> IsOnboardedAsync(Guid sellerId, CancellationToken cancellationToken);
}

/// <summary>An image identified from its content (magic bytes and header), not its name (L2-017).</summary>
public sealed record ImageInfo(string ContentType, int Width, int Height);

/// <summary>Reads the type and the displayed dimensions of an image without decoding all of it.</summary>
public interface IImageInspector
{
    /// <summary>The image, or null when the content is not JPEG, PNG or WebP.</summary>
    ImageInfo? Inspect(byte[] content);
}

/// <summary>One re-encoded photo version before it is stored.</summary>
public sealed record ProcessedPhotoVersion(int Size, PhotoFormat Format, int Width, int Height, byte[] Content);

/// <summary>
/// Applies the orientation, drops every metadata block (EXIF, XMP, IPTC, GPS, ICC) and re-encodes
/// the photo at each target width as WebP and JPEG, never upscaling (L2-017, L2-059).
/// </summary>
public interface IListingPhotoProcessor
{
    IReadOnlyList<ProcessedPhotoVersion> Process(byte[] original, IReadOnlyList<int> sizes);
}

/// <summary>A stored object and its content type.</summary>
public sealed record StoredObject(byte[] Content, string ContentType);

/// <summary>
/// Photo object storage. Originals are private; versions are public and immutable under their
/// content-addressed key. Real object storage and the CDN stay behind this port.
/// </summary>
public interface IPhotoStorage
{
    Task SaveOriginalAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken);

    Task SavePublicAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken);

    Task<StoredObject?> ReadPublicAsync(string key, CancellationToken cancellationToken);

    /// <summary>A private object, such as a problem photo (L2-036), which is never published to the CDN.</summary>
    Task<StoredObject?> ReadOriginalAsync(string key, CancellationToken cancellationToken);

    /// <summary>Removes a private object; a missing key is not an error.</summary>
    Task DeleteOriginalAsync(string key, CancellationToken cancellationToken);

    /// <summary>Removes a public version; a missing key is not an error.</summary>
    Task DeletePublicAsync(string key, CancellationToken cancellationToken);
}

/// <summary>A payment to create at the provider. The key makes a repeated request charge once (L2-030 AC6).</summary>
public sealed record PaymentRequest(Guid PaymentId, Guid CheckoutId, Guid ListingId, decimal Amount, string PaymentMethodToken, Guid IdempotencyKey);

/// <summary>Kinds of provider webhook events TuTuMuch acts on.</summary>
public enum PaymentEventType
{
    Succeeded,
    Failed,
}

/// <summary>
/// One verified provider webhook event. PaymentId is the TuTuMuch payment ID sent as metadata with the
/// payment; the card brand and last four digits are the only card details it carries (L2-030).
/// </summary>
public sealed record PaymentEvent(
    string EventId,
    PaymentEventType Type,
    Guid PaymentId,
    string ProviderPaymentId,
    string? CardBrand,
    string? CardLast4,
    string? DeclineReason);

/// <summary>
/// The PCI DSS compliant payment provider (L2-030). Card details go from the provider's hosted
/// fields to the provider; TuTuMuch only ever holds the payment method token. Tests use a fake.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Sales tax on item plus shipping for this US address, as the provider computes it.</summary>
    Task<decimal> QuoteTaxAsync(decimal taxableAmount, ShippingAddress address, CancellationToken cancellationToken);

    /// <summary>Asks the provider to charge the token; the result arrives later by webhook. Returns the provider's payment ID.</summary>
    Task<string> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken);

    /// <summary>Refunds a payment in full; the key makes a repeated call refund once. Returns the provider's refund ID.</summary>
    Task<string> RefundAsync(string providerPaymentId, decimal amount, Guid idempotencyKey, CancellationToken cancellationToken);

    /// <summary>The event when the signature over the raw body is valid, otherwise null (L2-030 AC4).</summary>
    PaymentEvent? ReadWebhook(string body, string? signature);
}

/// <summary>Kinds of payout webhook events TuTuMuch acts on.</summary>
public enum PayoutEventType
{
    AccountUpdated,
    PayoutPaid,
    PayoutFailed,
}

/// <summary>
/// One verified payout webhook event. Account events carry the provider account ID, whether
/// onboarding is complete and the bank's display name and last four digits; payout events carry
/// the TuTuMuch payout ID sent as the idempotency key (L2-031).
/// </summary>
public sealed record PayoutEvent(
    string EventId,
    PayoutEventType Type,
    string? ProviderAccountId,
    bool AccountComplete,
    string? BankName,
    string? BankLast4,
    Guid? PayoutId,
    string? FailureReason);

/// <summary>
/// The payout side of the payment provider (L2-031, ADR orders/0005): connected accounts, the
/// provider-hosted onboarding, transfers to the seller's bank and their webhook. Failures throw
/// <see cref="HttpRequestException"/>. Tests use a fake; no real credentials exist yet.
/// </summary>
public interface IPayoutProvider
{
    /// <summary>Creates the seller's connected account; returns the provider account ID.</summary>
    Task<string> CreateConnectedAccountAsync(Guid memberId, string email, CancellationToken cancellationToken);

    /// <summary>A short-lived link to the provider-hosted onboarding for the account.</summary>
    Task<string> CreateOnboardingLinkAsync(string accountId, string returnUrl, string refreshUrl, CancellationToken cancellationToken);

    /// <summary>Pays the amount to the account; the key makes a repeated call pay once. Returns the provider payout ID.</summary>
    Task<string> CreatePayoutAsync(string accountId, decimal amount, Guid idempotencyKey, CancellationToken cancellationToken);

    /// <summary>The event when the signature over the raw body is valid, otherwise null.</summary>
    PayoutEvent? ReadWebhook(string body, string? signature);
}

/// <summary>One verified carrier tracking event: the number as the carrier stores it and whether it reports delivery.</summary>
public sealed record CarrierEvent(string EventId, Carrier Carrier, string TrackingNumber, bool Delivered, DateTime OccurredAt);

/// <summary>
/// The carrier tracking service that reports delivery (L2-033 AC4). Which service, and whether it
/// pushes or is polled, is still to be chosen (ADR orders/0003); tests use a fake.
/// </summary>
public interface ICarrierTracking
{
    /// <summary>The event when the signature over the raw body is valid, otherwise null.</summary>
    CarrierEvent? ReadWebhook(string body, string? signature);
}

/// <summary>Public settings of the application.</summary>
public sealed class AppOptions
{
    /// <summary>Origin of TuTuMuch Web, used to build links in emails.</summary>
    public string BaseUrl { get; set; } = "http://localhost:4200";
}
