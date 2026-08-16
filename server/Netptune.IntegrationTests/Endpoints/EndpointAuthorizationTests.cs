using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

// The app relies on a fallback authorization policy, so an endpoint mapped without any
// authorization metadata is denied rather than served anonymously. These tests pin the
// set of endpoints that deliberately opt out, so widening it has to be a deliberate edit.
public sealed class EndpointAuthorizationTests
{
    private static readonly string[] ExpectedAnonymousRoutes =
    [
        "/api/auth/confirm-email",
        "/api/auth/github-login",
        "/api/auth/google-login",
        "/api/auth/login",
        "/api/auth/microsoft-login",
        "/api/auth/refresh",
        "/api/auth/register",
        "/api/auth/request-password-reset",
        "/api/auth/reset-password",
        "/api/auth/validate-workspace-invite",
        "/api/meta/build-info",
        "/api/public/workspaces/{workspaceKey}",
        "/api/public/workspaces/{workspaceKey}/members",
        "/health/deps",
        "/health/live",
        "/health/ready",
    ];

    private readonly NetptuneFixture Fixture;

    public EndpointAuthorizationTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public void Endpoints_ShouldOnlyAllowAnonymousAccess_ForTheExpectedRoutes()
    {
        var anonymousRoutes = RouteEndpoints()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText!)
            .Distinct()
            .Order()
            .ToList();

        anonymousRoutes.Should().BeEquivalentTo(ExpectedAnonymousRoutes);
    }

    [Fact]
    public async Task HealthEndpoints_ShouldRespond_WhenTheCallerIsAnonymous()
    {
        var client = Fixture.CreateAnonymousNetptuneClient("netptune");

        var live = await client.GetAsync("health/live", TestContext.Current.CancellationToken);
        var ready = await client.GetAsync("health/ready", TestContext.Current.CancellationToken);

        live.StatusCode.Should().Be(HttpStatusCode.OK);
        ready.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoints_ShouldReturnUnauthorized_WhenTheCallerIsAnonymous()
    {
        var client = Fixture.CreateAnonymousNetptuneClient("netptune");

        var response = await client.GetAsync("api/users", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private IEnumerable<RouteEndpoint> RouteEndpoints()
    {
        var dataSource = Fixture.Services.GetRequiredService<EndpointDataSource>();

        return dataSource.Endpoints.OfType<RouteEndpoint>();
    }
}
