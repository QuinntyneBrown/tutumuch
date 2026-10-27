using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The ItemRequests table (L2-097): deleted with the member, so erasing a member erases their
/// requests (L2-106). The size check backs up L2-013 as on Listings.
/// </summary>
internal sealed class ItemRequestConfiguration : IEntityTypeConfiguration<ItemRequest>
{
    public void Configure(EntityTypeBuilder<ItemRequest> builder)
    {
        builder.ToTable("ItemRequests", t =>
        {
            t.HasCheckConstraint("CK_ItemRequests_Size", "Size = 'Girls'''");
            t.HasCheckConstraint("CK_ItemRequests_MaxPrice", "MaxPrice IS NULL OR CAST(MaxPrice AS REAL) BETWEEN 1 AND 5000");
        });
        builder.HasKey(r => r.Id);
        builder.Ignore(r => r.Words);
        builder.Property(r => r.Category).HasMaxLength(16).IsRequired();
        builder.Property(r => r.Size).HasMaxLength(16).IsRequired();
        builder.Property(r => r.Keywords).HasMaxLength(ItemRequest.MaxKeywords).IsRequired();
        builder.Property(r => r.MaxPrice).HasPrecision(7, 2);
        builder.Property(r => r.MinimumCondition).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<Member>().WithMany().HasForeignKey(r => r.MemberId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => new { r.MemberId, r.CreatedAt }).HasDatabaseName("IX_ItemRequests_MemberId_CreatedAt");
        builder.HasIndex(r => new { r.Category, r.ExpiresAt }).HasDatabaseName("IX_ItemRequests_Category_ExpiresAt");
        builder.HasIndex(r => r.ExpiresAt).HasDatabaseName("IX_ItemRequests_ExpiresAt");
    }
}

/// <summary>
/// The ItemRequestMatches table (L2-100): one row per request and listing. Listings restrict their
/// delete from Members, so cascading from both sides leaves one cascade path from a member.
/// </summary>
internal sealed class ItemRequestMatchConfiguration : IEntityTypeConfiguration<ItemRequestMatch>
{
    public void Configure(EntityTypeBuilder<ItemRequestMatch> builder)
    {
        builder.ToTable("ItemRequestMatches");
        builder.HasKey(m => new { m.ItemRequestId, m.ListingId });
        builder.HasOne<ItemRequest>().WithMany().HasForeignKey(m => m.ItemRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Listing>().WithMany().HasForeignKey(m => m.ListingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => m.ListingId).HasDatabaseName("IX_ItemRequestMatches_ListingId");
    }
}
