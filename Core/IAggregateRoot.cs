namespace Core;

public interface IEventSourcing
{
    void Rehydrate(IEnumerable<IDomainEvent> history);
}

public interface IAggregateRoot : IEntity, IExpectedVersion
{
    /// <summary>
    /// Atenção com este campo; Depois você tem que verificar se ele realmente
    /// faz sentido estar aqui ou se não precisa mudar para uma outra interface.
    /// </summary>
    int Version { get; }

    IReadOnlyList<IDomainEvent> UncommittedEvents { get; }

    void MarkEventsAsCommitted();
}
