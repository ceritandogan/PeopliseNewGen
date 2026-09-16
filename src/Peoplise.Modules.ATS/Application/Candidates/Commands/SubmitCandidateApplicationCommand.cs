using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Commands;

/// <summary>
/// Deliberately takes only <see cref="PositionId"/>, not a <c>WorkflowDefinitionId</c> —
/// a public-facing apply form has no business knowing a position's internal workflow
/// id, and since <c>CreatePositionCommand</c> now auto-provisions a workflow for every
/// position, there's always exactly one to resolve server-side via
/// <see cref="Position.WorkflowDefinitionId"/>.
/// </summary>
public sealed record SubmitCandidateApplicationCommand(
    Guid CandidateId,
    Guid PositionId,
    string CandidateName,
    string CandidateEmail,
    string? CandidatePhone,
    string? ResumeUrl) : IRequest<Result<Guid>>;

public sealed class SubmitCandidateApplicationCommandValidator : AbstractValidator<SubmitCandidateApplicationCommand>
{
    public SubmitCandidateApplicationCommandValidator()
    {
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.CandidateName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CandidateEmail).NotEmpty().EmailAddress();
    }
}

public sealed class SubmitCandidateApplicationCommandHandler : IRequestHandler<SubmitCandidateApplicationCommand, Result<Guid>>
{
    private readonly AppDbContext _context;
    private readonly IRepository<Position, PositionId> _positions;
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;
    private readonly IRepository<CandidateProcess, CandidateProcessId> _processes;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitCandidateApplicationCommandHandler(
        AppDbContext context,
        IRepository<Position, PositionId> positions,
        IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows,
        IRepository<CandidateProcess, CandidateProcessId> processes,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _positions = positions;
        _workflows = workflows;
        _processes = processes;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(SubmitCandidateApplicationCommand request, CancellationToken cancellationToken)
    {
        var candidateId = CandidateId.From(request.CandidateId);
        var positionId = PositionId.From(request.PositionId);

        // "Bir aday aynı pozisyona iki kez başvuramaz" — this spans every existing
        // CandidateProcess for the pair, which no single aggregate instance can see;
        // the query here is the enforcement point (backed by a unique DB index, scoped
        // per tenant, as a second line of defense — see CandidateProcessConfiguration).
        // Deliberately goes through the normal (tenant + soft-delete filtered) query:
        // a withdrawn/soft-deleted prior application should not block a fresh one.
        var alreadyApplied = await _context.Set<CandidateProcess>()
            .AnyAsync(p => p.CandidateId == candidateId && p.PositionId == positionId, cancellationToken);

        if (alreadyApplied)
        {
            return Result.Failure<Guid>(Error.Conflict(
                "CandidateProcess.AlreadyApplied", "This candidate has already applied to this position."));
        }

        var position = await _positions.GetByIdAsync(positionId, cancellationToken);
        if (position is null)
            return Result.Failure<Guid>(Error.NotFound("Position.NotFound", $"No position '{request.PositionId}' was found."));

        if (position.WorkflowDefinitionId is null)
        {
            return Result.Failure<Guid>(Error.Conflict(
                "Position.NoWorkflow", "This position has no workflow assigned yet and cannot accept applications."));
        }

        var workflow = await _workflows.GetByIdAsync(position.WorkflowDefinitionId, cancellationToken);
        if (workflow is null)
            return Result.Failure<Guid>(Error.NotFound("WorkflowDefinition.NotFound", "The position's workflow definition could not be found."));

        var firstStage = workflow.Stages.OrderBy(s => s.Order).FirstOrDefault();

        var process = CandidateProcess.Submit(
            candidateId, positionId, workflow.Id,
            request.CandidateName, request.CandidateEmail, request.CandidatePhone, request.ResumeUrl,
            firstStage?.Id, DateTimeOffset.UtcNow);

        await _processes.AddAsync(process, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(process.Id.Value);
    }
}
