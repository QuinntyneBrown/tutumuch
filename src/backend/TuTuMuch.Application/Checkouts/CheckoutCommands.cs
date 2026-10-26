using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Catalog;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Listings;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Application.Checkouts;

/// <summary>The fields of a shipping address as the buyer sends them; any may be missing.</summary>
public sealed record AddressInput(string? FullName, string? Line1, string? Line2, string? City, string? State, string? Zip, string? Country);

/// <summary>Reserves the listing for the buyer for 10 minutes (L2-028), refusing their own listing (L2-026 AC2).</summary>
public sealed record StartCheckoutCommand(Guid BuyerId, string ListingReference) : IRequest<Result<CheckoutDto>>;

public sealed record GetCheckoutQuery(Guid BuyerId, Guid CheckoutId) : IRequest<Result<CheckoutDto>>;

/// <summary>Sets the US shipping address and the provider's tax quote (L2-027 AC2, AC3).</summary>
public sealed record SetCheckoutAddressCommand(Guid BuyerId, Guid CheckoutId, AddressInput Address) : IRequest<Result<CheckoutDto>>;

/// <summary>Charges the token for the total the buyer saw (L2-027 AC5), once per idempotency key (L2-030 AC6).</summary>
public sealed record SubmitPaymentCommand(Guid BuyerId, Guid CheckoutId, Guid IdempotencyKey, string PaymentMethodToken, decimal ExpectedTotal) : IRequest<Result<PaymentDto>>;

/// <summary>"Cancel and release item": the listing is Active again at once (L2-028 AC4).</summary>
public sealed record ReleaseCheckoutCommand(Guid BuyerId, Guid CheckoutId) : IRequest<Result<bool>>;

