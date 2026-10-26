using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutuMuch.Domain.Listings;
using TutuMuch.Domain.Members;

namespace TutuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The ListingPhotos table and its owned ListingPhotoVersions (L2-017). A deleted listing leaves
/// its photos unattached. Positions are not unique in the database, because a reorder saves
/// several rows at once; ListingPhotoAttacher keeps them 0 to n - 1.
/// </summary>
internal sealed class ListingPhotoConfiguration : IEntityTypeConfiguration<ListingPhoto>
{
    public void Configure(EntityTypeBuilder<ListingPhoto> builder)
    {
        builder.ToTable("ListingPhotos", t =>
            t.HasCheckConstraint("CK_ListingPhotos_Position", "Position IS NULL OR Position BETWEEN 0 AND 7"));
        builder.HasKey(p => p.Id);
        builder.Property(p => p.OriginalKey).HasMaxLength(200).IsRequired();
        builder.Property(p => p.OriginalContentType).HasMaxLength(32).IsRequired();
        builder.Property(p => p.OriginalHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasOne<Member>().WithMany().HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Listing>().WithMany().HasForeignKey(p => p.ListingId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(p => new { p.ListingId, p.Position }).HasDatabaseName("IX_ListingPhotos_ListingId_Position");
        builder.HasIndex(p => new { p.OwnerId, p.CreatedAt }).HasDatabaseName("IX_ListingPhotos_OwnerId_CreatedAt");
        builder.OwnsMany(p => p.Versions, versions =>
        {
            versions.ToTable("ListingPhotoVersions");
            versions.WithOwner().HasForeignKey("PhotoId");
            versions.HasKey("PhotoId", nameof(ListingPhotoVersion.Size), nameof(ListingPhotoVersion.Format));
            versions.Property(v => v.Format).HasConversion<string>().HasMaxLength(8);
            versions.Property(v => v.Key).HasMaxLength(80).IsRequired();
        });
        builder.Navigation(p => p.Versions).HasField("_versions");
    }
}
