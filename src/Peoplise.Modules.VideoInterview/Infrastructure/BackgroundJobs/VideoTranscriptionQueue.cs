using System.Threading.Channels;
using Peoplise.Modules.VideoInterview.Application.Common;

namespace Peoplise.Modules.VideoInterview.Infrastructure.BackgroundJobs;

/// <summary>
/// In-process, in-memory queue backed by an unbounded <see cref="Channel{T}"/> — no
/// broker, no persistence across restarts. Fine for this volume; see
/// <see cref="IVideoTranscriptionQueue"/>'s remarks for when to outgrow it.
/// </summary>
public sealed class VideoTranscriptionQueue : IVideoTranscriptionQueue
{
    private readonly Channel<VideoTranscriptionWorkItem> _channel = Channel.CreateUnbounded<VideoTranscriptionWorkItem>();

    public void Enqueue(VideoTranscriptionWorkItem item) => _channel.Writer.TryWrite(item);

    public Task<VideoTranscriptionWorkItem> DequeueAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAsync(cancellationToken).AsTask();
}
