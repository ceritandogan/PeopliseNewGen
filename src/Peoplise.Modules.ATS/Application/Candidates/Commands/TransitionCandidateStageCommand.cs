using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Services;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Commands;

/// <summary>
/// Evaluates the candidate's current stage against its <c>StageRule</c>s (via
/// <see cref="WorkflowEngine"/>) and applies whatever it decides: complete + advance,
/// eliminate, or leave the process where it is. Meant to be called after something that
/// could satisfy a rule — a new evaluation being submitted, or a scheduled job checking
/// delay-based rules — not on every request.
/// </summary>
public sealed record TransitionCandidateStageCommand(Guid CandidateProcessId) : IRequest<Result>;

public sealed class TransitionCandidateStageCommandValidator : AbstractValidator<TransitionCandidateStageCommand>
{
    public TransitionCandidateStageCommandValidator()
    {
        RuleFor(x => x.CandidateProcessId).NotEmpty();
    }
}

public sealed class TransitionCandidateStageCommandHandler : IRequestHandler<TransitionCandidateStageCommand, Result>
{
    private readonly IRepository<CandidateProcess, CandidateProcessId> _processes;
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;
    private readonly IUnitOfWork _unitOfWork;

    public TransitionCandidateStageCommandHandler(
        IRepository<CandidateProcess, CandidateProcessId> processes,
        IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows,
        IUnitOfWork unitOfWork)
    {
        _processes = processes;
        _workflows = workflows;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(TransitionCandidateStageCommand request, CancellationToken cancellationToken)
    {
        var process = await _processes.GetByIdAsync(CandidateProcessId.From(request.CandidateProcessId), cancellationToken);
        if (process is null)
            return Result.Failure(Error.NotFound("CandidateProcess.NotFound", $"No candidate process '{request.CandidateProcessId}' was found."));

        if (process.IsInTerminalState)
            return Result.Failure(Error.Conflict("CandidateProcess.AlreadyClosed", "This process has already reached a terminal state."));

        if (process.CurrentStageId is null)
            return Result.Failure(Error.Conflict("CandidateProcess.NoCurrentStage", "This process has no current stage."));

        var workflow = await _workflows.GetByIdAsync(process.WorkflowDefinitionId, cancellationToken);
        if (workflow is null)
            return Result.Failure(Error.NotFound("WorkflowDefinition.NotFound", "The process's workflow definition could not be found."));

        var orderedStages = workflow.Stages.OrderBy(s => s.Order).ToList();
        var currentStage = orderedStages.FirstOrDefault(s => s.Id == process.CurrentStageId.Value);
        if (currentStage is null)
            return Result.Failure(Error.Failure("CandidateProcess.StageNotInWorkflow", "The process's current stage is not part of its workflow definition."));

        var now = DateTimeOffset.UtcNow;
        var consensusResult = process.CalculateConsensusScore(currentStage.Id);
        var consensusScore = consensusResult.IsSuccess ? consensusResult.Value.Value : (decimal?)null;
        var daysInStage = (int)(now - process.CurrentStageEnteredAt).TotalDays;

        var decision = WorkflowEngine.Evaluate(currentStage, consensusScore, daysInStage);

        var outcome = decision switch
        {
            WorkflowDecision.Eliminate => process.Eliminate($"Auto-eliminated by workflow rule at stage '{currentStage.Name}'."),
            WorkflowDecision.Advance => AdvanceToNextStage(process, orderedStages, currentStage, now),
            _ => Result.Success(),
        };

        if (outcome.IsFailure)
            return outcome;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static Result AdvanceToNextStage(
        CandidateProcess process,
        IReadOnlyList<Domain.Entities.Stage> orderedStages,
        Domain.Entities.Stage currentStage,
        DateTimeOffset now)
    {
        var completeResult = process.CompleteCurrentStage(now);
        if (completeResult.IsFailure)
            return completeResult;

        var nextStage = orderedStages.FirstOrDefault(s => s.Order > currentStage.Order);
        if (nextStage is null)
        {
            // No further stage defined — the workflow has run its course; the offer/
            // acceptance decision from here is an explicit human action, not automatic.
            return Result.Success();
        }

        return process.MoveToNextStage(nextStage.Id, now);
    }
}
