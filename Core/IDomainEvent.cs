namespace Core;

public interface IDomainEvent
{
    Guid AggregateId { get; }
    int EventVersion { get; }
    DateTime OccurredOn { get; }
}

/// <summary>
/// Modificar o identificado para um tipo de ID específico
/// </summary>
/// <param name="AggregateId"></param>
public abstract record DomainEvent(Guid AggregateId) : IDomainEvent
{
    public int EventVersion { get; set; } = 1;
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
}
