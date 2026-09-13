using Peoplise.SharedKernel.AI;

namespace Peoplise.Infrastructure.AI;

/// <summary>
/// The default <see cref="IAIProvider"/> registration: fails loudly and immediately
/// rather than silently returning fake transcripts or fake scores. This is deliberate —
/// wiring a real Azure OpenAI / OpenAI / Claude SDK call needs real API credentials this
/// environment doesn't have, and a call site that *looks* like it works but quietly
/// returns placeholder data is worse than one that fails clearly with instructions.
/// Replace this registration (see <c>DependencyInjection.AddInfrastructure</c>) once a
/// real provider and its credentials exist.
/// </summary>
public sealed class NotConfiguredAIProvider : IAIProvider
{
    public Task<string> TranscribeAsync(string videoUrl, CancellationToken cancellationToken = default) =>
        throw NotConfigured();

    public Task<CodeReviewResult> ReviewCodeAsync(string question, string candidateCode, CancellationToken cancellationToken = default) =>
        throw NotConfigured();

    private static NotSupportedException NotConfigured() => new(
        "No AI provider is configured. Implement IAIProvider against Azure OpenAI, OpenAI, "
        + "or Claude and register it in DependencyInjection.AddInfrastructure in place of "
        + "NotConfiguredAIProvider before using any AI-dependent feature.");
}
