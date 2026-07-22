using System.Threading.Channels;
using Foveo.Application.Contracts;

namespace Foveo.Infrastructure.Processing;

public sealed class ChannelMediaProcessingQueue : IMediaProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(capacity: 1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });

    public ValueTask EnqueueAsync(Guid mediaId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(mediaId, ct);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}
