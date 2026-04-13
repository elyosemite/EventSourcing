using Core.Abstractions;

namespace EventSourcing.WebApi.Features.Command;

public record CreateOrderCommand(Guid OrderId) : ICommand;

public class CreateOrderHandler
    : ICommandHandler<CreateOrderCommand>
{
    public async Task HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"{nameof(CreateOrderHandler)}.{nameof(HandleAsync)}");
        await Task.CompletedTask;
    }
}