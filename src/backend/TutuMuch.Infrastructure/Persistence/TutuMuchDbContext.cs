using Microsoft.EntityFrameworkCore;
using TutuMuch.Application.Abstractions;
using TutuMuch.Domain.Listings;
using TutuMuch.Domain.Members;
using TutuMuch.Domain.Notifications;
using TutuMuch.Domain.Orders;
using TutuMuch.Domain.Payouts;

namespace TutuMuch.Infrastructure.Persistence;

/// <summary>EF Core context over Tutu Much database (SQL Server). LINQ only, so all SQL is parameterised (L2-049).</summary>
public sealed class TutuMuchDbContext(DbContextOptions<TutuMuchDbContext> options) : DbContext(options), ITutuMuchDbContext
{
    public DbSet<Member> Members => Set<Member>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<Session> Sessions => Set<Session>();

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

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TutuMuchDbContext).Assembly);
}
