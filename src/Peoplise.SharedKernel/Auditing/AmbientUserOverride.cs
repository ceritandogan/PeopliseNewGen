namespace Peoplise.SharedKernel.Auditing;

/// <summary>
/// An ambient "who's making this change" override that flows through the current async
/// call chain, independent of whether there's an HTTP request at all — the audit-trail
/// counterpart to <see cref="MultiTenancy.AmbientTenantOverride"/>. A scheduled
/// background job has no signed-in user to stamp <see cref="IAuditableEntity"/>'s
/// Created/UpdatedBy fields with; this lets it identify itself (e.g.
/// <c>"system:kvkk-retention-job"</c>) instead of leaving them null.
/// <see cref="ICurrentUserContext"/> implementations should check <see cref="Current"/>
/// before falling back to their normal resolution (an HTTP claim, typically).
/// </summary>
public static class AmbientUserOverride
{
    private static readonly AsyncLocal<string?> CurrentValue = new();

    public static string? Current => CurrentValue.Value;

    /// <summary>Sets the override for the duration of the returned scope, restoring whatever was there before on dispose.</summary>
    public static IDisposable Begin(string userId)
    {
        var previous = CurrentValue.Value;
        CurrentValue.Value = userId;
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly string? _previous;

        public RestoreScope(string? previous) => _previous = previous;

        public void Dispose() => CurrentValue.Value = _previous;
    }
}
