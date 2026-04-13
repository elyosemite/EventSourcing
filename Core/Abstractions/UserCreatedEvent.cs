namespace Core.Abstractions;

public class UserCreatedEvent : BaseEvent
{
    
}

public class BaseHandler : IEventHandler<BaseEvent>
{
    public async Task HandleAsync(BaseEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{nameof(BaseHandler)}.{nameof(HandleAsync)}");
        await Task.CompletedTask;
    }
}