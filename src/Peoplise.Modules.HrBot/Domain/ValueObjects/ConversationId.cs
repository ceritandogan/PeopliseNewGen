using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.ValueObjects;

/// <summary>Strongly-typed identity for the <c>Conversation</c> aggregate root.</summary>
public sealed class ConversationId : ValueObject
{
    public Guid Value { get; }

    private ConversationId(Guid value) => Value = value;

    public static ConversationId New() => new(Guid.NewGuid());

    public static ConversationId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A conversation id cannot be empty.", nameof(value));

        return new ConversationId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
