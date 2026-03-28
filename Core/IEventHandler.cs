namespace Core;

public interface IEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    void When(TEvent @event);
}
