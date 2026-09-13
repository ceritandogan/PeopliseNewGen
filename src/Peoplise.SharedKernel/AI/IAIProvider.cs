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
