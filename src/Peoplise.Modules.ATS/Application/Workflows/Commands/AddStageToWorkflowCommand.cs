using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Workflows.Commands;

public sealed record AddStageToWorkflowCommand(
    Guid WorkflowDefinitionId,
    string Name,
    StageType Type,
    int Order) : IRequest<Result<Guid>>;

public sealed class AddStageToWorkflowCommandValidator : AbstractValidator<AddStageToWorkflowCommand>
{
    public AddStageToWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddStageToWorkflowCommandHandler : IRequestHandler<AddStageToWorkflowCommand, Result<Guid>>
{
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;
    private readonly IUnitOfWork _unitOfWork;

    public AddStageToWorkflowCommandHandler(
        IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows,
        IUnitOfWork unitOfWork)
    {
        _workflows = workflows;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddStageToWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.GetByIdAsync(WorkflowDefinitionId.From(request.WorkflowDefinitionId), cancellationToken);
        if (workflow is null)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "WorkflowDefinition.NotFound", $"No workflow definition '{request.WorkflowDefinitionId}' was found."));
        }

        var result = workflow.AddStage(request.Name, request.Type, request.Order);
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id);
    }
}
