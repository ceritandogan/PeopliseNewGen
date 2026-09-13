using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.ValueObjects;

/// <summary>
/// A reviewer's (or a consensus's) score on a 0–100 scale. A value object rather than a
/// raw <c>decimal</c> so the valid range is enforced everywhere a score is created, not
/// re-checked at every call site.
/// </summary>
public sealed class EvaluationScore : ValueObject
{
    public const decimal MinValue = 0m;
    public const decimal MaxValue = 100m;

    public decimal Value { get; }

    private EvaluationScore(decimal value) => Value = value;

    public static EvaluationScore From(decimal value)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, $"An evaluation score must be between {MinValue} and {MaxValue}.");
        }

        return new EvaluationScore(value);
    }

    /// <summary>The average of one or more scores, itself a valid <see cref="EvaluationScore"/>.</summary>
    public static EvaluationScore Average(IReadOnlyCollection<EvaluationScore> scores)
    {
        if (scores.Count == 0)
            throw new ArgumentException("Cannot average zero scores.", nameof(scores));

        return From(scores.Average(s => s.Value));
    }

    public static bool operator >=(EvaluationScore left, EvaluationScore right) => left.Value >= right.Value;
    public static bool operator <=(EvaluationScore left, EvaluationScore right) => left.Value <= right.Value;
    public static bool operator >(EvaluationScore left, EvaluationScore right) => left.Value > right.Value;
    public static bool operator <(EvaluationScore left, EvaluationScore right) => left.Value < right.Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("0.##");
}
