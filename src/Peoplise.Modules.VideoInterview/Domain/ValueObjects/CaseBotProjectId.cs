using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.ValueObjects;

public sealed class CaseBotProjectId : ValueObject
{
    public Guid Value { get; }

    private CaseBotProjectId(Guid value) => Value = value;

    public static CaseBotProjectId New() => new(Guid.NewGuid());

    public static CaseBotProjectId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A case bot project id cannot be empty.", nameof(value));

        return new CaseBotProjectId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
