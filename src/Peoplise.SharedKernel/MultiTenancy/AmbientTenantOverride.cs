namespace Peoplise.SharedKernel.MultiTenancy;

/// <summary>
/// An ambient tenant override that flows through the current async call chain,
/// independent of whether there's an HTTP request at all. Two callers need this: the
/// one deliberately anonymous write in the system (a candidate applying, with no
/// session and so no tenant claim — resolves the tenant from the position being
/// applied to instead) and startup database seeding (runs before any request exists).
/// <see cref="ITenantContext"/> implementations should check <see cref="Current"/>
/// before falling back to their normal resolution (a JWT claim, typically).
/// </summary>
public static class AmbientTenantOverride
{
    private static readonly AsyncLocal<TenantId?> CurrentValue = new();

    public static TenantId? Current => CurrentValue.Value;

    /// <summary>Sets the override for the duration of the returned scope, restoring whatever was there before on dispose.</summary>
    public static IDisposable Begin(TenantId tenantId)
    {
        var previous = CurrentValue.Value;
        CurrentValue.Value = tenantId;
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly TenantId? _previous;

        public RestoreScope(TenantId? previous) => _previous = previous;

        public void Dispose() => CurrentValue.Value = _previous;
    }
}
