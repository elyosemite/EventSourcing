namespace Core.Pipeline;

public interface IMessageMiddleware
{
    Task InvokeAsync(MessageContext context, Func<Task> next);
}