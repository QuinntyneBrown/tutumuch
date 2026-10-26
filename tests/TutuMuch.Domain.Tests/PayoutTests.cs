// Unit Test
// Traces to: L2-031
// Description: The Tutu Much fee is a rate of item plus shipping, rounded to the cent and recorded on the
// order; the payout is Held until the order completes, then Releasable for an onboarded seller or
// Held for onboarding, and only a Releasable payout can be sent.

using TutuMuch.Domain.Orders;
using TutuMuch.Domain.Payouts;

namespace TutuMuch.Domain.Tests;

public sealed class PayoutTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_10_percent_fee_on_80_plus_10_shipping_is_9_and_the_payout_is_81()
    {
        var order = PaidOrder(80m, 10m);
        var payout = Payout.CreateHeld(order, Now);

        Assert.Equal(9.00m, order.FeeAmount);
        Assert.Equal(0.10m, order.FeeRate);
        Assert.Equal(90.00m, payout.GrossAmount);
        Assert.Equal(9.00m, payout.FeeAmount);
        Assert.Equal(81.00m, payout.NetAmount);
        Assert.Equal(PayoutStatus.Held, payout.Status);
        Assert.Equal(PayoutHoldReason.OrderNotCompleted, payout.HoldReason);
    }

    [Theory]
    [InlineData(30, 7, 3.70)]
    [InlineData(0.05, 0, 0.01)]
    [InlineData(12.34, 0, 1.23)]
    public void The_fee_rounds_to_the_cent_away_from_zero(decimal price, decimal shipping, decimal fee)
    {
        Assert.Equal(fee, PayoutFees.Fee(0.10m, price + shipping));
    }

    [Fact]
    public void Completion_makes_the_payout_releasable_for_an_onboarded_seller()
    {
        var payout = Payout.CreateHeld(PaidOrder(30m, 7m), Now);

        payout.OrderCompleted(sellerOnboarded: true, Now);

        Assert.Equal(PayoutStatus.Releasable, payout.Status);
        Assert.Null(payout.HoldReason);
        Assert.Equal(Now, payout.ReleasableAt);
    }

    [Fact]
    public void Completion_holds_the_payout_until_the_seller_onboards()
    {
        var payout = Payout.CreateHeld(PaidOrder(30m, 7m), Now);

        payout.OrderCompleted(sellerOnboarded: false, Now);
        Assert.Equal(PayoutHoldReason.OnboardingIncomplete, payout.HoldReason);

        payout.SellerOnboarded(Now.AddDays(1));
        Assert.Equal(PayoutStatus.Releasable, payout.Status);
    }

    [Fact]
    public void Onboarding_does_not_release_a_payout_whose_order_is_open()
    {
        var payout = Payout.CreateHeld(PaidOrder(30m, 7m), Now);

        payout.SellerOnboarded(Now);

        Assert.Equal(PayoutStatus.Held, payout.Status);
        Assert.Throws<InvalidOperationException>(() => payout.MarkSent("po_1", Now));
    }

    [Fact]
    public void A_sent_payout_is_paid_or_failed()
    {
        var paid = Payout.CreateHeld(PaidOrder(30m, 7m), Now);
        paid.OrderCompleted(true, Now);
        paid.MarkSent("po_1", Now);
        paid.MarkPaid(Now.AddDays(2));

        var failed = Payout.CreateHeld(PaidOrder(30m, 7m), Now);
        failed.OrderCompleted(true, Now);
        failed.MarkSent("po_2", Now);
        failed.MarkFailed("Account closed", Now.AddDays(2));

        Assert.Equal((PayoutStatus.Paid, Now.AddDays(2)), (paid.Status, paid.PaidAt));
        Assert.Equal((PayoutStatus.Failed, "Account closed"), (failed.Status, failed.FailureReason));
    }

    [Fact]
    public void Onboarding_is_complete_only_when_the_provider_says_so()
    {
        var account = SellerPayoutAccount.Start(Guid.NewGuid(), "acct_1", Now);
        Assert.Equal(PayoutAccountStatus.Pending, account.Status);
        Assert.Null(account.BankDisplay);

        Assert.True(account.Update(true, "Chase", "6021", Now));
        Assert.False(account.Update(true, "Chase", "6021", Now));
        Assert.Equal("Chase •••• 6021", account.BankDisplay);

        account.Update(false, null, null, Now);
        Assert.Equal(PayoutAccountStatus.Restricted, account.Status);
    }

    private static Order PaidOrder(decimal price, decimal shipping)
    {
        var checkout = Checkout.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), price, shipping, Now.AddMinutes(-5));
        checkout.SetAddress(ShippingAddress.Create("Marcus Bell", "214 Kent Ave", null, "Brooklyn", "NY", "11249"), 1m);
        var payment = Payment.Create(checkout.Id, Guid.NewGuid(), price + shipping + 1m, Now);
        return Order.CreatePaid(48302, checkout, Guid.NewGuid(), payment, 0.10m, Now);
    }
}
