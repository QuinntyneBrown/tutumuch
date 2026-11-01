// Acceptance Test
// Traces to: L2-117, L2-118, L2-119
// Description: The deposit is 20% of the shelf price, at least $5 and never more than the price.
// A paid grab must be accepted within 48 hours and bought within 3 days; the shopper gets the
// total back on every ending except cancelling after acceptance, which keeps the deposit.

using TuTuMuch.Domain.Finds;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Domain.Tests;

public sealed class GrabTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 15, 0, 0, DateTimeKind.Utc);
    private static readonly ShippingAddress Brooklyn = ShippingAddress.Create("Marcus Bell", "214 Kent Ave", null, "Brooklyn", "NY", "11249");

    [Theory]
    [InlineData("40.00", "8.00")]
    [InlineData("12.00", "5.00")]
    [InlineData("3.00", "3.00")]
    [InlineData("24.99", "5.00")]
    [InlineData("102.37", "20.47")]
    public void The_deposit_is_20_percent_at_least_5_dollars_and_never_more_than_the_price(string price, string deposit)
    {
        Assert.Equal(Money(deposit), GrabPricing.Deposit(Money(price), 20m, 5m));
    }

    [Fact]
    public void The_total_is_price_fee_and_tax()
    {
        var amounts = GrabPricing.Quote(40m, 8m, 4.26m, 20m, 5m);

        Assert.Equal(52.26m, amounts.Total);
        Assert.Equal(8m, amounts.Deposit);
        Assert.Equal(44.26m, amounts.RefundAfterAcceptance);
    }

    [Fact]
    public void A_paid_grab_is_requested_with_48_hours_to_accept()
    {
        var grab = Paid();

        Assert.Equal(GrabStatus.Requested, grab.Status);
        Assert.Equal(Now + TimeSpan.FromHours(48), grab.Deadline);
        Assert.True(grab.IsOpen);
        Assert.Equal(grab.Total, grab.RefundIfCancelled);
    }

    [Fact]
    public void A_declined_payment_ends_the_grab_without_a_refund()
    {
        var grab = Started();

        grab.PaymentFailed("Insufficient funds.", Now);

        Assert.Equal(GrabStatus.PaymentFailed, grab.Status);
        Assert.False(grab.IsOpen);
        Assert.Null(grab.RefundedAmount);
    }

    [Fact]
    public void Accepting_sets_3_days_to_buy_and_cancelling_then_keeps_the_deposit()
    {
        var grab = Paid();
        grab.Accept(Now.AddHours(1));

        Assert.Equal(Now.AddHours(1) + TimeSpan.FromDays(3), grab.Deadline);
        Assert.Equal(grab.Total - grab.Deposit, grab.Cancel(Now.AddHours(2)));
        Assert.Equal(GrabStatus.Cancelled, grab.Status);
    }

    [Fact]
    public void Cancelling_before_acceptance_refunds_everything()
    {
        var grab = Paid();

        Assert.Equal(grab.Total, grab.Cancel(Now.AddHours(1)));
    }

    [Fact]
    public void Declining_and_unavailable_refund_everything()
    {
        var declined = Paid();
        var unavailable = Paid();
        unavailable.Accept(Now);

        Assert.Equal(declined.Total, declined.Decline(Now));
        Assert.Equal(unavailable.Total, unavailable.MarkUnavailable(GrabUnavailableReason.PriceWentUp, Now));
        Assert.Equal("Price went up", unavailable.UnavailableReason!.Value.ToLabel());
    }

    [Fact]
    public void A_bought_grab_cannot_be_cancelled_and_is_received()
    {
        var grab = Paid();
        grab.Accept(Now);
        grab.MarkBought(Now.AddDays(1));

        Assert.Null(grab.RefundIfCancelled);
        Assert.Throws<InvalidOperationException>(() => grab.Cancel(Now.AddDays(1)));
        Assert.False(grab.IsDueToReceive(Now.AddDays(14)));
        Assert.True(grab.IsDueToReceive(Now.AddDays(15)));
        grab.ConfirmReceived(Now.AddDays(2));
        Assert.Equal(GrabStatus.Received, grab.Status);
    }

    [Fact]
    public void Missed_deadlines_expire_with_a_full_refund()
    {
        var unanswered = Paid();
        var unbought = Paid();
        unbought.Accept(Now);

        Assert.False(unanswered.CanAccept(Now.AddHours(48)));
        Assert.False(unanswered.IsOverdue(Now.AddHours(47)));
        Assert.Equal(unanswered.Total, unanswered.Expire(Now.AddHours(48)));
        Assert.True(unbought.IsOverdue(Now.AddDays(3)));
        Assert.Equal(unbought.Total, unbought.Expire(Now.AddDays(3)));
    }

    private static Grab Started() =>
        Grab.Start(Grab.FirstNumber, Guid.NewGuid(), Guid.NewGuid(), GrabPricing.Quote(40m, 8m, 4.26m, 20m, 5m), Brooklyn, Now);

    private static Grab Paid()
    {
        var grab = Started();
        grab.PaymentSucceeded(Now);
        return grab;
    }

    private static decimal Money(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
