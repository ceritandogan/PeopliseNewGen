using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Peoplise.Infrastructure.Identity;
using Xunit;

namespace Peoplise.Infrastructure.Tests.Identity;

public class HttpContextCurrentUserContextTests
{
    [Fact]
    public void Resolves_UserId_from_the_name_identifier_claim()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-42")], authenticationType: "Test")),
        };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var currentUser = new HttpContextCurrentUserContext(accessor);

        currentUser.UserId.Should().Be("user-42");
    }

    [Fact]
    public void Resolves_null_when_unauthenticated()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext());

        var currentUser = new HttpContextCurrentUserContext(accessor);

        currentUser.UserId.Should().BeNull();
    }
}
