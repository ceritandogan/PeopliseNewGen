using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Commands;

/// <summary>The command <see cref="CandidateProcess.AddNote"/> never had wrapping it — flagged since Stage 5's frontend build (the Candidate Detail page's note UI had nothing to actually call).</summary>
public sealed record AddCandidateNoteCommand(
    Guid CandidateProcessId,
    string AuthorId,
    string Text,
    bool IsPrivate) : IRequest<Result>;

public sealed class AddCandidateNoteCommandValidator : AbstractValidator<AddCandidateNoteCommand>
{
    public AddCandidateNoteCommandValidator()
    {
        RuleFor(x => x.CandidateProcessId).NotEmpty();
        RuleFor(x => x.AuthorId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty();
    }
}

public sealed class AddCandidateNoteCommandHandler : IRequestHandler<AddCandidateNoteCommand, Result>
{
    private readonly IRepository<CandidateProcess, CandidateProcessId> _processes;
    private readonly IUnitOfWork _unitOfWork;

    public AddCandidateNoteCommandHandler(IRepository<CandidateProcess, CandidateProcessId> processes, IUnitOfWork unitOfWork)
    {
        _processes = processes;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddCandidateNoteCommand request, CancellationToken cancellationToken)
    {
        var process = await _processes.GetByIdAsync(CandidateProcessId.From(request.CandidateProcessId), cancellationToken);
        if (process is null)
            return Result.Failure(Error.NotFound("CandidateProcess.NotFound", $"No candidate process '{request.CandidateProcessId}' was found."));

        process.AddNote(request.AuthorId, request.Text, request.IsPrivate, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
