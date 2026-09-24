using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Workflows.Commands;

public sealed record RemoveStageRuleCommand(Guid WorkflowDefinitionId, Guid StageId, Guid RuleId) : IRequest<Result>;

public sealed class RemoveStageRuleCommandValidator : AbstractValidator<RemoveStageRuleCommand>
{
    public RemoveStageRuleCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId).NotEmpty();
        RuleFor(x => x.StageId).NotEmpty();
        RuleFor(x => x.RuleId).NotEmpty();
    }
}

public sealed class RemoveStageRuleCommandHandler : IRequestHandler<RemoveStageRuleCommand, Result>
{
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveStageRuleCommandHandler(IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows, IUnitOfWork unitOfWork)
    {
        _workflows = workflows;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveStageRuleCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.GetByIdAsync(WorkflowDefinitionId.From(request.WorkflowDefinitionId), cancellationToken);
        if (workflow is null)
        {
            return Result.Failure(Error.NotFound(
                "WorkflowDefinition.NotFound", $"No workflow definition '{request.WorkflowDefinitionId}' was found."));
        }

        var stage = workflow.Stages.FirstOrDefault(s => s.Id == request.StageId);
        if (stage is null)
            return Result.Failure(Error.NotFound("WorkflowDefinition.StageNotFound", $"No stage '{request.StageId}' was found in this workflow."));

        var result = stage.RemoveRule(request.RuleId);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
