using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.ValueObjects;

/// <summary>
/// Identifies a candidate (a person) across every position they apply to. Not an
/// aggregate root itself in this MVP — there is no separate rich "Candidate" aggregate
/// yet, only per-application state on <c>CandidateProcess</c> — but kept as its own
/// value object rather than a raw <see cref="Guid"/> because it is referenced from
/// multiple aggregates (<c>CandidateProcess</c>, and a future talent-pool/candidate
/// profile feature) and stands for a distinct concept from a process's own identity.
/// </summary>
public sealed class CandidateId : ValueObject
{
    public Guid Value { get; }

    private CandidateId(Guid value) => Value = value;

    public static CandidateId New() => new(Guid.NewGuid());

    public static CandidateId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A candidate id cannot be empty.", nameof(value));

        return new CandidateId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
