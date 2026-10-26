using MediatR;
using TutuMuch.Application.Orders;

namespace TutuMuch.Api.Tests.Support;

/// <summary>Records every OrderCompleted event, standing in for the payout handler of seller-payouts.</summary>
public sealed class RecordingOrderCompleted : INotificationHandler<OrderCompleted>
{
    private readonly List<OrderCompleted> _events = [];

    public IReadOnlyList<OrderCompleted> Events
    {
        get
        {
            lock (_events)
            {
                return [.. _events];
            }
        }
    }

    public Task Handle(OrderCompleted notification, CancellationToken cancellationToken)
    {
        lock (_events)
        {
            _events.Add(notification);
        }

        return Task.CompletedTask;
    }
}
