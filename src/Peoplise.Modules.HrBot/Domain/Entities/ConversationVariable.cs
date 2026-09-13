using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.Entities;

/// <summary>One captured answer within a specific <c>Conversation</c> — the value side of a <see cref="ProjectVariable"/>.</summary>
public sealed class ConversationVariable : BaseEntity<Guid>
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public DateTimeOffset CapturedAt { get; private set; }

    private ConversationVariable()
    {
        // Reserved for EF Core materialization.
    }

    public ConversationVariable(Guid id, string key, string value, DateTimeOffset capturedAt) : base(id)
    {
        Key = key;
        Value = value;
        CapturedAt = capturedAt;
    }
}
