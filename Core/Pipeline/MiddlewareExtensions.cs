using Microsoft.Extensions.DependencyInjection;

namespace Core.Pipeline;

public static class MiddlewareExtensions
{
    public static Func<MessageDelegate, MessageDelegate> UseMiddleware<T>()
        where T : IMessageMiddleware
    {
        return next => async context =>
        {
            var middleware = context.Services.GetRequiredService<T>();
            
            await middleware.InvokeAsync(context, () => next(context));
        };
    }
}
