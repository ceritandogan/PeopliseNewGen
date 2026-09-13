namespace Peoplise.Modules.VideoInterview.Application.Common;

public sealed record VideoTranscriptionWorkItem(Guid CaseId, Guid StepConversationId, string VideoUrl);

/// <summary>
/// The in-process hand-off between "a video answer was recorded" (raised as
/// <c>VideoRecordedEvent</c>, handled synchronously within the request) and "go
/// transcribe it" (done off the request thread by <c>VideoTranscriptionBackgroundService</c>).
/// No RabbitMQ/MassTransit needed for this volume — see the architecture doc's
/// scope-cut rationale. Revisit only once video processing volume genuinely saturates
/// this in-process queue.
/// </summary>
public interface IVideoTranscriptionQueue
{
    void Enqueue(VideoTranscriptionWorkItem item);

    Task<VideoTranscriptionWorkItem> DequeueAsync(CancellationToken cancellationToken);
}
