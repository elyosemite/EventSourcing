using System.Collections.Concurrent;

namespace Core.Pipeline;

public interface IMessageDispatcher
{
    Task Dispatch(MessageContext context);
}


public class MessageDispatcher(IServiceProvider provider) : IMessageDispatcher
{
    private readonly ConcurrentDictionary<Type, Func<MessageContext, Task>> _cache = new();
    
    public Task Dispatch(MessageContext context)
    {
        var handler = _cache.GetOrAdd(
            context.MessageType, BuilderHandler);

        return handler(context);
    }

    private Func<MessageContext, Task> BuilderHandler(Type messageType)
    {
        if (typeof(ICommand).IsAssignableFrom(messageType))
            return BuildCommandHandler(messageType);
        
        if (typeof(IEvent).IsAssignableFrom(messageType))
            return BuildEventHandler(messageType);
        
        if (TryGetRequestResponse(messageType, out var responseType))
            return BuildRequestHandler(messageType, responseType);
        
        throw new InvalidOperationException($"Unknown message type: {messageType}");
    }
    
    private Func<MessageContext, Task> BuildCommandHandler(Type commandType)
    {
        return async context =>
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);

            var handler = context.Services.GetService(handlerType)
                ?? throw new InvalidOperationException("No command handler found");

            var method = handlerType.GetMethod("HandleAsync")!;

            await (Task)method.Invoke(handler, new[]
            {
                context.Message,
                context.CancellationToken
            })!;
        };
    }

    private Func<MessageContext, Task> BuildEventHandler(Type eventType)
    {
        return async context =>
        {
            var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);

            var handlers = context.Services.GetServices(handlerType);

            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod("HandleAsync")!;

                tasks.Add((Task)method.Invoke(handler, new[]
                {
                    context.Message,
                    context.CancellationToken
                })!);
            }

            await Task.WhenAll(tasks);
        };
    }

    private Func<MessageContext, Task> BuildRequestHandler(
        Type requestType,
        Type responseType)
    {
        return async context =>
        {
            var handlerType = typeof(IRequestHandler<,>)
                .MakeGenericType(requestType, responseType);

            var handler = context.Services.GetService(handlerType)
                ?? throw new InvalidOperationException("No request handler found");

            var method = handlerType.GetMethod("HandleAsync")!;

            var result = method.Invoke(handler, new[]
            {
                context.Message,
                context.CancellationToken
            });

            context.SetResponse(result!);

            await (Task)result!;
        };
    }

    private bool TryGetRequestResponse(Type requestType, out Type responseType)
    {
        var interfaceType = requestType
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRequest<>));

        if (interfaceType is null)
        {
            responseType = null!;
            return false;
        }

        responseType = interfaceType.GetGenericArguments()[0];
        return true;
    }
}
