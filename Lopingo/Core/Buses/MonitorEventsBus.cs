using System.Threading.Channels;

namespace Lopingo.Core.Buses;

// Transitions (down/up) → Telegram worker. Every completed check → Blazor UI refresh.
public sealed class MonitorEventsBus
{
    private readonly Channel<MonitorUpdated> _channel =
        Channel.CreateUnbounded<MonitorUpdated>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true,
        });

    public ChannelWriter<MonitorUpdated> Writer => _channel.Writer;
    public ChannelReader<MonitorUpdated> Reader => _channel.Reader;

    private readonly Channel<Guid> _checkedChannel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
        });

    public ChannelWriter<Guid> CheckedWriter => _checkedChannel.Writer;
    public ChannelReader<Guid> CheckedReader => _checkedChannel.Reader;
}
