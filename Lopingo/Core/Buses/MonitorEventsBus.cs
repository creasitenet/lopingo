using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Lopingo.Core.Buses;

// Transitions (down/up) → Telegram worker (single consumer).
// Every completed check → fan-out to all Blazor UI subscribers.
public sealed class MonitorEventsBus
{
    private readonly Channel<MonitorUpdated> _channel =
        Channel.CreateUnbounded<MonitorUpdated>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    public ChannelWriter<MonitorUpdated> Writer => _channel.Writer;
    public ChannelReader<MonitorUpdated> Reader => _channel.Reader;

    private readonly ConcurrentDictionary<Guid, Channel<Guid>> _checkedSubs = new();

    public CheckedSubscription SubscribeChecked()
    {
        var id = Guid.NewGuid();
        var ch = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        _checkedSubs[id] = ch;
        return new CheckedSubscription(ch.Reader, () =>
        {
            if (_checkedSubs.TryRemove(id, out var removed))
                removed.Writer.TryComplete();
        });
    }

    public void PublishChecked(Guid monitorId)
    {
        foreach (var ch in _checkedSubs.Values)
            ch.Writer.TryWrite(monitorId);
    }
}

public sealed class CheckedSubscription : IDisposable
{
    private readonly Action _dispose;
    private int _disposed;

    public CheckedSubscription(ChannelReader<Guid> reader, Action dispose)
    {
        Reader = reader;
        _dispose = dispose;
    }

    public ChannelReader<Guid> Reader { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _dispose();
    }
}
