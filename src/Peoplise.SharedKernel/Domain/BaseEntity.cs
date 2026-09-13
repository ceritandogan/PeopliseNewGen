namespace Peoplise.SharedKernel.Domain;

/// <summary>
/// Base class for every entity in the domain model: an object defined by its identity
/// (<typeparamref name="TId"/>) rather than by the equality of its attributes.
/// </summary>
/// <typeparam name="TId">The type of the entity's identity value.</typeparam>
public abstract class BaseEntity<TId> : IEquatable<BaseEntity<TId>>
    where TId : notnull
{
    /// <summary>
    /// The entity's identity. Two entities are equal when their <see cref="Id"/>s are
    /// equal, regardless of any other property values.
    /// </summary>
    public TId Id { get; protected set; }

    protected BaseEntity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Reserved for ORM materialization, which needs to construct an instance before
    /// <see cref="Id"/> is known.
    /// </summary>
    protected BaseEntity()
    {
        Id = default!;
    }

    public bool Equals(BaseEntity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as BaseEntity<TId>);

    public override int GetHashCode() => (GetType(), Id).GetHashCode();

    public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right) => !(left == right);
}
