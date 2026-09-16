using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Workflows.Commands;

public sealed record CreateWorkflowDefinitionCommand(string Name) : IRequest<Result<Guid>>;

public sealed class CreateWorkflowDefinitionCommandValidator : AbstractValidator<CreateWorkflowDefinitionCommand>
{
    public CreateWorkflowDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateWorkflowDefinitionCommandHandler : IRequestHandler<CreateWorkflowDefinitionCommand, Result<Guid>>
{
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWorkflowDefinitionCommandHandler(
        IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows,
        IUnitOfWork unitOfWork)
    {
        _workflows = workflows;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        var workflow = WorkflowDefinition.Create(request.Name);

        await _workflows.AddAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(workflow.Id.Value);
    }
}
