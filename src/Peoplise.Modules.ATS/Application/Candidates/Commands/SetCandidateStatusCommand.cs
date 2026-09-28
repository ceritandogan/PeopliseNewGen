using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Commands;

/// <summary>
/// The Kanban board's drag-and-drop move — see <see cref="CandidateProcess.SetStatus"/>'s
/// remarks for why this is deliberately permissive (any status to any status, including
/// reversing Accepted/Rejected) and why Eliminated/TimedOut are refused as targets.
/// </summary>
public sealed record SetCandidateStatusCommand(Guid CandidateProcessId, PipelineStatus Status) : IRequest<Result>;

public sealed class SetCandidateStatusCommandValidator : AbstractValidator<SetCandidateStatusCommand>
{
    public SetCandidateStatusCommandValidator()
    {
        RuleFor(x => x.CandidateProcessId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class SetCandidateStatusCommandHandler : IRequestHandler<SetCandidateStatusCommand, Result>
{
    private readonly IRepository<CandidateProcess, CandidateProcessId> _processes;
    private readonly IUnitOfWork _unitOfWork;

    public SetCandidateStatusCommandHandler(IRepository<CandidateProcess, CandidateProcessId> processes, IUnitOfWork unitOfWork)
    {
        _processes = processes;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetCandidateStatusCommand request, CancellationToken cancellationToken)
    {
        var process = await _processes.GetByIdAsync(CandidateProcessId.From(request.CandidateProcessId), cancellationToken);
        if (process is null)
            return Result.Failure(Error.NotFound("CandidateProcess.NotFound", $"No candidate process '{request.CandidateProcessId}' was found."));

        var result = process.SetStatus(request.Status);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
