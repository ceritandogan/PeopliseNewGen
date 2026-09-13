using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.AI;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Commands;

public sealed record RequestAICodeReviewCommand(
    Guid CaseId,
    Guid StepId,
    string Question,
    string CandidateCode) : IRequest<Result<CodeReviewResult>>;

public sealed class RequestAICodeReviewCommandValidator : AbstractValidator<RequestAICodeReviewCommand>
{
    public RequestAICodeReviewCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.CandidateCode).NotEmpty();
    }
}

public sealed class RequestAICodeReviewCommandHandler : IRequestHandler<RequestAICodeReviewCommand, Result<CodeReviewResult>>
{
    private readonly IRepository<Case, CaseId> _cases;
    private readonly IAIProvider _aiProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RequestAICodeReviewCommandHandler(IRepository<Case, CaseId> cases, IAIProvider aiProvider, IUnitOfWork unitOfWork)
    {
        _cases = cases;
        _aiProvider = aiProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CodeReviewResult>> Handle(RequestAICodeReviewCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.GetByIdAsync(CaseId.From(request.CaseId), cancellationToken);
        if (@case is null)
            return Result.Failure<CodeReviewResult>(Error.NotFound("Case.NotFound", $"No case '{request.CaseId}' was found."));

        var review = await _aiProvider.ReviewCodeAsync(request.Question, request.CandidateCode, cancellationToken);

        @case.RecordCodeReview(request.StepId, review, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(review);
    }
}
