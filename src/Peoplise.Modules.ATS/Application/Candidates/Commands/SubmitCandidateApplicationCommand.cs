using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Commands;

public sealed record SubmitCandidateApplicationCommand(
    Guid CandidateId,
    Guid PositionId,
    Guid WorkflowDefinitionId,
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
        RuleFor(x => x.WorkflowDefinitionId).NotEmpty();
        RuleFor(x => x.CandidateName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CandidateEmail).NotEmpty().EmailAddress();
    }
}

public sealed class SubmitCandidateApplicationCommandHandler : IRequestHandler<SubmitCandidateApplicationCommand, Result<Guid>>
{
    private readonly AppDbContext _context;
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;
    private readonly IRepository<CandidateProcess, CandidateProcessId> _processes;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitCandidateApplicationCommandHandler(
        AppDbContext context,
        IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows,
        IRepository<CandidateProcess, CandidateProcessId> processes,
        IUnitOfWork unitOfWork)
    {
        _context = context;
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

        var workflow = await _workflows.GetByIdAsync(WorkflowDefinitionId.From(request.WorkflowDefinitionId), cancellationToken);
        if (workflow is null)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "WorkflowDefinition.NotFound", $"No workflow definition '{request.WorkflowDefinitionId}' was found."));
        }

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
