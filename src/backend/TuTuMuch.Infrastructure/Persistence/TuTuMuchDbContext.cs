using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Domain.Orders;
using TuTuMuch.Domain.Payouts;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Infrastructure.Persistence;

/// <summary>EF Core context over TuTuMuch database (SQL Server). LINQ only, so all SQL is parameterised (L2-049).</summary>
public sealed class TuTuMuchDbContext(DbContextOptions<TuTuMuchDbContext> options) : DbContext(options), ITuTuMuchDbContext
{
    public DbSet<Member> Members => Set<Member>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    public DbSet<PendingExternalJoin> PendingExternalJoins => Set<PendingExternalJoin>();

    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();

    public DbSet<Checkout> Checkouts => Set<Checkout>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Refund> Refunds => Set<Refund>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<SavedAddress> SavedAddresses => Set<SavedAddress>();

    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();

    public DbSet<Payout> Payouts => Set<Payout>();

    public DbSet<SellerPayoutAccount> SellerPayoutAccounts => Set<SellerPayoutAccount>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<OrderProblem> OrderProblems => Set<OrderProblem>();

    public DbSet<NotificationPreferences> NotificationPreferences => Set<NotificationPreferences>();

    public DbSet<ItemRequest> ItemRequests => Set<ItemRequest>();

    public DbSet<ItemRequestMatch> ItemRequestMatches => Set<ItemRequestMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TuTuMuchDbContext).Assembly);
}
