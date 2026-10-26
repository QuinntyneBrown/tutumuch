namespace TutuMuch.Domain.Orders;

/// <summary>The address a member last paid with, offered first at their next checkout (L2-027 AC4).</summary>
public sealed class SavedAddress
{
    private SavedAddress()
    {
    }

    public Guid MemberId { get; private set; }

    public ShippingAddress Address { get; private set; } = null!;

    public DateTime UpdatedAt { get; private set; }

    public static SavedAddress For(Guid memberId, ShippingAddress address, DateTime now) =>
        new() { MemberId = memberId, Address = address, UpdatedAt = now };

    public void Replace(ShippingAddress address, DateTime now)
    {
        Address = address;
        UpdatedAt = now;
    }
}
