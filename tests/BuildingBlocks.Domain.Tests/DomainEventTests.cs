using Xunit;

namespace BuildingBlocks.Domain.Tests;

#pragma warning disable CS1591
public class DomainEventTests
{
    private sealed class TestDomainEvent() : DomainEvent;

    [Fact]
    public void DomainEvent_should_set_OccurredOnUtc_to_current_time()
    {
        var before = DateTime.UtcNow;
        var domainEvent = new TestDomainEvent();
        var after = DateTime.UtcNow;

        Assert.InRange(domainEvent.OccurredOnUtc, before, after);
        Assert.Equal(DateTimeKind.Utc, domainEvent.OccurredOnUtc.Kind);
    }
}
#pragma warning restore CS1591