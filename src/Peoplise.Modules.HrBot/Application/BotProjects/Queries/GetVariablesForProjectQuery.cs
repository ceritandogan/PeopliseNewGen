using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.BotProjects.Queries;

/// <summary>The declared variables a project's WaitResponse steps can capture answers into — distinct from ConversationVariable, the actual per-conversation value.</summary>
public sealed record GetVariablesForProjectQuery(Guid BotProjectId) : IRequest<Result<IReadOnlyList<ProjectVariableDto>>>;

public sealed record ProjectVariableDto(Guid Id, string Key, string? Description);

public sealed class GetVariablesForProjectQueryHandler : IRequestHandler<GetVariablesForProjectQuery, Result<IReadOnlyList<ProjectVariableDto>>>
{
    private readonly IRepository<BotProject, BotProjectId> _projects;

    public GetVariablesForProjectQueryHandler(IRepository<BotProject, BotProjectId> projects)
    {
        _projects = projects;
    }

    public async Task<Result<IReadOnlyList<ProjectVariableDto>>> Handle(GetVariablesForProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(BotProjectId.From(request.BotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<IReadOnlyList<ProjectVariableDto>>(Error.NotFound("BotProject.NotFound", $"No project '{request.BotProjectId}' was found."));

        IReadOnlyList<ProjectVariableDto> variables = project.Variables.Select(v => new ProjectVariableDto(v.Id, v.Key, v.Description)).ToList();
        return Result.Success(variables);
    }
}
