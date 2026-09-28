using FluentValidation;
using MediatR;
using Peoplise.SharedKernel.AI;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Commands;

/// <summary>
/// Backs the "Aday ekle" form's optional LinkedIn-paste helper: HR copies text from a
/// candidate's LinkedIn profile (viewed in their own browser — this never fetches
/// LinkedIn itself, which has no public API for this and whose ToS a server-side scrape
/// would violate) and this pre-fills the form's name/email/phone/resumeUrl fields.
/// Named as a command, not a query, despite persisting nothing: like
/// <c>RequestAICodeReviewCommand</c>, it triggers a real, billed external call, not a
/// local read.
/// </summary>
public sealed record ExtractCandidateProfileFromTextCommand(string PastedText) : IRequest<Result<CandidateProfileExtraction>>;

public sealed class ExtractCandidateProfileFromTextCommandValidator : AbstractValidator<ExtractCandidateProfileFromTextCommand>
{
    public ExtractCandidateProfileFromTextCommandValidator()
    {
        RuleFor(x => x.PastedText).NotEmpty().MinimumLength(20);
    }
}

public sealed class ExtractCandidateProfileFromTextCommandHandler
    : IRequestHandler<ExtractCandidateProfileFromTextCommand, Result<CandidateProfileExtraction>>
{
    private readonly IAIProvider _aiProvider;

    public ExtractCandidateProfileFromTextCommandHandler(IAIProvider aiProvider)
    {
        _aiProvider = aiProvider;
    }

    public async Task<Result<CandidateProfileExtraction>> Handle(ExtractCandidateProfileFromTextCommand request, CancellationToken cancellationToken)
    {
        var extraction = await _aiProvider.ExtractCandidateProfileAsync(request.PastedText, cancellationToken);
        return Result.Success(extraction);
    }
}
