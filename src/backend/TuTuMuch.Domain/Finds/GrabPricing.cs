namespace TuTuMuch.Domain.Finds;

/// <summary>The amounts of one grab, fixed when it is paid (L2-117).</summary>
public sealed record GrabAmounts(decimal ShelfPrice, decimal GrabFee, decimal SalesTax, decimal Total, decimal Deposit)
{
    /// <summary>What the shopper gets back when they cancel after TuTuMuch accepts: everything but the deposit.</summary>
    public decimal RefundAfterAcceptance => Total - Deposit;
}

/// <summary>
/// The grab price rule (ADR orders/0009): the total is shelf price + grab fee + tax, paid up front;
/// the deposit is a percentage of the shelf price, at least a minimum and never more than the
/// shelf price, rounded to the cent, midpoint away from zero.
/// </summary>
public static class GrabPricing
{
    public static decimal Deposit(decimal shelfPrice, decimal depositPercent, decimal minimumDeposit)
    {
        var deposit = decimal.Round(shelfPrice * depositPercent / 100m, 2, MidpointRounding.AwayFromZero);
        return Math.Min(Math.Max(deposit, minimumDeposit), shelfPrice);
    }

    public static GrabAmounts Quote(decimal shelfPrice, decimal grabFee, decimal salesTax, decimal depositPercent, decimal minimumDeposit) =>
        new(shelfPrice, grabFee, salesTax, shelfPrice + grabFee + salesTax, Deposit(shelfPrice, depositPercent, minimumDeposit));
}
