// Acceptance Test
// Traces to: L2-074
// Description: Verify seed creates verified demo members through the account commands, who can sign in with the documented demo password

using TutuMuch.Application.Accounts;
using TutuMuch.Cli.Tests.Support;
using TutuMuch.Domain.Notifications;

namespace TutuMuch.Cli.Tests;

public sealed class SeedMembersTests(SeededDemo demo) : IClassFixture<SeededDemo>
{
    [Fact]
    public void Seed_succeeds()
    {
        Assert.Equal(ExitCodes.Success, demo.Run.ExitCode);
        Assert.Contains($"Seeded {DemoCatalogue.Members.Count} members and {DemoCatalogue.Listings.Count} listings.", demo.Run.Output);
    }

    [Fact]
    public void Six_to_ten_verified_test_domain_members_exist_and_none_is_an_administrator()
    {
        var members = demo.Harness.InDatabase(db => db.Members.ToList());

        Assert.InRange(members.Count, 6, 10);
        Assert.All(members, m =>
        {
            Assert.True(m.IsEmailVerified);
            Assert.EndsWith(".test", m.Email, StringComparison.Ordinal);
            Assert.False(m.IsAdministrator);
        });
    }

    [Fact]
    public async Task Every_demo_member_signs_in_with_the_demo_password()
    {
        foreach (var member in DemoCatalogue.Members)
        {
            var result = await demo.Harness.SendAsync(new SignInCommand(member.Email, DemoCatalogue.Password));

            Assert.True(result.IsSuccess, $"{member.Email} could not sign in: {result.Error?.Code}");
        }
    }

    [Fact]
    public void No_demo_member_has_a_pending_email_and_the_verification_emails_are_suppressed()
    {
        var messages = demo.Harness.InDatabase(db => db.OutboxMessages.ToList());

        Assert.DoesNotContain(messages, m => m.Status is OutboxMessageStatus.Pending or OutboxMessageStatus.Processing);
        Assert.Equal(DemoCatalogue.Members.Count, messages.Count(m => m.NotificationType == NotificationType.EmailVerification && m.Status == OutboxMessageStatus.Suppressed));
    }

    [Fact]
    public void Every_verification_link_is_used()
    {
        var tokens = demo.Harness.InDatabase(db => db.EmailVerificationTokens.ToList());

        Assert.Equal(DemoCatalogue.Members.Count, tokens.Count);
        Assert.All(tokens, t => Assert.NotNull(t.UsedAt));
    }

    [Fact]
    public async Task The_cli_readme_documents_the_demo_password_for_local_use_only()
    {
        var readme = await File.ReadAllTextAsync(RepositoryFiles.PathOf("src", "backend", "TutuMuch.Cli", "README.md"));

        Assert.Contains(DemoCatalogue.Password, readme);
        Assert.Contains("local use only", readme);
        Assert.All(DemoCatalogue.Members, m => Assert.Contains(m.Email, readme));
    }
}
