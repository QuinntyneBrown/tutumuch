using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The ShowcaseRequests table (L2-109): one row per member and listing, which also settles two
/// sends racing each other. Deleted with the member, so erasing a member erases their requests
/// (L2-111). Listings restrict their delete from Members, so cascading from both sides leaves one
/// cascade path from a member.
/// </summary>
internal sealed class ShowcaseRequestConfiguration : IEntityTypeConfiguration<ShowcaseRequest>
{
    public void Configure(EntityTypeBuilder<ShowcaseRequest> builder)
    {
        builder.ToTable("ShowcaseRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Note).HasMaxLength(ShowcaseRequest.MaxNote);
        builder.HasOne<Member>().WithMany().HasForeignKey(r => r.MemberId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Listing>().WithMany().HasForeignKey(r => r.ListingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => new { r.MemberId, r.ListingId }).IsUnique().HasDatabaseName("IX_ShowcaseRequests_MemberId_ListingId");
        builder.HasIndex(r => new { r.MemberId, r.CreatedAt }).HasDatabaseName("IX_ShowcaseRequests_MemberId_CreatedAt");
        builder.HasIndex(r => r.UpdatedAt).HasDatabaseName("IX_ShowcaseRequests_UpdatedAt");
    }
}
