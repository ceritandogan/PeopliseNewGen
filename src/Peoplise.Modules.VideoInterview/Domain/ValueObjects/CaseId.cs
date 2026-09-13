using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.ValueObjects;

public sealed class CaseId : ValueObject
{
    public Guid Value { get; }

    private CaseId(Guid value) => Value = value;

    public static CaseId New() => new(Guid.NewGuid());

    public static CaseId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A case id cannot be empty.", nameof(value));

        return new CaseId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
