using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.ValueObjects;

/// <summary>Strongly-typed identity for the <c>BotProject</c> aggregate root.</summary>
public sealed class BotProjectId : ValueObject
{
    public Guid Value { get; }

    private BotProjectId(Guid value) => Value = value;

    public static BotProjectId New() => new(Guid.NewGuid());

    public static BotProjectId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A bot project id cannot be empty.", nameof(value));

        return new BotProjectId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
