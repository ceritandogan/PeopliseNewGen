using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identity for the <c>CandidateProcess</c> aggregate root — one per
/// (candidate, position) application, distinct from <see cref="CandidateId"/> because a
/// candidate can have concurrent processes against different positions.
/// </summary>
public sealed class CandidateProcessId : ValueObject
{
    public Guid Value { get; }

    private CandidateProcessId(Guid value) => Value = value;

    public static CandidateProcessId New() => new(Guid.NewGuid());

    public static CandidateProcessId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A candidate process id cannot be empty.", nameof(value));

        return new CandidateProcessId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
