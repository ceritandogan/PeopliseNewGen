namespace Peoplise.SharedKernel.AI;

/// <summary>
/// The AI/LLM strategy abstraction from the architecture decision summary (Azure
/// OpenAI, OpenAI, or Claude — swappable, never called directly). Two capabilities:
/// transcribing a candidate's video answer, and scoring a candidate's submitted code
/// against the 5-dimension rubric.
/// </summary>
public interface IAIProvider
{
    Task<string> TranscribeAsync(string videoUrl, CancellationToken cancellationToken = default);

    Task<CodeReviewResult> ReviewCodeAsync(string question, string candidateCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort extraction of contact fields from text HR pasted out of a candidate's
    /// LinkedIn profile (or any similar free text) — never a live fetch of LinkedIn itself,
    /// which has no public API for this and whose ToS a server-side scrape would violate.
    /// Any field not actually present in the text comes back <c>null</c>, never guessed.
    /// </summary>
    Task<CandidateProfileExtraction> ExtractCandidateProfileAsync(string pastedText, CancellationToken cancellationToken = default);
}

/// <summary>
/// The 5-dimension AI code review rubric: each dimension scored 0–20, for a 0–100
/// total — "Okunabilirlik, İşlevsellik, Veri Doğrulama, Kullanım Senaryosu, Sözdizimi."
/// </summary>
public sealed record CodeReviewResult(
    int Readability,
    int Functionality,
    int DataValidation,
    int UseCaseHandling,
    int Syntax)
{
    public const int MaxPerDimension = 20;

    public int Total => Readability + Functionality + DataValidation + UseCaseHandling + Syntax;
}

/// <summary>Any field the model didn't find literally present in the pasted text is <c>null</c>, not guessed.</summary>
public sealed record CandidateProfileExtraction(string? Name, string? Email, string? Phone, string? ResumeUrl);
