using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The Checkouts table. The filtered unique index allows one open or processing checkout per
/// listing, backing up the conditional update on Listings (L2-029).
/// </summary>
internal sealed class CheckoutConfiguration : IEntityTypeConfiguration<Checkout>
{
    public void Configure(EntityTypeBuilder<Checkout> builder)
    {
        builder.ToTable("Checkouts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.ItemPrice).HasPrecision(10, 2);
        builder.Property(c => c.ShippingCost).HasPrecision(10, 2);
        builder.Property(c => c.SalesTax).HasPrecision(10, 2);
        builder.Property(c => c.Total).HasPrecision(10, 2);
        builder.Property(c => c.LastDeclineReason).HasMaxLength(200);
        builder.OwnsOne(c => c.Address, a => AddressColumns.Map(a, "Shipping"));
        builder.Navigation(c => c.Address).IsRequired(false);
        builder.HasOne<Listing>().WithMany().HasForeignKey(c => c.ListingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Member>().WithMany().HasForeignKey(c => c.BuyerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.ListingId)
            .IsUnique()
            .HasFilter("Status IN ('Open', 'Processing')")
            .HasDatabaseName("UX_Checkouts_ListingId_Open");
        builder.HasIndex(c => new { c.Status, c.ExpiresAt }).HasDatabaseName("IX_Checkouts_Status_ExpiresAt");
        builder.HasIndex(c => c.BuyerId).HasDatabaseName("IX_Checkouts_BuyerId");
        builder.Ignore(c => c.HoldsListing);
    }
}

/// <summary>The Payments table: provider reference, card brand and last four only (L2-030); one row per idempotency key.</summary>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasPrecision(10, 2);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.ProviderPaymentId).HasMaxLength(100);
        builder.Property(p => p.CardBrand).HasMaxLength(20);
        builder.Property(p => p.CardLast4).HasMaxLength(4).IsFixedLength();
        builder.Property(p => p.DeclineReason).HasMaxLength(200);
        builder.HasOne<Checkout>().WithMany().HasForeignKey(p => p.CheckoutId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.IdempotencyKey).IsUnique().HasDatabaseName("UX_Payments_IdempotencyKey");
        builder.HasIndex(p => p.CheckoutId).HasDatabaseName("IX_Payments_CheckoutId");
    }
}

internal sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Amount).HasPrecision(10, 2);
        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(24);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.ProviderRefundId).HasMaxLength(100);
        builder.HasOne<Payment>().WithMany().HasForeignKey(r => r.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.PaymentId).HasDatabaseName("IX_Refunds_PaymentId");
    }
}

