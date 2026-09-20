using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Peoplise.Api.Tests;

public class AuthorizeEndpointTests
{
    [Fact]
    public async Task Visiting_connect_authorize_unauthenticated_redirects_to_the_login_page()
    {
        await using var factory = new ApiTestFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(
            "/connect/authorize?client_id=peoplise-panel&redirect_uri=http://localhost:5173/auth/callback"
            + "&response_type=code&scope=openid&code_challenge=x&code_challenge_method=S256&state=abc");

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Redirect, because: body);
        response.Headers.Location!.OriginalString.Should().Contain("/connect/login");
    }
}
