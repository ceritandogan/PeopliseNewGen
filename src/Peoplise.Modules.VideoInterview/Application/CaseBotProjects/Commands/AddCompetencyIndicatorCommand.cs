using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.Entities;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;

public sealed record AddCompetencyIndicatorCommand(Guid CaseBotProjectId, Guid CompetencyId, string Description) : IRequest<Result<Guid>>;

public sealed class AddCompetencyIndicatorCommandValidator : AbstractValidator<AddCompetencyIndicatorCommand>
{
    public AddCompetencyIndicatorCommandValidator()
    {
        RuleFor(x => x.CaseBotProjectId).NotEmpty();
        RuleFor(x => x.CompetencyId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
    }
}

public sealed class AddCompetencyIndicatorCommandHandler : IRequestHandler<AddCompetencyIndicatorCommand, Result<Guid>>
{
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public AddCompetencyIndicatorCommandHandler(IRepository<CaseBotProject, CaseBotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddCompetencyIndicatorCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(CaseBotProjectId.From(request.CaseBotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<Guid>(Error.NotFound("CaseBotProject.NotFound", $"No project '{request.CaseBotProjectId}' was found."));

        var competency = project.FindCompetency(request.CompetencyId);
        if (competency is null)
            return Result.Failure<Guid>(Error.NotFound("CaseBotProject.CompetencyNotFound", $"No competency '{request.CompetencyId}' was found on this project."));

        var indicator = new BehavioralIndicator(Guid.NewGuid(), request.Description);
        competency.AddIndicator(indicator);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(indicator.Id);
    }
}