/// <summary>
/// The Orders table. One order per checkout, and one order per listing that is not Cancelled
/// (L2-029), so a relisted listing can sell again (L2-035). Status is a concurrency token, so two
/// transitions racing from the same status (the seller shipping while the deadline job cancels)
/// cannot both commit.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Reference).HasMaxLength(16).IsRequired();
        builder.HasIndex(o => o.Reference).IsUnique().HasDatabaseName("UX_Orders_Reference");
        builder.HasIndex(o => o.Number).IsUnique().HasDatabaseName("UX_Orders_Number");
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(16).IsConcurrencyToken();
        builder.Property(o => o.Carrier).HasConversion<string>().HasMaxLength(8);
        builder.Property(o => o.TrackingNumber).HasMaxLength(34);
        builder.Property(o => o.CancellationReason).HasConversion<string>().HasMaxLength(24);
        builder.Property(o => o.CancellationNote).HasMaxLength(Order.MaxCancellationText);
        builder.Property(o => o.CancellationRequestStatus).HasConversion<string>().HasMaxLength(16);
        builder.Property(o => o.CancellationRequestMessage).HasMaxLength(Order.MaxCancellationText);
        builder.HasMany(o => o.StatusChanges).WithOne().HasForeignKey(c => c.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.StatusChanges).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(o => o.ItemPrice).HasPrecision(10, 2);
        builder.Property(o => o.ShippingCost).HasPrecision(10, 2);
        builder.Property(o => o.SalesTax).HasPrecision(10, 2);
        builder.Property(o => o.Total).HasPrecision(10, 2);
        builder.Property(o => o.FeeRate).HasPrecision(5, 4);
        builder.Property(o => o.FeeAmount).HasPrecision(10, 2);
        builder.OwnsOne(o => o.ShippingAddress, a => AddressColumns.Map(a, "Shipping"));
        builder.Navigation(o => o.ShippingAddress).IsRequired();
        builder.HasOne<Checkout>().WithMany().HasForeignKey(o => o.CheckoutId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Listing>().WithMany().HasForeignKey(o => o.ListingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Member>().WithMany().HasForeignKey(o => o.BuyerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Member>().WithMany().HasForeignKey(o => o.SellerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(o => o.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(o => o.CheckoutId).IsUnique().HasDatabaseName("UX_Orders_CheckoutId");
        builder.HasIndex(o => o.ListingId)
            .IsUnique()
            .HasFilter("Status <> 'Cancelled'")
            .HasDatabaseName("UX_Orders_ListingId");
        builder.HasIndex(o => new { o.BuyerId, o.PaidAt }).HasDatabaseName("IX_Orders_BuyerId_PaidAt");
        builder.HasIndex(o => new { o.SellerId, o.PaidAt }).HasDatabaseName("IX_Orders_SellerId_PaidAt");
        builder.HasIndex(o => new { o.Status, o.ShipBy }).HasDatabaseName("IX_Orders_Status_ShipBy");
        builder.HasIndex(o => new { o.Status, o.ShipReminderDue }).HasDatabaseName("IX_Orders_Status_ShipReminderDue");
        builder.HasIndex(o => new { o.Status, o.DeliveredAt }).HasDatabaseName("IX_Orders_Status_DeliveredAt");
        builder.Ignore(o => o.AutoCompletesAt);
        builder.HasIndex(o => new { o.Carrier, o.TrackingNumber }).HasDatabaseName("IX_Orders_Carrier_TrackingNumber");
    }
}

/// <summary>The append-only OrderStatusChanges table (L2-032 AC2). Nothing updates or deletes a row.</summary>
internal sealed class OrderStatusChangeConfiguration : IEntityTypeConfiguration<OrderStatusChange>
{
    public void Configure(EntityTypeBuilder<OrderStatusChange> builder)
    {
        builder.ToTable("OrderStatusChanges");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.From).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.To).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.Actor).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(c => new { c.OrderId, c.OccurredAt }).HasDatabaseName("IX_OrderStatusChanges_OrderId_OccurredAt");
    }
}

internal sealed class SavedAddressConfiguration : IEntityTypeConfiguration<SavedAddress>
{
    public void Configure(EntityTypeBuilder<SavedAddress> builder)
    {
        builder.ToTable("SavedAddresses");
        builder.HasKey(a => a.MemberId);
        builder.OwnsOne(a => a.Address, a => AddressColumns.Map(a, string.Empty));
        builder.Navigation(a => a.Address).IsRequired();
        builder.HasOne<Member>().WithOne().HasForeignKey<SavedAddress>(a => a.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        builder.ToTable("PaymentWebhookEvents");
        builder.HasKey(e => e.ProviderEventId);
        builder.Property(e => e.ProviderEventId).HasMaxLength(100);
        builder.Property(e => e.Type).HasMaxLength(32).IsRequired();
    }
}

internal static class AddressColumns
{
    /// <summary>Maps a shipping address to columns named with the prefix, such as ShippingCity.</summary>
    public static void Map<TOwner>(OwnedNavigationBuilder<TOwner, ShippingAddress> address, string prefix)
        where TOwner : class
    {
        address.Property(a => a.FullName).HasColumnName($"{prefix}FullName").HasMaxLength(ShippingAddress.MaxName).IsRequired();
        address.Property(a => a.Line1).HasColumnName($"{prefix}Line1").HasMaxLength(ShippingAddress.MaxLine).IsRequired();
        address.Property(a => a.Line2).HasColumnName($"{prefix}Line2").HasMaxLength(ShippingAddress.MaxLine);
        address.Property(a => a.City).HasColumnName($"{prefix}City").HasMaxLength(ShippingAddress.MaxCity).IsRequired();
        address.Property(a => a.State).HasColumnName($"{prefix}State").HasMaxLength(2).IsRequired();
        address.Property(a => a.Zip).HasColumnName($"{prefix}Zip").HasMaxLength(10).IsRequired();
        address.Property(a => a.Country).HasColumnName($"{prefix}Country").HasMaxLength(2).IsRequired();
    }
}
