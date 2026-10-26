using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutuMuch.Domain.Members;
using TutuMuch.Domain.Orders;

namespace TutuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The OrderProblems table (L2-036). One problem per order and one per reference; Status is a
/// concurrency token, so a seller reply and a resolution racing on the same problem cannot both
/// commit. The filtered index serves the completion job's open-problem check.
/// </summary>
internal sealed class OrderProblemConfiguration : IEntityTypeConfiguration<OrderProblem>
{
    public void Configure(EntityTypeBuilder<OrderProblem> builder)
    {
        builder.ToTable("OrderProblems");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Reference).HasMaxLength(16).IsRequired();
        builder.HasIndex(p => p.Reference).IsUnique().HasDatabaseName("UX_OrderProblems_Reference");
        builder.HasIndex(p => p.Number).IsUnique().HasDatabaseName("UX_OrderProblems_Number");
        builder.HasIndex(p => p.OrderId).IsUnique().HasDatabaseName("UX_OrderProblems_OrderId");
        builder.Property(p => p.Reason).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Description).HasMaxLength(OrderProblem.MaxText).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16).IsConcurrencyToken();
        builder.Property(p => p.SellerResponse).HasMaxLength(OrderProblem.MaxText);
        builder.Property(p => p.Outcome).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.RefundAmount).HasPrecision(10, 2);
        builder.Property(p => p.ResolutionReason).HasMaxLength(OrderProblem.MaxText);
        builder.HasIndex(p => new { p.Status, p.OpenedAt })
            .HasFilter("Status = 'Open'")
            .HasDatabaseName("IX_OrderProblems_Open_OpenedAt");
        builder.HasOne<Order>().WithMany().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Member>().WithMany().HasForeignKey(p => p.ResolvedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Photos).WithOne().HasForeignKey(p => p.ProblemId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Photos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(p => p.CanSellerRespond);
    }
}

/// <summary>The ProblemPhotos table: where each private evidence photo is stored, in upload order.</summary>
internal sealed class ProblemPhotoConfiguration : IEntityTypeConfiguration<ProblemPhoto>
{
    public void Configure(EntityTypeBuilder<ProblemPhoto> builder)
    {
        builder.ToTable("ProblemPhotos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.StorageKey).HasMaxLength(128).IsRequired();
        builder.Property(p => p.ContentType).HasMaxLength(32).IsRequired();
    }
}
