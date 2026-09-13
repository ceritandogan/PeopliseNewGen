using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Commands;

public sealed record SubmitReviewerScoringCommand(
    Guid CaseId,
    string ReviewerId,
    Guid StepId,
    Guid CompetencyId,
    int Score,
    string? Notes) : IRequest<Result>;

public sealed class SubmitReviewerScoringCommandValidator : AbstractValidator<SubmitReviewerScoringCommand>
{
    public SubmitReviewerScoringCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.ReviewerId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.CompetencyId).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(0, 100);
    }
}

public sealed class SubmitReviewerScoringCommandHandler : IRequestHandler<SubmitReviewerScoringCommand, Result>
{
    private readonly IRepository<Case, CaseId> _cases;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitReviewerScoringCommandHandler(IRepository<Case, CaseId> cases, IUnitOfWork unitOfWork)
    {
        _cases = cases;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SubmitReviewerScoringCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.GetByIdAsync(CaseId.From(request.CaseId), cancellationToken);
        if (@case is null)
            return Result.Failure(Error.NotFound("Case.NotFound", $"No case '{request.CaseId}' was found."));

        var result = @case.SubmitScoring(
            request.ReviewerId, request.StepId, request.CompetencyId, request.Score, DateTimeOffset.UtcNow, notes: request.Notes);

        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
