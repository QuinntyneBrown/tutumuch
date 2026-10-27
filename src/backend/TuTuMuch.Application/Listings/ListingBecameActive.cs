using MediatR;

namespace TuTuMuch.Application.Listings;

/// <summary>
/// A listing moved into Active by publish (L2-019) or relist after a cancelled order (L2-035 AC4),
/// published once after the save (ADR catalog/0004). Item requests are matched against it (L2-100).
/// </summary>
public sealed record ListingBecameActive(Guid ListingId) : INotification;
