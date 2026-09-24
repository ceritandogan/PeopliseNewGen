using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.BotProjects.Queries;

/// <summary>The authored flow/step/route structure for one project — nothing returned this before this feature existed.</summary>
public sealed record GetFlowsForBotProjectQuery(Guid BotProjectId) : IRequest<Result<IReadOnlyList<BotFlowDto>>>;

public sealed record BotFlowDto(Guid Id, string Name, bool IsDefault, IReadOnlyList<BotFlowStepDto> Steps);

public sealed record BotFlowStepDto(
    Guid Id,
    StepType Type,
    string Content,
    int Order,
    IReadOnlyList<string> QuickReplyOptions,
    string? CaptureVariableKey,
    bool IsFinalStep,
    bool IsScreenOut,
    IReadOnlyList<BotStepRouteDto> Routes);

public sealed record BotStepRouteDto(
    Guid Id,
    ConditionType ConditionType,
    IReadOnlyList<string> Keywords,
    StepRouteType RouteType,
    Guid? TargetFlowId,
    Guid? TargetStepId);

public sealed class GetFlowsForBotProjectQueryHandler : IRequestHandler<GetFlowsForBotProjectQuery, Result<IReadOnlyList<BotFlowDto>>>
{
    private readonly IRepository<BotProject, BotProjectId> _projects;

    public GetFlowsForBotProjectQueryHandler(IRepository<BotProject, BotProjectId> projects)
    {
        _projects = projects;
    }

    public async Task<Result<IReadOnlyList<BotFlowDto>>> Handle(GetFlowsForBotProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(BotProjectId.From(request.BotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<IReadOnlyList<BotFlowDto>>(Error.NotFound("BotProject.NotFound", $"No project '{request.BotProjectId}' was found."));

        IReadOnlyList<BotFlowDto> flows = project.Flows
            .Select(f => new BotFlowDto(
                f.Id,
                f.Name,
                f.IsDefault,
                f.Steps.Select(s => new BotFlowStepDto(
                    s.Id, s.Type, s.Content, s.Order, s.QuickReplyOptions.ToList(), s.CaptureVariableKey, s.IsFinalStep, s.IsScreenOut,
                    s.Routes.Select(r => new BotStepRouteDto(r.Id, r.ConditionType, r.Keywords.ToList(), r.RouteType, r.TargetFlowId, r.TargetStepId)).ToList()))
                    .ToList()))
            .ToList();

        return Result.Success(flows);
    }
}
