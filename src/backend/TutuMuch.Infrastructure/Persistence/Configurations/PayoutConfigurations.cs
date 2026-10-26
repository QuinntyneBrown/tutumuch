using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutuMuch.Domain.Members;
using TutuMuch.Domain.Orders;
using TutuMuch.Domain.Payouts;

namespace TutuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The Payouts table: one payout per order. Status is a concurrency token, so the release job and
/// a webhook racing on the same payout cannot both commit (L2-031).
/// </summary>
internal sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("Payouts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.GrossAmount).HasPrecision(10, 2);
        builder.Property(p => p.FeeRate).HasPrecision(5, 4);
        builder.Property(p => p.FeeAmount).HasPrecision(10, 2);
        builder.Property(p => p.NetAmount).HasPrecision(10, 2);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16).IsConcurrencyToken();
        builder.Property(p => p.HoldReason).HasConversion<string>().HasMaxLength(24);
        builder.Property(p => p.ProviderPayoutId).HasMaxLength(100);
        builder.Property(p => p.FailureReason).HasMaxLength(200);
        builder.HasOne<Order>().WithMany().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Member>().WithMany().HasForeignKey(p => p.SellerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.OrderId).IsUnique().HasDatabaseName("UX_Payouts_OrderId");
        builder.HasIndex(p => p.Status).HasDatabaseName("IX_Payouts_Status");
        builder.HasIndex(p => new { p.SellerId, p.Status }).HasDatabaseName("IX_Payouts_SellerId_Status");
    }
}

/// <summary>The SellerPayoutAccounts table: one connected account per member; no bank account number.</summary>
internal sealed class SellerPayoutAccountConfiguration : IEntityTypeConfiguration<SellerPayoutAccount>
{
    public void Configure(EntityTypeBuilder<SellerPayoutAccount> builder)
    {
        builder.ToTable("SellerPayoutAccounts");
        builder.HasKey(a => a.MemberId);
        builder.Property(a => a.ProviderAccountId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(a => a.BankName).HasMaxLength(60);
        builder.Property(a => a.BankLast4).HasMaxLength(4).IsFixedLength();
        builder.HasOne<Member>().WithMany().HasForeignKey(a => a.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.ProviderAccountId).IsUnique().HasDatabaseName("UX_SellerPayoutAccounts_ProviderAccountId");
        builder.Ignore(a => a.BankDisplay);
    }
}
