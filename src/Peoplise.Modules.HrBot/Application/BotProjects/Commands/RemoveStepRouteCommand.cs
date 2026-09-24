using FluentValidation;
using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.BotProjects.Commands;

public sealed record RemoveStepRouteCommand(Guid BotProjectId, Guid FlowId, Guid StepId, Guid RouteId) : IRequest<Result>;

public sealed class RemoveStepRouteCommandValidator : AbstractValidator<RemoveStepRouteCommand>
{
    public RemoveStepRouteCommandValidator()
    {
        RuleFor(x => x.BotProjectId).NotEmpty();
        RuleFor(x => x.FlowId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.RouteId).NotEmpty();
    }
}

public sealed class RemoveStepRouteCommandHandler : IRequestHandler<RemoveStepRouteCommand, Result>
{
    private readonly IRepository<BotProject, BotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveStepRouteCommandHandler(IRepository<BotProject, BotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveStepRouteCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(BotProjectId.From(request.BotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure(Error.NotFound("BotProject.NotFound", $"No project '{request.BotProjectId}' was found."));

        var flow = project.FindFlow(request.FlowId);
        if (flow is null)
            return Result.Failure(Error.NotFound("BotProject.FlowNotFound", $"No flow '{request.FlowId}' was found on this project."));

        var step = flow.FindStep(request.StepId);
        if (step is null)
            return Result.Failure(Error.NotFound("Flow.StepNotFound", $"No step '{request.StepId}' was found on this flow."));

        var result = step.RemoveRoute(request.RouteId);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
