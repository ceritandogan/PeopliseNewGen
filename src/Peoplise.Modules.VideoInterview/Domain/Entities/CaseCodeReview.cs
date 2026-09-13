using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>
/// The AI's 5-dimension code review for one <see cref="ValueObjects.StepType.SoftwareDevelopmentQuestion"/>
/// step — a fixed rubric distinct from the general <see cref="Competency"/> scoring
/// system (Section F vs. Section C/D of the architecture doc).
/// </summary>
public sealed class CaseCodeReview : BaseEntity<Guid>
{
    public Guid StepId { get; private set; }
    public int Readability { get; private set; }
    public int Functionality { get; private set; }
    public int DataValidation { get; private set; }
    public int UseCaseHandling { get; private set; }
    public int Syntax { get; private set; }
    public DateTimeOffset ReviewedAt { get; private set; }

    public int Total => Readability + Functionality + DataValidation + UseCaseHandling + Syntax;

    private CaseCodeReview()
    {
        // Reserved for EF Core materialization.
    }

    public CaseCodeReview(
        Guid id, Guid stepId, int readability, int functionality, int dataValidation,
        int useCaseHandling, int syntax, DateTimeOffset reviewedAt)
        : base(id)
    {
        StepId = stepId;
        Readability = readability;
        Functionality = functionality;
        DataValidation = dataValidation;
        UseCaseHandling = useCaseHandling;
        Syntax = syntax;
        ReviewedAt = reviewedAt;
    }
}
