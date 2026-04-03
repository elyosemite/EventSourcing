using System.Collections.Frozen;

namespace Core;

public interface IDomainEventDispatcher
{
    void Dispatch(IDomainEvent @event);
}

public interface IEventSourcing
{
    void Rehydrate(IEnumerable<IDomainEvent> history);
}

public interface IAggregateRoot : IEntity, IDomainEventDispatcher
{
    /// <summary>
    /// Atenção com este campo; Depois você tem que verificar se ele realmente
    /// faz sentido estar aqui ou se não precisa mudar para uma outra interface.
    /// </summary>
    int Version { get; }

    IReadOnlyList<IDomainEvent> UncommittedEvents { get; }

    void MarkEventsAsCommitted(); // clear uncommitted events
}
