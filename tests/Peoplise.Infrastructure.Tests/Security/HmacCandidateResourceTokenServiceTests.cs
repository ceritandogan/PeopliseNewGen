using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Peoplise.Infrastructure.Security;
using Xunit;

namespace Peoplise.Infrastructure.Tests.Security;

public class HmacCandidateResourceTokenServiceTests
{
    private static HmacCandidateResourceTokenService CreateService(string? signingKey = "dGVzdC1zaWduaW5nLWtleS1mb3ItdW5pdC10ZXN0cw==")
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Auth:CandidateLinks:SigningKey"].Returns(signingKey);
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);

        return new HmacCandidateResourceTokenService(configuration, environment);
    }

    [Fact]
    public void A_freshly_issued_token_validates_for_the_resource_it_was_issued_for()
    {
        var service = CreateService();
        var resourceId = Guid.NewGuid();

        var token = service.Issue(CandidateResourceType.Conversation, resourceId, DateTimeOffset.UtcNow.AddDays(1));

        service.TryValidate(token, CandidateResourceType.Conversation, resourceId, out var error).Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public void A_token_does_not_validate_for_a_different_resource_id()
    {
        var service = CreateService();
        var token = service.Issue(CandidateResourceType.Conversation, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

        var isValid = service.TryValidate(token, CandidateResourceType.Conversation, Guid.NewGuid(), out var error);

        isValid.Should().BeFalse();
        error.Should().NotBeEmpty();
    }

    [Fact]
    public void A_token_does_not_validate_for_a_different_resource_type()
    {
        var service = CreateService();
        var resourceId = Guid.NewGuid();
        var token = service.Issue(CandidateResourceType.Conversation, resourceId, DateTimeOffset.UtcNow.AddDays(1));

        var isValid = service.TryValidate(token, CandidateResourceType.Case, resourceId, out _);

        isValid.Should().BeFalse("a conversation token must not grant access to a case, even with the same id");
    }

    [Fact]
    public void An_expired_token_does_not_validate()
    {
        var service = CreateService();
        var resourceId = Guid.NewGuid();
        var token = service.Issue(CandidateResourceType.Conversation, resourceId, DateTimeOffset.UtcNow.AddSeconds(-1));

        var isValid = service.TryValidate(token, CandidateResourceType.Conversation, resourceId, out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("expired");
    }

    [Fact]
    public void A_tampered_token_does_not_validate()
    {
        var service = CreateService();
        var resourceId = Guid.NewGuid();
        var token = service.Issue(CandidateResourceType.Conversation, resourceId, DateTimeOffset.UtcNow.AddDays(1));
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        service.TryValidate(tampered, CandidateResourceType.Conversation, resourceId, out _).Should().BeFalse();
    }

    [Fact]
    public void A_token_signed_with_a_different_key_does_not_validate()
    {
        var issuer = CreateService(signingKey: "a2V5LW9uZQ==");
        var verifier = CreateService(signingKey: "a2V5LXR3bw==");
        var resourceId = Guid.NewGuid();
        var token = issuer.Issue(CandidateResourceType.Conversation, resourceId, DateTimeOffset.UtcNow.AddDays(1));

        verifier.TryValidate(token, CandidateResourceType.Conversation, resourceId, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-real-token")]
    [InlineData("only.one.dot.too.many")]
    public void Missing_or_malformed_tokens_do_not_validate(string? malformedToken)
    {
        var service = CreateService();

        service.TryValidate(malformedToken, CandidateResourceType.Conversation, Guid.NewGuid(), out var error).Should().BeFalse();
        error.Should().NotBeEmpty();
    }

    [Fact]
    public void Falls_back_to_an_ephemeral_key_in_Development_when_none_is_configured()
    {
        var service = CreateService(signingKey: null);
        var resourceId = Guid.NewGuid();

        var token = service.Issue(CandidateResourceType.Case, resourceId, DateTimeOffset.UtcNow.AddDays(1));

        service.TryValidate(token, CandidateResourceType.Case, resourceId, out _).Should().BeTrue();
    }

    [Fact]
    public void Refuses_to_construct_outside_Development_with_no_key_configured()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Auth:CandidateLinks:SigningKey"].Returns((string?)null);
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);

        var act = () => new HmacCandidateResourceTokenService(configuration, environment);

        act.Should().Throw<InvalidOperationException>();
    }
}
