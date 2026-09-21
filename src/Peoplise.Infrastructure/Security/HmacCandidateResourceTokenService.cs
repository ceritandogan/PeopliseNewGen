using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Peoplise.Infrastructure.Security;

/// <summary>
/// A minimal <c>base64url(payload) + "." + base64url(HMAC-SHA256)</c> token — not a JWT,
/// no issuer/audience/claims machinery, because none of that is needed for "prove you
/// hold the capability for exactly this one resource." Its own signing key, config-driven
/// like <c>Auth:Certificates:*</c>, deliberately separate from OpenIddict's. See ADR 0004.
/// </summary>
public sealed class HmacCandidateResourceTokenService : ICandidateResourceTokenService
{
    private readonly byte[] _key;

    public HmacCandidateResourceTokenService(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredKey = configuration["Auth:CandidateLinks:SigningKey"];
        if (!string.IsNullOrEmpty(configuredKey))
        {
            _key = Convert.FromBase64String(configuredKey);
            return;
        }

        // Same trade-off this codebase already accepts for OpenIddict's development
        // certificates (see AddAuth): an ephemeral key regenerated every process start
        // invalidates every link issued before a restart, which is fine for local dev
        // and the WebApplicationFactory test host, and refused everywhere else.
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            _key = RandomNumberGenerator.GetBytes(32);
            return;
        }

        throw new InvalidOperationException("Auth:CandidateLinks:SigningKey must be configured outside Development.");
    }

    public string Issue(CandidateResourceType resourceType, Guid resourceId, DateTimeOffset expiresAt)
    {
        var payloadSegment = EncodePayload(new TokenPayload(resourceType, resourceId, expiresAt.ToUnixTimeSeconds()));
        var signature = Base64UrlEncode(ComputeSignature(payloadSegment));
        return $"{payloadSegment}.{signature}";
    }

    public bool TryValidate(string? token, CandidateResourceType resourceType, Guid resourceId, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrEmpty(token))
        {
            error = "Missing candidate access token.";
            return false;
        }

        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            error = "Malformed candidate access token.";
            return false;
        }

        byte[] expectedSignature;
        byte[] actualSignature;
        try
        {
            expectedSignature = ComputeSignature(parts[0]);
            actualSignature = Base64UrlDecode(parts[1]);
        }
        catch (FormatException)
        {
            error = "Malformed candidate access token.";
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature))
        {
            error = "Invalid candidate access token.";
            return false;
        }

        TokenPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TokenPayload>(Base64UrlDecode(parts[0]));
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            error = "Malformed candidate access token.";
            return false;
        }

        if (payload is null || payload.ResourceType != resourceType || payload.ResourceId != resourceId)
        {
            error = "This token does not grant access to this resource.";
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.Exp)
        {
            error = "This candidate access token has expired.";
            return false;
        }

        return true;
    }

    private byte[] ComputeSignature(string payloadSegment)
    {
        using var hmac = new HMACSHA256(_key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadSegment));
    }

    private static string EncodePayload(TokenPayload payload) =>
        Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(padded);
    }

    private sealed record TokenPayload(CandidateResourceType ResourceType, Guid ResourceId, long Exp);
}
