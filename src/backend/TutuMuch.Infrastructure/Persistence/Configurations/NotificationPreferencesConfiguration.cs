using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutuMuch.Domain.Members;
using TutuMuch.Domain.Notifications;

namespace TutuMuch.Infrastructure.Persistence.Configurations;

/// <summary>
/// The NotificationPreferences table (L2-043): one row per member, deleted with the member. The
/// version is the concurrency token the API exposes as the ETag.
/// </summary>
internal sealed class NotificationPreferencesConfiguration : IEntityTypeConfiguration<NotificationPreferences>
{
    public void Configure(EntityTypeBuilder<NotificationPreferences> builder)
    {
        builder.ToTable("NotificationPreferences");
        builder.HasKey(p => p.MemberId);
        builder.HasOne<Member>().WithOne().HasForeignKey<NotificationPreferences>(p => p.MemberId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(p => p.LastMarketingChangeSource).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Version).IsConcurrencyToken();
    }
}
