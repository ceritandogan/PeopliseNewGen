namespace Peoplise.Infrastructure.Security;

/// <summary>Which kind of candidate-facing resource a token grants access to.</summary>
public enum CandidateResourceType
{
    Conversation,
    Case,
}

/// <summary>
/// Issues and validates the signed, stateless capability token that lets the candidate
/// app act on one specific conversation or case, replacing "knowing the GUID is enough"
/// on its write endpoints. Deliberately not OpenIddict, not a JWT — see ADR 0004 for why
/// this is a small, separate, hand-rolled token instead.
/// </summary>
public interface ICandidateResourceTokenService
{
    string Issue(CandidateResourceType resourceType, Guid resourceId, DateTimeOffset expiresAt);

    bool TryValidate(string? token, CandidateResourceType resourceType, Guid resourceId, out string error);
}
