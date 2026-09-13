using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Peoplise.Modules.VideoInterview.Application.Common;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.AI;
using Peoplise.SharedKernel.Persistence;

namespace Peoplise.Modules.VideoInterview.Infrastructure.BackgroundJobs;

/// <summary>
/// Dequeues transcription work items and processes them off the request thread — the
/// "in-process hosted service, RabbitMQ/MassTransit gerekmez" decision from the
/// architecture doc. Creates its own DI scope per item since a singleton
/// <see cref="BackgroundService"/> outlives any request scope.
/// </summary>
/// <remarks>
/// With the default <see cref="IAIProvider"/> registration
/// (<c>NotConfiguredAIProvider</c>), every dequeued item will fail at the
/// <see cref="IAIProvider.TranscribeAsync"/> call — that's expected until a real AI
/// provider is wired in. The failure is caught and logged per item so one bad item
/// doesn't stop the loop from processing the rest of the queue.
/// </remarks>
public sealed class VideoTranscriptionBackgroundService : BackgroundService
{
    private readonly IVideoTranscriptionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoTranscriptionBackgroundService> _logger;

    public VideoTranscriptionBackgroundService(
        IVideoTranscriptionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<VideoTranscriptionBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            VideoTranscriptionWorkItem item;
            try
            {
                item = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessAsync(item, stoppingToken);
        }
    }

    private async Task ProcessAsync(VideoTranscriptionWorkItem item, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var aiProvider = scope.ServiceProvider.GetRequiredService<IAIProvider>();
            var transcript = await aiProvider.TranscribeAsync(item.VideoUrl, cancellationToken);

            var cases = scope.ServiceProvider.GetRequiredService<IRepository<Case, CaseId>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var @case = await cases.GetByIdAsync(CaseId.From(item.CaseId), cancellationToken);
            if (@case is null)
            {
                _logger.LogWarning("Case {CaseId} no longer exists; dropping transcription result.", item.CaseId);
                return;
            }

            var result = @case.CompleteTranscription(item.StepConversationId, transcript);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Could not apply transcription for case {CaseId}, step conversation {StepConversationId}: {Error}",
                    item.CaseId, item.StepConversationId, result.Error.Message);
                return;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Logged and swallowed deliberately: one failed transcription (e.g. no AI
            // provider configured yet) must not crash the loop and stop every future
            // item in the queue from being processed.
            _logger.LogError(
                ex, "Failed to transcribe video for case {CaseId}, step conversation {StepConversationId}.",
                item.CaseId, item.StepConversationId);
        }
    }
}
