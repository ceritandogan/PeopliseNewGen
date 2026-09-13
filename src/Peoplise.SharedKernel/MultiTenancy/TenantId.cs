using Peoplise.SharedKernel.Domain;

namespace Peoplise.SharedKernel.MultiTenancy;

/// <summary>
/// Identifies the tenant an entity or request belongs to, in this shared-database,
/// row-level multi-tenancy model. Wrapping the raw <see cref="Guid"/> stops a tenant id
/// from being interchanged with any other <see cref="Guid"/>-typed identifier by accident.
/// </summary>
public sealed class TenantId : ValueObject
{
    public Guid Value { get; }

    private TenantId(Guid value) => Value = value;

    public static TenantId New() => new(Guid.NewGuid());

    public static TenantId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A tenant id cannot be empty.", nameof(value));

        return new TenantId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
