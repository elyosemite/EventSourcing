using Core;

namespace Tests.Aggregates;

public partial class Student : AggregateRoot
{
    public string Name { get; private set; }

    public Student(Guid id, string name)
    {
        Id = id;
        Name = name;

        Apply(new StudentCreatedEvent(id, name));
    }
}

public record StudentCreatedEvent(
    Guid AggregateId, string Name
) : DomainEvent(AggregateId);

// EventHandler for StudentCreatedEvent
public partial class Student : IEventHandler<StudentCreatedEvent>
{
    public Student()
    {
        Register<StudentCreatedEvent>(this);
    }

    public void When(StudentCreatedEvent @event)
    {
        Id = @event.AggregateId;
        Name = @event.Name;
    }
}

[TestFixture]
public class EntityTests
{
    [Test]
    public void Create_Entity_And_Return_Properties()
    {
        var id = Guid.NewGuid();
        var name = "John Doe";
        var student = new Student(id, name); // It triggers the StudentCreatedEvent and applies it to set the properties

        Assert.That(student.Id, Is.EqualTo(id));
        Assert.That(student.Name, Is.EqualTo(name));
        Assert.That(student.Version, Is.EqualTo(1));
        Assert.That(student.DomainEvents, Has.Count.EqualTo(1));
    }
}
