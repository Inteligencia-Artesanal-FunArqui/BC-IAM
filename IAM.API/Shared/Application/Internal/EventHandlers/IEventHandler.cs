using Cortex.Mediator.Notifications;
using OsitoPolar.IAM.Service.Shared.Domain.Model.Events;

namespace OsitoPolar.IAM.Service.Shared.Application.Internal.EventHandlers;

public interface IEventHandler<in TEvent> : INotificationHandler<TEvent> where TEvent : IEvent
{
    
}