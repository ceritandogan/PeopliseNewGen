using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Peoplise.SharedKernel.AI;

namespace Peoplise.Infrastructure.AI;

/// <summary>
/// The real <see cref="IAIProvider"/> for code review, backed by the Claude API
/// (structured JSON output — no tool-use loop needed for a single scoring call). Only
/// registered once <c>AI:Anthropic:ApiKey</c> is configured; see
/// <c>scripts/setup-anthropic-key.sh</c> and <c>DependencyInjection.AddMediaAndAI</c>.
/// </summary>
public sealed class AnthropicAIProvider : IAIProvider
{
    private readonly AnthropicClient _client;
    private readonly string _model;

    public AnthropicAIProvider(AnthropicClient client, string model)
    {
        _client = client;
        _model = model;
    }

    /// <summary>
    /// The Anthropic API has no speech-to-text capability — this genuinely can't be
    /// implemented against Claude, regardless of scope. Video transcription would need
    /// a different provider (e.g. a dedicated STT service); until then this stays
    /// exactly as unconfigured as it was under <see cref="NotConfiguredAIProvider"/>.
    /// </summary>
    public Task<string> TranscribeAsync(string videoUrl, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "Anthropic's API has no transcription capability — TranscribeAsync can't be implemented against Claude.");

    public async Task<CodeReviewResult> ReviewCodeAsync(string question, string candidateCode, CancellationToken cancellationToken = default)
    {
        var schema = new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
            // Anthropic's structured-output schema is a restricted JSON Schema dialect —
            // "minimum"/"maximum" on an integer property is rejected outright ("are not
            // supported"), so the 0-20 range is prompt-instructed instead and clamped
            // defensively below rather than schema-enforced.
            ["properties"] = JsonSerializer.SerializeToElement(new
            {
                readability = new { type = "integer" },
                functionality = new { type = "integer" },
                dataValidation = new { type = "integer" },
                useCaseHandling = new { type = "integer" },
                syntax = new { type = "integer" },
            }),
            ["required"] = JsonSerializer.SerializeToElement(
                new[] { "readability", "functionality", "dataValidation", "useCaseHandling", "syntax" }),
        };

        var prompt = $"""
            You are reviewing a candidate's code submission for a technical interview question.

            Question: {question}

            Candidate's code:
            ```
            {candidateCode}
            ```

            Score the submission on each of these five dimensions, 0-{CodeReviewResult.MaxPerDimension} each:
            - readability: naming, structure, clarity
            - functionality: does it solve the stated problem
            - dataValidation: input validation, edge cases
            - useCaseHandling: handles the realistic use case, not just the happy path
            - syntax: correctness of the language syntax used
            """;

        var response = await _client.Messages.Create(
            new MessageCreateParams
            {
                Model = _model,
                MaxTokens = 1024,
                OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = schema } },
                Messages = [new() { Role = Role.User, Content = prompt }],
            },
            cancellationToken);

        var text = response.Content.Select(b => b.Value).OfType<TextBlock>().FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("The Anthropic response had no text content to parse as a code review result.");

        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;

        return new CodeReviewResult(
            Readability: ClampDimension(root.GetProperty("readability").GetInt32()),
            Functionality: ClampDimension(root.GetProperty("functionality").GetInt32()),
            DataValidation: ClampDimension(root.GetProperty("dataValidation").GetInt32()),
            UseCaseHandling: ClampDimension(root.GetProperty("useCaseHandling").GetInt32()),
            Syntax: ClampDimension(root.GetProperty("syntax").GetInt32()));
    }

    /// <summary>The 0-20 range is prompt-instructed, not schema-enforced (see the schema's remarks) — clamp defensively.</summary>
    private static int ClampDimension(int value) => Math.Clamp(value, 0, CodeReviewResult.MaxPerDimension);
}
