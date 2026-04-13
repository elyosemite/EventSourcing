namespace Core.Pipeline;

public sealed class PipelineBuilder
{
    private readonly IList<Func<MessageDelegate, MessageDelegate>> _components
        = new List<Func<MessageDelegate, MessageDelegate>>();
    
    public PipelineBuilder Use(
        Func<MessageDelegate, MessageDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    public MessageDelegate Build(MessageDelegate terminal)
    {
        var pipeline = terminal;

        for (int i = _components.Count - 1; i >= 0; i--)
        {
            pipeline = _components[i](pipeline);
        }

        return pipeline;
    }
}