using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TuTuMuch.Domain.Finds;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The ShelfFinds table (L2-113, ADR catalog/0009). Deleted with the poster, so erasing a member
/// erases their finds (L2-121). The list reads Active finds newest first.
/// </summary>
internal sealed class ShelfFindConfiguration : IEntityTypeConfiguration<ShelfFind>
{
    public void Configure(EntityTypeBuilder<ShelfFind> builder)
    {
        builder.ToTable("ShelfFinds");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Reference).HasMaxLength(16).IsRequired();
        builder.HasIndex(f => f.Reference).IsUnique().HasDatabaseName("UX_ShelfFinds_Reference");
        builder.HasIndex(f => f.Number).IsUnique().HasDatabaseName("UX_ShelfFinds_Number");
        builder.Property(f => f.Title).HasMaxLength(ShelfFind.MaxTitle).IsRequired();
        builder.Property(f => f.Category).HasMaxLength(40).IsRequired();
        builder.Property(f => f.ShelfPrice).HasPrecision(10, 2);
        builder.Property(f => f.GrabFee).HasPrecision(10, 2);
        builder.Property(f => f.StoreName).HasMaxLength(ShelfFind.MaxStoreName).IsRequired();
        builder.Property(f => f.StoreAddress).HasMaxLength(ShelfFind.MaxStoreAddress).IsRequired();
        builder.Property(f => f.Note).HasMaxLength(ShelfFind.MaxNote);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(16);
        builder.Ignore(f => f.ExpiresOn);
        builder.Ignore(f => f.OffersGrab);
        builder.HasOne<Member>().WithMany().HasForeignKey(f => f.PosterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(f => new { f.Status, f.SeenOn, f.CreatedAt }).HasDatabaseName("IX_ShelfFinds_Status_SeenOn_CreatedAt");
        builder.HasIndex(f => new { f.PosterId, f.CreatedAt }).HasDatabaseName("IX_ShelfFinds_PosterId_CreatedAt");
    }
}

/// <summary>
/// The Grabs table (L2-117, ADR orders/0009). The filtered unique index allows one open grab per
/// find, so the database decides a race. Status is a concurrency token, so an administrator, the
/// shopper and the deadline job cannot all move one grab. Deleted with the shopper (L2-121); the
/// find restricts its delete, leaving one cascade path from a member.
/// </summary>
internal sealed class GrabConfiguration : IEntityTypeConfiguration<Grab>
{
    public void Configure(EntityTypeBuilder<Grab> builder)
    {
        builder.ToTable("Grabs");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Reference).HasMaxLength(16).IsRequired();
        builder.HasIndex(g => g.Reference).IsUnique().HasDatabaseName("UX_Grabs_Reference");
        builder.HasIndex(g => g.Number).IsUnique().HasDatabaseName("UX_Grabs_Number");
        builder.Property(g => g.Status).HasConversion<string>().HasMaxLength(16).IsConcurrencyToken();
        builder.Property(g => g.UnavailableReason).HasConversion<string>().HasMaxLength(16);
        builder.Property(g => g.PaymentDeclineReason).HasMaxLength(200);
        builder.Property(g => g.ShelfPrice).HasPrecision(10, 2);
        builder.Property(g => g.GrabFee).HasPrecision(10, 2);
        builder.Property(g => g.SalesTax).HasPrecision(10, 2);
        builder.Property(g => g.Total).HasPrecision(10, 2);
        builder.Property(g => g.Deposit).HasPrecision(10, 2);
        builder.Property(g => g.RefundedAmount).HasPrecision(10, 2);
        builder.OwnsOne(g => g.Address, a => AddressColumns.Map(a, "Shipping"));
        builder.Navigation(g => g.Address).IsRequired();
        builder.Ignore(g => g.IsOpen);
        builder.Ignore(g => g.Deadline);
        builder.Ignore(g => g.RefundIfCancelled);
        builder.HasOne<ShelfFind>().WithMany().HasForeignKey(g => g.FindId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Member>().WithMany().HasForeignKey(g => g.ShopperId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(g => g.FindId)
            .IsUnique()
            .HasFilter("Status IN ('PaymentPending', 'Requested', 'Accepted', 'Bought')")
            .HasDatabaseName("UX_Grabs_FindId_Open");
        builder.HasIndex(g => new { g.ShopperId, g.CreatedAt }).HasDatabaseName("IX_Grabs_ShopperId_CreatedAt");
        builder.HasIndex(g => g.Status).HasDatabaseName("IX_Grabs_Status");
    }
}
