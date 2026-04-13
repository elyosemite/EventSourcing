namespace Core.Pipeline;

public sealed class MessageContext(
    object message,
    Type messageType,
    IServiceProvider services,
    CancellationToken cancellationToken)
{
    public object Message { get; } = message;
    public Type MessageType { get; } = messageType;
    public IServiceProvider Services { get; } = services;
    public CancellationToken CancellationToken { get; } = cancellationToken;
}