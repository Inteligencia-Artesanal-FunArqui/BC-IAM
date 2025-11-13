using Cortex.Mediator;
using OsitoPolar.IAM.Service.Shared.Domain.Model.Events;
using OsitoPolar.IAM.Service.Shared.Domain.Services;

namespace OsitoPolar.IAM.Service.Shared.Infrastructure.EventBus;

/// <summary>
/// Event Bus implementation using Cortex.Mediator
/// </summary>
public class CortexEventBus : IEventBus
{
    private readonly IMediator _mediator;

    public CortexEventBus(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        await _mediator.PublishAsync(@event, cancellationToken);
    }
}
