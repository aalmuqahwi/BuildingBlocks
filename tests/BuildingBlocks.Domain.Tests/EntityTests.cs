using Xunit;

namespace BuildingBlocks.Domain.Tests;

#pragma warning disable CS1591

public class EntityTests
{
    private class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
    }

    private class AnotherTestEntity : Entity<Guid>
    {
        public AnotherTestEntity(Guid id) : base(id) { }
    }

    private class TestDomainEvent() : DomainEvent;

    [Fact]
    public void Two_entities_with_same_id_are_equals()
    {
        var id = Guid.NewGuid();
        var left = new TestEntity(id);
        var right = new TestEntity(id);

        var result = left == right;

        Assert.True(result);
    }

    [Fact]
    public void Two_entities_with_different_id_are_not_equals()
    {
        var left = new TestEntity(Guid.NewGuid());
        var right = new TestEntity(Guid.NewGuid());

        var result = left != right;

        Assert.True(result);
    }

    [Fact]
    public void Entity_should_not_equal_null()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.False(entity.Equals(null));
    }

    [Fact]
    public void Entity_should_not_equal_unrelated_object()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.False(entity.Equals("not an entity"));
    }

    [Fact]
    public void Entity_should_equal_itself()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.True(entity.Equals(entity));
    }

    [Fact]
    public void Entities_with_same_id_but_different_type_should_not_be_equal()
    {
        var id = Guid.NewGuid();
        var left = new TestEntity(id);
        var right = new AnotherTestEntity(id);

        Assert.False(left.Equals(right));
    }

    public class HashCode
    {
        [Fact]
        public void Two_entities_with_same_id_have_the_same_hash_code()
        {
            var id = Guid.NewGuid();
            var left = new TestEntity(id);
            var right = new TestEntity(id);

            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }
    }

    [Fact]
    public void RaiseDomainEvent_should_add_event_to_list()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var domainEvent = new TestDomainEvent();

        entity.RaiseDomainEvent(domainEvent);
        var events = entity.PopDomainEvents();

        Assert.Single(events);
        Assert.Same(domainEvent, events[0]);
    }

    [Fact]
    public void PopDomainEvents_should_clear_events_after_popping()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.RaiseDomainEvent(new TestDomainEvent());

        entity.PopDomainEvents();          // first pop
        var secondPop = entity.PopDomainEvents(); // should be empty now

        Assert.Empty(secondPop);
    }

}
#pragma warning restore CS1591