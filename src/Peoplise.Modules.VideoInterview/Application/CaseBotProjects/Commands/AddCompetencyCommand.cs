using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;

public sealed record AddCompetencyCommand(Guid CaseBotProjectId, string Name, string? Description) : IRequest<Result<Guid>>;

public sealed class AddCompetencyCommandValidator : AbstractValidator<AddCompetencyCommand>
{
    public AddCompetencyCommandValidator()
    {
        RuleFor(x => x.CaseBotProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class AddCompetencyCommandHandler : IRequestHandler<AddCompetencyCommand, Result<Guid>>
{
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public AddCompetencyCommandHandler(IRepository<CaseBotProject, CaseBotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddCompetencyCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(CaseBotProjectId.From(request.CaseBotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<Guid>(Error.NotFound("CaseBotProject.NotFound", $"No project '{request.CaseBotProjectId}' was found."));

        var competency = project.AddCompetency(request.Name, request.Description);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(competency.Id);
    }
}
