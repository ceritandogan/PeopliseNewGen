using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Peoplise.Infrastructure.Identity;
using Peoplise.SharedKernel.MultiTenancy;
using Xunit;

namespace Peoplise.Infrastructure.Tests.Identity;

public class HttpContextTenantContextTests
{
    [Fact]
    public void Resolves_the_TenantId_from_the_tenant_id_claim()
    {
        var tenantGuid = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            User = PrincipalWithClaim(HttpContextTenantContext.TenantClaimType, tenantGuid.ToString()),
        };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var tenantContext = new HttpContextTenantContext(accessor);

        tenantContext.TenantId.Should().Be(TenantId.From(tenantGuid));
    }

    [Fact]
    public void Resolves_null_when_the_tenant_id_claim_is_missing()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext());

        var tenantContext = new HttpContextTenantContext(accessor);

        tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public void Resolves_null_when_there_is_no_HttpContext_at_all()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var tenantContext = new HttpContextTenantContext(accessor);

        tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public void Resolves_null_when_the_claim_value_is_not_a_valid_guid()
    {
        var httpContext = new DefaultHttpContext
        {
            User = PrincipalWithClaim(HttpContextTenantContext.TenantClaimType, "not-a-guid"),
        };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var tenantContext = new HttpContextTenantContext(accessor);

        tenantContext.TenantId.Should().BeNull();
    }

    private static ClaimsPrincipal PrincipalWithClaim(string type, string value) =>
        new(new ClaimsIdentity([new Claim(type, value)], authenticationType: "Test"));
}
