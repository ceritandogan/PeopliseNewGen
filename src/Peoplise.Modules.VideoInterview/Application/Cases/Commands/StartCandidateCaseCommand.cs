using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.Entities;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Commands;

/// <summary>
/// Keyed by <see cref="PositionId"/>, not <c>CaseBotProjectId</c> — same reasoning as
/// HrBot's <c>StartConversationCommand</c>: the candidate app already thinks entirely in
/// terms of positions, and has no reason to know a <c>CaseBotProject</c> exists as its
/// own concept.
/// </summary>
public sealed record StartCandidateCaseCommand(Guid PositionId, Guid CandidateId) : IRequest<Result<StartCandidateCaseResult>>;

public sealed record StartCandidateCaseResult(
    Guid CaseId,
    int RetakesAllowed,
    Guid StepId,
    StepType StepType,
    string Content,
    int? PreparationTimeSeconds,
    int? RecordingTimeSeconds);

public sealed class StartCandidateCaseCommandValidator : AbstractValidator<StartCandidateCaseCommand>
{
    public StartCandidateCaseCommandValidator()
    {
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
    }
}

public sealed class StartCandidateCaseCommandHandler : IRequestHandler<StartCandidateCaseCommand, Result<StartCandidateCaseResult>>
{
    private readonly AppDbContext _context;
    private readonly IRepository<Case, CaseId> _cases;
    private readonly IUnitOfWork _unitOfWork;

    public StartCandidateCaseCommandHandler(
        AppDbContext context,
        IRepository<Case, CaseId> cases,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _cases = cases;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StartCandidateCaseResult>> Handle(StartCandidateCaseCommand request, CancellationToken cancellationToken)
    {
        // Lookup by PositionId, not by CaseBotProjectId — beyond IRepository's simple
        // by-id contract, so this reads via AppDbContext directly (same reasoning as
        // HrBot's StartConversationCommandHandler).
        var project = await _context.Set<CaseBotProject>()
            .SingleOrDefaultAsync(p => p.PositionId == request.PositionId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<StartCandidateCaseResult>(
                Error.NotFound("CaseBotProject.NotFound", $"No case bot project is configured for position '{request.PositionId}'."));
        }

        var defaultFlow = project.DefaultFlow();
        if (defaultFlow is null)
            return Result.Failure<StartCandidateCaseResult>(Error.Conflict("CaseBotProject.NoDefaultFlow", "This project has no default flow to start from."));

        var firstStep = defaultFlow.FirstStep();
        if (firstStep is null)
            return Result.Failure<StartCandidateCaseResult>(Error.Conflict("CaseBotProject.EmptyDefaultFlow", "The default flow has no steps."));

        var @case = Case.Start(project.Id, request.CandidateId, defaultFlow.Id, firstStep.Id, DateTimeOffset.UtcNow);

        await _cases.AddAsync(@case, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new StartCandidateCaseResult(
            @case.Id.Value,
            project.RetakesAllowed,
            firstStep.Id,
            firstStep.Type,
            firstStep.Content,
            firstStep.PreparationTimeSeconds,
            firstStep.RecordingTimeSeconds));
    }
}
