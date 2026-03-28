namespace Core;

public interface IAggregateRoot : IEntity
{
    /// <summary>
    /// Atenção com este campo; Depois você tem que verificar se ele realmente
    /// faz sentido estar aqui ou se não precisa mudar para uma outra interface.
    /// </summary>
    int Version { get; }

    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void Rehydrate(IEnumerable<IDomainEvent> history);
}