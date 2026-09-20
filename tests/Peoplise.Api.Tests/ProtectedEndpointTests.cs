using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Peoplise.Api.Tests;

public class ProtectedEndpointTests
{
    [Fact]
    public async Task A_protected_endpoint_rejects_a_request_with_no_authentication()
    {
        await using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/cases/{Guid.NewGuid()}/withdraw-consent", new { reason = "test" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_protected_endpoint_accepts_the_test_identity_and_reaches_the_real_handler()
    {
        await using var factory = new ApiTestFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantIdHeader, Guid.NewGuid().ToString());

        // Proves the whole pipeline (TestAuthHandler -> [Authorize] -> tenant claim ->
        // MediatR -> the real RequestDataDeletionCommandHandler) actually runs, by
        // asserting on the handler's own business-logic response (not-found for a
        // case that doesn't exist) rather than an auth failure.
        var response = await client.PostAsJsonAsync($"/api/cases/{Guid.NewGuid()}/withdraw-consent", new { reason = "test" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Case.NotFound");
    }
}
