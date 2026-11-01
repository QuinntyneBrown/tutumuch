using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Infrastructure.Persistence.Configurations;

/// <summary>The Bids table (L2-115): one row per accepted bid, read by listing and time.</summary>
internal sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("Bids", t => t.HasCheckConstraint("CK_Bids_Amount", "CAST(Amount AS REAL) BETWEEN 1 AND 5000"));
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Amount).HasPrecision(7, 2);
        builder.HasOne<Listing>().WithMany().HasForeignKey(b => b.ListingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Member>().WithMany().HasForeignKey(b => b.BidderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => new { b.ListingId, b.PlacedAt }).HasDatabaseName("IX_Bids_ListingId_PlacedAt");
        builder.HasIndex(b => new { b.BidderId, b.ListingId }).HasDatabaseName("IX_Bids_BidderId_ListingId");
    }
}
