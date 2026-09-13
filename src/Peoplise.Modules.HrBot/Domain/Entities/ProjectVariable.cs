using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.Entities;

/// <summary>
/// The *definition* of a variable a <c>BotProject</c>'s flows can capture (e.g. "salary
/// expectation") — distinct from <see cref="ConversationVariable"/>, which is the actual
/// value captured in one specific conversation.
/// </summary>
public sealed class ProjectVariable : BaseEntity<Guid>
{
    public string Key { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private ProjectVariable()
    {
        // Reserved for EF Core materialization.
    }

    public ProjectVariable(Guid id, string key, string? description) : base(id)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A project variable must have a key.", nameof(key));

        Key = key;
        Description = description;
    }
}
