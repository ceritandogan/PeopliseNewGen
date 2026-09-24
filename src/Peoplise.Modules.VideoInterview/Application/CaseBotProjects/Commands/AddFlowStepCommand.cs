using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;

public sealed record AddFlowStepCommand(
    Guid CaseBotProjectId,
    Guid FlowId,
    StepType Type,
    string Content,
    int Order,
    int? PreparationTimeSeconds,
    int? RecordingTimeSeconds,
    IReadOnlyList<Guid>? RelatedCompetencyIds) : IRequest<Result<Guid>>;

public sealed class AddFlowStepCommandValidator : AbstractValidator<AddFlowStepCommand>
{
    public AddFlowStepCommandValidator()
    {
        RuleFor(x => x.CaseBotProjectId).NotEmpty();
        RuleFor(x => x.FlowId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Loads and saves through CaseBotProject's own repository — Flow/Step are EF owned entities with no repository of their own, same shape as AddCompetencyCommand.</summary>
public sealed class AddFlowStepCommandHandler : IRequestHandler<AddFlowStepCommand, Result<Guid>>
{
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public AddFlowStepCommandHandler(IRepository<CaseBotProject, CaseBotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddFlowStepCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(CaseBotProjectId.From(request.CaseBotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<Guid>(Error.NotFound("CaseBotProject.NotFound", $"No project '{request.CaseBotProjectId}' was found."));

        var flow = project.FindFlow(request.FlowId);
        if (flow is null)
            return Result.Failure<Guid>(Error.NotFound("CaseBotProject.FlowNotFound", $"No flow '{request.FlowId}' was found on this project."));

        var result = flow.AddStep(
            request.Type, request.Content, request.Order,
            request.PreparationTimeSeconds, request.RecordingTimeSeconds, request.RelatedCompetencyIds);
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(result.Value.Id);
    }
}
