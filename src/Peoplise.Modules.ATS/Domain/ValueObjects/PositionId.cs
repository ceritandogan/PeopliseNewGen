using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.ValueObjects;

/// <summary>Strongly-typed identity for the <c>Position</c> aggregate root.</summary>
public sealed class PositionId : ValueObject
{
    public Guid Value { get; }

    private PositionId(Guid value) => Value = value;

    public static PositionId New() => new(Guid.NewGuid());

    public static PositionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A position id cannot be empty.", nameof(value));

        return new PositionId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
