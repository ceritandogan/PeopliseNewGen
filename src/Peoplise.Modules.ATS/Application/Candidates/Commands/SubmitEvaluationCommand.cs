using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Commands;

public sealed record SubmitEvaluationCommand(
    Guid CandidateProcessId,
    string EvaluatorId,
    decimal Score,
    string? Comments) : IRequest<Result>;

public sealed class SubmitEvaluationCommandValidator : AbstractValidator<SubmitEvaluationCommand>
{
    public SubmitEvaluationCommandValidator()
    {
        RuleFor(x => x.CandidateProcessId).NotEmpty();
        RuleFor(x => x.EvaluatorId).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(EvaluationScore.MinValue, EvaluationScore.MaxValue);
    }
}

public sealed class SubmitEvaluationCommandHandler : IRequestHandler<SubmitEvaluationCommand, Result>
{
    private readonly IRepository<CandidateProcess, CandidateProcessId> _processes;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitEvaluationCommandHandler(IRepository<CandidateProcess, CandidateProcessId> processes, IUnitOfWork unitOfWork)
    {
        _processes = processes;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SubmitEvaluationCommand request, CancellationToken cancellationToken)
    {
        var process = await _processes.GetByIdAsync(CandidateProcessId.From(request.CandidateProcessId), cancellationToken);
        if (process is null)
            return Result.Failure(Error.NotFound("CandidateProcess.NotFound", $"No candidate process '{request.CandidateProcessId}' was found."));

        var result = process.SubmitEvaluation(
            request.EvaluatorId, EvaluationScore.From(request.Score), request.Comments, DateTimeOffset.UtcNow);

        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
