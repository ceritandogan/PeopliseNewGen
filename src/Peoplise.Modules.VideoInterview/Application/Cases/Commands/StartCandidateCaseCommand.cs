using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Commands;

public sealed record StartCandidateCaseCommand(Guid CaseBotProjectId, Guid CandidateId) : IRequest<Result<Guid>>;

public sealed class StartCandidateCaseCommandValidator : AbstractValidator<StartCandidateCaseCommand>
{
    public StartCandidateCaseCommandValidator()
    {
        RuleFor(x => x.CaseBotProjectId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
    }
}

public sealed class StartCandidateCaseCommandHandler : IRequestHandler<StartCandidateCaseCommand, Result<Guid>>
{
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;
    private readonly IRepository<Case, CaseId> _cases;
    private readonly IUnitOfWork _unitOfWork;

    public StartCandidateCaseCommandHandler(
        IRepository<CaseBotProject, CaseBotProjectId> projects,
        IRepository<Case, CaseId> cases,
        IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _cases = cases;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(StartCandidateCaseCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(CaseBotProjectId.From(request.CaseBotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<Guid>(Error.NotFound("CaseBotProject.NotFound", $"No case bot project '{request.CaseBotProjectId}' was found."));

        var defaultFlow = project.DefaultFlow();
        if (defaultFlow is null)
            return Result.Failure<Guid>(Error.Conflict("CaseBotProject.NoDefaultFlow", "This project has no default flow to start from."));

        var firstStep = defaultFlow.FirstStep();
        if (firstStep is null)
            return Result.Failure<Guid>(Error.Conflict("CaseBotProject.EmptyDefaultFlow", "The default flow has no steps."));

        var @case = Case.Start(project.Id, request.CandidateId, defaultFlow.Id, firstStep.Id, DateTimeOffset.UtcNow);

        await _cases.AddAsync(@case, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(@case.Id.Value);
    }
}
