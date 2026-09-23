using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Workflows.Commands;

public sealed record RemoveWorkflowStageCommand(Guid WorkflowDefinitionId, Guid StageId) : IRequest<Result>;

public sealed class RemoveWorkflowStageCommandValidator : AbstractValidator<RemoveWorkflowStageCommand>
{
    public RemoveWorkflowStageCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId).NotEmpty();
        RuleFor(x => x.StageId).NotEmpty();
    }
}

public sealed class RemoveWorkflowStageCommandHandler : IRequestHandler<RemoveWorkflowStageCommand, Result>
{
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveWorkflowStageCommandHandler(
        IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows, AppDbContext context, IUnitOfWork unitOfWork)
    {
        _workflows = workflows;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveWorkflowStageCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.GetByIdAsync(WorkflowDefinitionId.From(request.WorkflowDefinitionId), cancellationToken);
        if (workflow is null)
        {
            return Result.Failure(Error.NotFound(
                "WorkflowDefinition.NotFound", $"No workflow definition '{request.WorkflowDefinitionId}' was found."));
        }

        // Cross-aggregate check WorkflowDefinition.RemoveStage can't make itself: removing
        // a stage a candidate is currently sitting on would silently orphan their pipeline
        // position (CandidateProcess.CurrentStageId would point at a stage that no longer
        // exists) — checked here, same reasoning as SubmitReviewerScoringCommandHandler's
        // stepId/competencyId validation.
        var candidateOnStage = await _context.Set<CandidateProcess>()
            .AnyAsync(p => p.CurrentStageId == request.StageId, cancellationToken);

        if (candidateOnStage)
        {
            return Result.Failure(Error.Conflict(
                "WorkflowDefinition.StageInUse",
                "One or more candidates are currently on this stage. Move them to a different stage before removing it."));
        }

        var result = workflow.RemoveStage(request.StageId);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
