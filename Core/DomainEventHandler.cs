using System.Collections.Frozen;

namespace Core.Aggregates;

public static class DomainEventHandler
{
    public static Builder<TAggregate> CreateFor<TAggregate>() => new();

    public sealed class Builder<TAggregate>
    {
        private readonly Dictionary<Type, Action<TAggregate, IDomainEvent>> _map = new();

        public Builder<TAggregate> On<TEvent>(Action<TAggregate, TEvent> apply)
            where TEvent : IDomainEvent
        {
            ArgumentNullException.ThrowIfNull(apply);

            // TODO: Talvez criar um envelop para o evento e dentro deste envelop você pode usar alguns metadados
            // e trazer o nome do evento, assim você não precisa fazer cast do tipo;
            var key = typeof(TEvent);
            if (_map.ContainsKey(key))
                throw new InvalidOperationException(
                    $"Duplicate handler registration for event '{key.Name}' in aggregate '{typeof(TAggregate).Name}'.");
            
            _map[key] = (TAggregate aggregate, IDomainEvent @event) => apply(aggregate, (TEvent)@event);

            return this;
        }

        public FrozenDictionary<Type, Action<TAggregate, IDomainEvent>> BuildFrozen()
            => _map.ToFrozenDictionary();
    }
}