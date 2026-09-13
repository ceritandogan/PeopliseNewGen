namespace Peoplise.Modules.ATS.Domain.ValueObjects;

/// <summary>Aşama Tipleri — the kind of step a <c>Stage</c> represents in a workflow.</summary>
public enum StageType
{
    InformationForm,
    ScreeningTest,
    VideoInterview,
    DocumentCollection,
    LiveInterview,
    ReviewerApproval,
    OfferStage,
}

/// <summary>Where a <c>CandidateProcess</c> sits in the hiring pipeline.</summary>
public enum PipelineStatus
{
    NewApplication,
    UnderReview,
    Testing,
    Interviewing,
    Offer,
    Accepted,
    Rejected,
    Eliminated,
    TimedOut,
}

/// <summary>Çalışma modeli — where the role is performed.</summary>
public enum WorkMode
{
    Office,
    Remote,
    Hybrid,
}

/// <summary>Kıdem seviyesi — the position's seniority band.</summary>
public enum SeniorityLevel
{
    Intern,
    Junior,
    Mid,
    Senior,
    Lead,
    Principal,
}

/// <summary>Pozisyon tipi — full-time vs. part-time.</summary>
public enum EmploymentType
{
    FullTime,
    PartTime,
}

/// <summary>Rol bazlı erişim — a position team member's role in the hiring team.</summary>
public enum PositionRole
{
    Manager,
    Evaluator,
    Observer,
}

/// <summary>The kind of transition a <c>StageRule</c> triggers.</summary>
public enum StageRuleType
{
    /// <summary>"Puan ≥ X ise sonraki aşamaya otomatik taşı" — needs <c>Threshold</c>.</summary>
    AdvanceIfScoreAtLeast,

    /// <summary>"Puan &lt; Y ise adayı otomatik ele" — needs <c>Threshold</c>.</summary>
    EliminateIfScoreBelow,

    /// <summary>"Önceki aşama tamamlandıktan Z gün sonra sonraki aşamayı aktif et" — needs <c>DelayDays</c>.</summary>
    ActivateAfterDelay,
}
