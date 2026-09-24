using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.Entities;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;

public sealed record AddCompetencyLevelCommand(Guid CaseBotProjectId, Guid CompetencyId, int Level, string Description) : IRequest<Result<Guid>>;

public sealed class AddCompetencyLevelCommandValidator : AbstractValidator<AddCompetencyLevelCommand>
{
    public AddCompetencyLevelCommandValidator()
    {
        RuleFor(x => x.CaseBotProjectId).NotEmpty();
        RuleFor(x => x.CompetencyId).NotEmpty();
        RuleFor(x => x.Level).InclusiveBetween(CompetencyLevel.MinLevel, CompetencyLevel.MaxLevel);
        RuleFor(x => x.Description).NotEmpty();
    }
}

/// <summary>Loads and saves through CaseBotProject's own repository — Competency/CompetencyLevel are EF owned entities with no repository of their own, same shape as AddCompetencyCommand.</summary>
public sealed class AddCompetencyLevelCommandHandler : IRequestHandler<AddCompetencyLevelCommand, Result<Guid>>
{
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public AddCompetencyLevelCommandHandler(IRepository<CaseBotProject, CaseBotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddCompetencyLevelCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(CaseBotProjectId.From(request.CaseBotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<Guid>(Error.NotFound("CaseBotProject.NotFound", $"No project '{request.CaseBotProjectId}' was found."));

        var competency = project.FindCompetency(request.CompetencyId);
        if (competency is null)
            return Result.Failure<Guid>(Error.NotFound("CaseBotProject.CompetencyNotFound", $"No competency '{request.CompetencyId}' was found on this project."));

        var level = new CompetencyLevel(Guid.NewGuid(), request.Level, request.Description);
        competency.AddLevel(level);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(level.Id);
    }
}
