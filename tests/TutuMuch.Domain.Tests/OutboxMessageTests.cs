// Unit Test
// Traces to: L2-042
// Description: An outbox message is due at once, is claimed by one worker at a time, and retries a
// transient failure after 1, 2, 4 ... minutes up to 60, plus at most 20% jitter, until 24 hours
// after it was created; a permanent failure ends it at once.

using TutuMuch.Domain.Notifications;

namespace TutuMuch.Domain.Tests;

public sealed class OutboxMessageTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    private static OutboxMessage Claimed()
    {
        var message = OutboxMessage.Create(NotificationType.SaleMade, Guid.NewGuid(), "{}", Now);
        message.Claim(Now);
        return message;
    }

    [Fact]
    public void A_new_message_is_pending_and_due_at_once()
    {
        var message = OutboxMessage.Create(NotificationType.SaleMade, Guid.NewGuid(), "{}", Now);

        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.True(message.IsClaimable(Now));
    }

    [Fact]
    public void A_claimed_message_is_not_claimable_until_the_claim_lapses()
    {
        var message = Claimed();

        Assert.Equal(OutboxMessageStatus.Processing, message.Status);
        Assert.False(message.IsClaimable(Now.AddMinutes(1)));
        Assert.True(message.IsClaimable(Now + OutboxMessage.ClaimFor));
    }

    [Fact]
    public void Claiming_renews_the_concurrency_token()
    {
        var message = OutboxMessage.Create(NotificationType.SaleMade, Guid.NewGuid(), "{}", Now);
        var before = message.ClaimToken;

        message.Claim(Now);

        Assert.NotEqual(before, message.ClaimToken);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(6, 32)]
    [InlineData(7, 60)]
    [InlineData(9, 60)]
    public void Transient_failures_back_off_exponentially_up_to_an_hour(int attempt, int minutes)
    {
        var message = OutboxMessage.Create(NotificationType.SaleMade, Guid.NewGuid(), "{}", Now);
        var at = Now;
        for (var i = 1; i <= attempt; i++)
        {
            at = message.NextAttemptAt;
            message.Claim(at);
            Assert.False(message.RecordFailure("http_503", at, jitter: 0));
        }

        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(attempt, message.AttemptCount);
        Assert.Equal(at.AddMinutes(minutes), message.NextAttemptAt);
    }

    [Fact]
    public void Jitter_adds_at_most_20_percent()
    {
        var message = Claimed();

        message.RecordFailure("timeout", Now, jitter: 1);

        Assert.Equal(Now.AddMinutes(1.2), message.NextAttemptAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void A_failure_past_the_24_hour_window_is_final()
    {
        var message = Claimed();
        var late = Now.AddHours(23.99);
        message.RecordFailure("timeout", Now, 0);
        message.Claim(late);

        Assert.True(message.RecordFailure("timeout", late, 0));
        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
    }

    [Fact]
    public void A_permanent_failure_ends_the_message_at_once()
    {
        var message = Claimed();

        message.Fail("rejected:422");

        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
        Assert.False(message.IsClaimable(Now.AddDays(1)));
    }

    [Fact]
    public void A_sent_message_records_when()
    {
        var message = Claimed();

        message.RecordSent(Now.AddSeconds(3));

        Assert.Equal(OutboxMessageStatus.Sent, message.Status);
        Assert.Equal(Now.AddSeconds(3), message.SentAt);
        Assert.Equal(1, message.AttemptCount);
    }

    [Fact]
    public void Only_a_claimed_message_records_an_outcome()
    {
        var message = OutboxMessage.Create(NotificationType.SaleMade, Guid.NewGuid(), "{}", Now);

        Assert.Throws<InvalidOperationException>(() => message.RecordSent(Now));
    }
}