public sealed class CheckoutCommandHandlers(
    ITuTuMuchDbContext db,
    IPaymentProvider provider,
    IOptions<PhotoOptions> photoOptions,
    TimeProvider clock) :
    IRequestHandler<StartCheckoutCommand, Result<CheckoutDto>>,
    IRequestHandler<GetCheckoutQuery, Result<CheckoutDto>>,
    IRequestHandler<SetCheckoutAddressCommand, Result<CheckoutDto>>,
    IRequestHandler<SubmitPaymentCommand, Result<PaymentDto>>,
    IRequestHandler<ReleaseCheckoutCommand, Result<bool>>
{
    public static readonly Error OwnListing = new(422, "CANNOT_BUY_OWN_LISTING", "You can't buy your own listing");
    public static readonly Error Reserved = new(409, "LISTING_RESERVED", "Someone else is checking out");
    public static readonly Error Unavailable = new(409, "LISTING_UNAVAILABLE", "This item isn't available");
    public static readonly Error Expired = new(409, "RESERVATION_EXPIRED", "Your reservation ended");
    public static readonly Error Processing = new(409, "PAYMENT_IN_PROGRESS", "Your payment is being confirmed");
    public static readonly Error AddressRequired = new(409, "ADDRESS_REQUIRED", "Add a shipping address first");
    public static readonly Error TotalChanged = new(409, "TOTAL_CHANGED", "The total changed. Check it and pay again");
    public static readonly Error AlreadyPaid = new(409, "CHECKOUT_PAID", "This checkout is already paid");
    public static readonly Error ProviderUnavailable = new(502, "PAYMENT_PROVIDER_UNAVAILABLE", "We couldn't reach the payment provider");

    public async Task<Result<CheckoutDto>> Handle(StartCheckoutCommand command, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.AsNoTracking().SingleOrDefaultAsync(l => l.Reference == command.ListingReference, cancellationToken);
        if (listing is null || !ListingDetailVisibility.CanView(listing.Status, listing.SellerId, command.BuyerId))
        {
            return Error.NotFound("LISTING_NOT_FOUND");
        }

        if (!listing.CanBePurchasedBy(command.BuyerId))
        {
            return OwnListing;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var mine = await db.Checkouts.SingleOrDefaultAsync(
            c => c.ListingId == listing.Id && c.BuyerId == command.BuyerId && (c.Status == CheckoutStatus.Open || c.Status == CheckoutStatus.Processing),
            cancellationToken);
        if (mine is not null && mine.IsLive(now))
        {
            // Opening checkout again (a refresh) returns the buyer's own reservation.
            return await ToDtoAsync(mine, listing, now, cancellationToken);
        }

        var reservationLapsed = listing.Status == ListingStatus.Reserved && listing.ReservedUntil <= now;
        if (listing.Status == ListingStatus.Reserved && !reservationLapsed)
        {
            return Reserved;
        }

        if (listing.Status is not (ListingStatus.Active or ListingStatus.Reserved))
        {
            return Unavailable;
        }

        var checkout = Checkout.Start(Guid.NewGuid(), listing.Id, command.BuyerId, listing.Price ?? 0m, listing.ShippingCost ?? 0m, now);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // The database decides the winner (L2-029): only one conditional update can match an
            // Active listing, or a Reserved one whose 10 minutes have passed.
            var reserved = await db.Listings
                .Where(l => l.Id == listing.Id && (l.Status == ListingStatus.Active || (l.Status == ListingStatus.Reserved && l.ReservedUntil <= now)))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(l => l.Status, ListingStatus.Reserved)
                        .SetProperty(l => l.ReservedByCheckoutId, checkout.Id)
                        .SetProperty(l => l.ReservedUntil, checkout.ExpiresAt)
                        .SetProperty(l => l.Version, l => l.Version + 1),
                    cancellationToken);
            if (reserved == 0)
            {
                return Reserved;
            }

            await db.Checkouts
                .Where(c => c.ListingId == listing.Id && (c.Status == CheckoutStatus.Open || c.Status == CheckoutStatus.Processing))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, CheckoutStatus.Expired).SetProperty(c => c.EndedAt, now), cancellationToken);
            db.Checkouts.Add(checkout);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The filtered unique index on open checkouts backs up the conditional update.
            return Reserved;
        }

        return await ToDtoAsync(checkout, listing, now, cancellationToken);
    }

    public async Task<Result<CheckoutDto>> Handle(GetCheckoutQuery query, CancellationToken cancellationToken)
    {
        var checkout = await FindAsync(query.BuyerId, query.CheckoutId, cancellationToken);
        if (checkout is null)
        {
            return NotFound;
        }

        var listing = await db.Listings.AsNoTracking().SingleAsync(l => l.Id == checkout.ListingId, cancellationToken);
        return await ToDtoAsync(checkout, listing, clock.GetUtcNow().UtcDateTime, cancellationToken);
    }

    public async Task<Result<CheckoutDto>> Handle(SetCheckoutAddressCommand command, CancellationToken cancellationToken)
    {
        var checkout = await FindAsync(command.BuyerId, command.CheckoutId, cancellationToken);
        if (checkout is null)
        {
            return NotFound;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        if (StateError(checkout, now) is { } stateError)
        {
            return stateError;
        }

        var input = command.Address;
        if (ShippingAddress.IsOutsideUnitedStates(input.Country))
        {
            return Error.Invalid(
                "NON_US_ADDRESS",
                new Dictionary<string, FieldError> { ["country"] = new("NON_US_ADDRESS", "Choose a US address to continue.") },
                "TuTuMuch ships within the United States only");
        }

        var violations = ShippingAddress.Check(input.FullName, input.Line1, input.Line2, input.City, input.State, input.Zip);
        if (violations.Count > 0)
        {
            return Error.Invalid("ADDRESS_INVALID", violations.ToDictionary(v => v.Field, v => new FieldError(v.Code, v.Message)));
        }

        var address = ShippingAddress.Create(input.FullName!, input.Line1!, input.Line2, input.City!, input.State!, input.Zip!);
        var tax = await provider.QuoteTaxAsync(checkout.ItemPrice + checkout.ShippingCost, address, cancellationToken);
        checkout.SetAddress(address, decimal.Round(tax, 2, MidpointRounding.AwayFromZero));
        await db.SaveChangesAsync(cancellationToken);
        var listing = await db.Listings.AsNoTracking().SingleAsync(l => l.Id == checkout.ListingId, cancellationToken);
        return await ToDtoAsync(checkout, listing, now, cancellationToken);
    }

    public async Task<Result<PaymentDto>> Handle(SubmitPaymentCommand command, CancellationToken cancellationToken)
    {
        var checkout = await FindAsync(command.BuyerId, command.CheckoutId, cancellationToken);
        if (checkout is null)
        {
            return NotFound;
        }

        var repeat = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.IdempotencyKey == command.IdempotencyKey, cancellationToken);
        if (repeat is not null)
        {
            return repeat.CheckoutId == checkout.Id ? new PaymentDto(repeat.Id, repeat.Status.ToString()) : NotFound;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        if (StateError(checkout, now) is { } stateError)
        {
            return stateError;
        }

        if (checkout.Total is not { } total)
        {
            return AddressRequired;
        }

        if (command.ExpectedTotal != total)
        {
            return TotalChanged;
        }

        var payment = Payment.Create(checkout.Id, command.IdempotencyKey, total, now);
        checkout.BeginPayment();
        db.Payments.Add(payment);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A double click raced this request with the same key; the first one is the payment.
            var first = await db.Payments.AsNoTracking().SingleAsync(p => p.IdempotencyKey == command.IdempotencyKey, cancellationToken);
            return new PaymentDto(first.Id, first.Status.ToString());
        }

        try
        {
            var providerPaymentId = await provider.CreatePaymentAsync(
                new PaymentRequest(payment.Id, checkout.Id, checkout.ListingId, total, command.PaymentMethodToken, command.IdempotencyKey),
                cancellationToken);
            payment.Accepted(providerPaymentId);
        }
        catch (HttpRequestException)
        {
            payment.Fail(string.Empty, "The payment couldn't be started", now);
            checkout.RecordDecline("We couldn't reach the payment provider. Try again.");
            await db.SaveChangesAsync(cancellationToken);
            return ProviderUnavailable;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new PaymentDto(payment.Id, payment.Status.ToString());
    }

    public async Task<Result<bool>> Handle(ReleaseCheckoutCommand command, CancellationToken cancellationToken)
    {
        var checkout = await FindAsync(command.BuyerId, command.CheckoutId, cancellationToken);
        if (checkout is null)
        {
            return NotFound;
        }

        if (checkout.Status == CheckoutStatus.Paid)
        {
            return AlreadyPaid;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var listing = await db.Listings.SingleAsync(l => l.Id == checkout.ListingId, cancellationToken);
        checkout.Release(CheckoutStatus.Released, now);
        listing.ReleaseReservation(checkout.Id, now);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static readonly Error NotFound = Error.NotFound("CHECKOUT_NOT_FOUND");

    private static Error? StateError(Checkout checkout, DateTime now) =>
        checkout.Status == CheckoutStatus.Paid ? AlreadyPaid
        : !checkout.IsLive(now) ? Expired
        : checkout.Status == CheckoutStatus.Processing ? Processing
        : null;

    /// <summary>Another member's checkout answers as missing (L2-010).</summary>
    private Task<Checkout?> FindAsync(Guid buyerId, Guid checkoutId, CancellationToken cancellationToken) =>
        db.Checkouts.SingleOrDefaultAsync(c => c.Id == checkoutId && c.BuyerId == buyerId, cancellationToken);

    private async Task<CheckoutDto> ToDtoAsync(Checkout checkout, Listing listing, DateTime now, CancellationToken cancellationToken)
    {
        var cover = await db.ListingPhotos.AsNoTracking()
            .Where(p => p.ListingId == listing.Id).OrderBy(p => p.Position)
            .FirstOrDefaultAsync(cancellationToken);
        var saved = await db.SavedAddresses.AsNoTracking()
            .Where(a => a.MemberId == checkout.BuyerId)
            .FirstOrDefaultAsync(cancellationToken);
        var photo = cover is null ? null : CatalogPhotos.ToDto(cover, string.Empty, photoOptions.Value.PublicBaseUrl.TrimEnd('/'));
        return CheckoutViews.ToDto(checkout, listing, photo, saved?.Address, now);
    }
}
