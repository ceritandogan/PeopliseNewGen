using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>
/// What the candidate submitted at one step — a video, a document, free text, or a
/// quick-reply choice, plus the transcript once the background transcription job
/// finishes with it.
/// </summary>
public sealed class CaseStepConversation : BaseEntity<Guid>
{
    public Guid StepId { get; private set; }
    public string? TextResponse { get; private set; }
    public string? VideoUrl { get; private set; }
    public string? DocumentUrl { get; private set; }
    public string? TranscriptText { get; private set; }
    public DateTimeOffset RespondedAt { get; private set; }

    private CaseStepConversation()
    {
        // Reserved for EF Core materialization.
    }

    public CaseStepConversation(
        Guid id, Guid stepId, DateTimeOffset respondedAt,
        string? textResponse = null, string? videoUrl = null, string? documentUrl = null)
        : base(id)
    {
        StepId = stepId;
        RespondedAt = respondedAt;
        TextResponse = textResponse;
        VideoUrl = videoUrl;
        DocumentUrl = documentUrl;
    }

    public void SetTranscript(string transcriptText) => TranscriptText = transcriptText;

    /// <summary>KVKK deletion: clears media references (the files themselves are deleted via <c>IFileStorageService</c> by the caller first).</summary>
    public void ClearMediaReferences()
    {
        VideoUrl = null;
        DocumentUrl = null;
        TranscriptText = null;
    }
}
