using Core;

namespace Tests.EventBusTests;

[TestFixture]
public class EventBusInMemoryTests
{
    [Test]
    public async Task Foo()
    {
        // Arrange
        var id = Guid.NewGuid();
        var eventFoo = new EventFoo(id, "Yuri Melo");
        var listener = new FooEventListener();
        
        // Act
        EventBus.SubscribeAsync(listener);
        await EventBus.PublishAsync(eventFoo);
        
        // Assert
        Assert.That(EventBus.Subscriptions.Count, Is.EqualTo(1));
        Assert.That(EventBus.Subscriptions.Single().Key, Is.EqualTo(typeof(EventFoo)));
    }

    public record EventFoo(Guid FooId, string name) : DomainEvent(FooId);
    
    public readonly EventBusInMemory EventBus = new();

    public class FooEventListener : IEventListener<EventFoo>
    {
        public Task HandleAsync(
            EventFoo @event,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Agora está okay, man!!!! Acabou de processar o evento {@event}");
            return Task.CompletedTask;
        }
    }
}