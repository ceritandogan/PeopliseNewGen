using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Workflows.Commands;

public sealed record ReorderWorkflowStagesCommand(
    Guid WorkflowDefinitionId,
    IReadOnlyList<Guid> OrderedStageIds) : IRequest<Result>;

public sealed class ReorderWorkflowStagesCommandValidator : AbstractValidator<ReorderWorkflowStagesCommand>
{
    public ReorderWorkflowStagesCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId).NotEmpty();
        RuleFor(x => x.OrderedStageIds).NotEmpty();
    }
}

public sealed class ReorderWorkflowStagesCommandHandler : IRequestHandler<ReorderWorkflowStagesCommand, Result>
{
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderWorkflowStagesCommandHandler(
        IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows, IUnitOfWork unitOfWork)
    {
        _workflows = workflows;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReorderWorkflowStagesCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.GetByIdAsync(WorkflowDefinitionId.From(request.WorkflowDefinitionId), cancellationToken);
        if (workflow is null)
        {
            return Result.Failure(Error.NotFound(
                "WorkflowDefinition.NotFound", $"No workflow definition '{request.WorkflowDefinitionId}' was found."));
        }

        var result = workflow.ReorderStages(request.OrderedStageIds);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
