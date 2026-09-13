using MediatR;
using Peoplise.Infrastructure.Events;
using Peoplise.Modules.VideoInterview.Domain.Events;

namespace Peoplise.Modules.VideoInterview.Application.Common;

/// <summary>
/// "VideoRecordedEvent tetiklendiğinde IAIProvider.TranscribeAsync çağrılır" — this is
/// the trigger. Runs synchronously within the request (fast: just an enqueue), the
/// actual AI call happens later on <c>VideoTranscriptionBackgroundService</c>'s own
/// thread.
/// </summary>
public sealed class VideoRecordedEventHandler : INotificationHandler<DomainEventNotification<VideoRecordedEvent>>
{
    private readonly IVideoTranscriptionQueue _queue;

    public VideoRecordedEventHandler(IVideoTranscriptionQueue queue)
    {
        _queue = queue;
    }

    public Task Handle(DomainEventNotification<VideoRecordedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _queue.Enqueue(new VideoTranscriptionWorkItem(domainEvent.CaseId.Value, domainEvent.StepConversationId, domainEvent.VideoUrl));
        return Task.CompletedTask;
    }
}
