using MediatR;
using Microsoft.Extensions.Logging;
using Peoplise.Infrastructure.Events;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Domain.Events;

namespace Peoplise.Modules.ATS.Application.Candidates.Events;

/// <summary>
/// The trigger that makes auto-advance/auto-eliminate <c>StageRule</c>s actually fire:
/// a new evaluation is exactly the kind of input a score-based rule might now be
/// satisfied by, so re-run the current stage's rules right after one is submitted. Runs
/// synchronously within the same request as the evaluation itself (cheap — no external
/// call, just re-reading data already committed by the time this dispatches — see
/// UnitOfWork.SaveChangesAsync's ordering) rather than queuing work, the way
/// <c>VideoRecordedEventHandler</c> defers to a background service for its slower,
/// AI-backed work.
/// </summary>
public sealed class EvaluationSubmittedEventHandler : INotificationHandler<DomainEventNotification<EvaluationSubmittedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<EvaluationSubmittedEventHandler> _logger;

    public EvaluationSubmittedEventHandler(IMediator mediator, ILogger<EvaluationSubmittedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EvaluationSubmittedEvent> notification, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new TransitionCandidateStageCommand(notification.DomainEvent.ProcessId.Value), cancellationToken);

            // An expected Result.Failure here (e.g. the process already reached a
            // terminal state) isn't this handler's problem to surface — the evaluation
            // itself already saved; a stage that simply doesn't advance yet is the
            // normal "Wait" outcome, not a failure worth propagating.
            if (result.IsFailure)
            {
                _logger.LogInformation(
                    "Post-evaluation stage transition check for process {ProcessId} did not advance: {Error}",
                    notification.DomainEvent.ProcessId.Value, result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            // Logged and swallowed deliberately, same reasoning as the KVKK sweep and
            // the transcription background service: the evaluation this handler reacts
            // to has already been committed successfully by the time this runs, so an
            // unexpected failure here must not fail that already-saved action's response.
            _logger.LogError(
                ex, "Post-evaluation stage transition check failed for process {ProcessId}.",
                notification.DomainEvent.ProcessId.Value);
        }
    }
}
