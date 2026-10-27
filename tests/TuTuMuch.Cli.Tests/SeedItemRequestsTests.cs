// Acceptance Test
// Traces to: L2-098, L2-100, L2-102
// Description: Verify seed adds open item requests for a demo shopper through the same commands as
// the API, that a demo listing published after them matches one, and that the match email is
// suppressed so a local install sends nothing.

using TuTuMuch.Cli.Tests.Support;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Cli.Tests;

public sealed class SeedItemRequestsTests(SeededDemo demo) : IClassFixture<SeededDemo>
{
    [Fact]
    public void The_demo_shopper_has_open_item_requests_and_one_is_matched_by_a_demo_listing()
    {
        var shopper = DemoCatalogue.Members.Single(m => m.DisplayName == DemoCatalogue.RequestingMember);
        var (requests, matched, emails) = demo.Harness.InDatabase(db =>
        {
            var memberId = db.Members.Single(m => m.Email == shopper.Email).Id;
            var requests = db.ItemRequests.Where(r => r.MemberId == memberId).ToList();
            var ids = requests.Select(r => r.Id).ToList();
            var matched = db.ItemRequestMatches.Where(m => ids.Contains(m.ItemRequestId)).Select(m => m.ItemRequestId).Distinct().Count();
            var emails = db.OutboxMessages.Where(m => m.NotificationType == NotificationType.ItemRequestMatched).Select(m => m.Status).ToList();
            return (requests, matched, emails);
        });

        Assert.Equal(ExitCodes.Success, demo.Run.ExitCode);
        Assert.Equal(DemoCatalogue.ItemRequests.Count, requests.Count);
        Assert.All(requests, r => Assert.True(r.ExpiresAt > DateTime.UtcNow));
        Assert.Equal(1, matched);
        Assert.NotEmpty(emails);
        Assert.All(emails, s => Assert.Equal(OutboxMessageStatus.Suppressed, s));
        Assert.Contains($"Seeded {DemoCatalogue.ItemRequests.Count} item requests.", demo.Run.Output);
    }
}
