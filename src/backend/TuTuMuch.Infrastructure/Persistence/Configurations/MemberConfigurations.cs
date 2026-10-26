using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.DisplayName).HasMaxLength(30).IsRequired();
        builder.Property(m => m.NormalizedDisplayName).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(254).IsRequired();
        builder.Property(m => m.NormalizedEmail).HasMaxLength(254).IsRequired();
        builder.Property(m => m.PasswordHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(m => m.NormalizedDisplayName).IsUnique().HasDatabaseName("UX_Members_NormalizedDisplayName");
        builder.HasIndex(m => m.NormalizedEmail).IsUnique().HasDatabaseName("UX_Members_NormalizedEmail");
        builder.Ignore(m => m.IsEmailVerified);
        builder.Ignore(m => m.IsClosed);
    }
}

internal sealed class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("UX_EmailVerificationTokens_TokenHash");
        builder.HasIndex(t => new { t.MemberId, t.CreatedAt }).HasDatabaseName("IX_EmailVerificationTokens_MemberId_CreatedAt");
        builder.HasOne<Member>().WithMany().HasForeignKey(t => t.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(s => s.TokenHash).IsUnique().HasDatabaseName("UX_Sessions_TokenHash");
        builder.HasIndex(s => s.FamilyId).HasDatabaseName("IX_Sessions_FamilyId");
        builder.HasOne<Member>().WithMany().HasForeignKey(s => s.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}
