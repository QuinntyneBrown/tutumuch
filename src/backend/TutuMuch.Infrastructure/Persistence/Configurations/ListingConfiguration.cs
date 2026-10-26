using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutuMuch.Domain.Listings;
using TutuMuch.Domain.Members;

namespace TutuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The Listings table. Prices are exact decimals (L2-018); the size check backs up L2-013. The
/// range checks cast to REAL so they hold on SQL Server and on the SQLite provider the tests use.
/// </summary>
internal sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("Listings", t =>
        {
            t.HasCheckConstraint("CK_Listings_Size", "Size = 'Girls'''");
            t.HasCheckConstraint("CK_Listings_Price", "Price IS NULL OR CAST(Price AS REAL) BETWEEN 1 AND 5000");
            t.HasCheckConstraint("CK_Listings_ShippingCost", "ShippingCost IS NULL OR CAST(ShippingCost AS REAL) BETWEEN 0 AND 100");
        });
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Reference).HasMaxLength(16).IsRequired();
        builder.HasIndex(l => l.Reference).IsUnique().HasDatabaseName("UX_Listings_Reference");
        builder.HasIndex(l => l.Number).IsUnique().HasDatabaseName("UX_Listings_Number");
        builder.Property(l => l.RemovalReason).HasConversion<string>().HasMaxLength(24);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(l => l.Version).IsConcurrencyToken();
        builder.Property(l => l.Category).HasMaxLength(16).IsRequired();
        builder.Property(l => l.Subcategory).HasMaxLength(24);
        builder.Property(l => l.Size).HasMaxLength(16).IsRequired();
        builder.Property(l => l.Title).HasMaxLength(Listing.MaxTitle);
        builder.Property(l => l.Description).HasMaxLength(Listing.MaxDescription);
        builder.Property(l => l.Brand).HasMaxLength(Listing.MaxBrand);
        builder.Property(l => l.Condition).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Flaws).HasMaxLength(Listing.MaxFlaws);
        builder.Property(l => l.Price).HasPrecision(7, 2);
        builder.Property(l => l.ShippingCost).HasPrecision(5, 2);
        builder.HasOne<Member>().WithMany().HasForeignKey(l => l.SellerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => new { l.SellerId, l.CreatedAt }).HasDatabaseName("IX_Listings_SellerId_CreatedAt");
        builder.HasIndex(l => new { l.SellerId, l.Status, l.CreatedAt }).HasDatabaseName("IX_Listings_SellerId_Status_CreatedAt");
        builder.HasIndex(l => new { l.SellerId, l.PublishedAt }).HasDatabaseName("IX_Listings_SellerId_PublishedAt");
        builder.HasIndex(l => new { l.Status, l.PublishedAt }).HasDatabaseName("IX_Listings_Status_PublishedAt");
        builder.HasIndex(l => new { l.Status, l.ReservedUntil }).HasDatabaseName("IX_Listings_Status_ReservedUntil");
        builder.HasIndex(l => l.UpdatedAt).HasDatabaseName("IX_Listings_Draft_UpdatedAt").HasFilter("Status = 'Draft'");
    }
}
