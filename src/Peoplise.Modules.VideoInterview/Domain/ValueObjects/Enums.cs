namespace Peoplise.Modules.VideoInterview.Domain.ValueObjects;

/// <summary>
/// Adım Tipleri. One non-polymorphic <c>Step</c> class carries whichever of these
/// optional fields its type actually needs (preparation/recording time, capture
/// variable, etc.) rather than a type hierarchy per step kind — deliberately simpler
/// than a fully polymorphic model, matching the "don't over-abstract for a small team"
/// guidance; see the module's design notes.
/// </summary>
public enum StepType
{
    ShowMessage,
    PlayVideoQuestion,
    RecordVideoAnswer,
    UploadDocument,
    TakeNote,
    AddCalendarEvent,
    SetPoint,
    QuickReply,
    FillInTheBlank,
    BasketQuestion,
    SoftwareDevelopmentQuestion,
}

public enum CaseStatus
{
    InProgress,
    Completed,
    TimedOut,
    ConsentWithdrawn,
    RetentionExpired,
}
