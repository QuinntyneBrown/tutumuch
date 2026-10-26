using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The OutboxMessages table: email requests written with the change that caused them (L2-042).
/// The claim token is a concurrency token, so two workers cannot claim the same message.
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.NotificationType).HasConversion<string>().HasMaxLength(40);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(m => m.Payload).IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(1000);
        builder.Property(m => m.SuppressionReason).HasMaxLength(50);
        builder.Property(m => m.ClaimToken).IsConcurrencyToken();
        builder.HasIndex(m => new { m.Status, m.NextAttemptAt })
            .HasFilter("Status IN ('Pending', 'Processing')")
            .HasDatabaseName("IX_OutboxMessages_Due");
    }
}
