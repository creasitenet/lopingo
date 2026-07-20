using Lopingo.Core.Buses;

namespace Lopingo.Tests;

public sealed class MonitorEventsBusTests
{
    [Fact]
    public void PublishChecked_reaches_all_subscribers()
    {
        var bus = new MonitorEventsBus();
        using var a = bus.SubscribeChecked();
        using var b = bus.SubscribeChecked();
        var id = Guid.NewGuid();

        bus.PublishChecked(id);

        Assert.True(a.Reader.TryRead(out var fromA));
        Assert.True(b.Reader.TryRead(out var fromB));
        Assert.Equal(id, fromA);
        Assert.Equal(id, fromB);
    }

    [Fact]
    public void Disposed_subscriber_no_longer_receives()
    {
        var bus = new MonitorEventsBus();
        var a = bus.SubscribeChecked();
        using var b = bus.SubscribeChecked();
        a.Dispose();

        bus.PublishChecked(Guid.NewGuid());

        Assert.False(a.Reader.TryRead(out _));
        Assert.True(b.Reader.TryRead(out _));
    }
}
