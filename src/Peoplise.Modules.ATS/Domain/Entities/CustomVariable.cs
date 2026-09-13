using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Entities;

/// <summary>A position-specific key/value pair (custom fields, e.g. "budget_code": "R&amp;D-42").</summary>
public sealed class CustomVariable : BaseEntity<Guid>
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    private CustomVariable()
    {
        // Reserved for EF Core materialization.
    }

    public CustomVariable(Guid id, string key, string value) : base(id)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A custom variable must have a key.", nameof(key));

        Key = key;
        Value = value;
    }
}
