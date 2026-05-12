using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;

using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class AuthExternalProviderEndpointTests
{
    private readonly HttpClient Client;

    public AuthExternalProviderEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        Client.DefaultRequestHeaders.Remove("workspace");
    }

    [Theory]
    [InlineData("api/auth/github-login", "github.com", "/test-github-callback")]
    [InlineData("api/auth/google-login", "accounts.google.com", "/test-google-callback")]
    [InlineData("api/auth/microsoft-login", "login.microsoftonline.com", "/test-microsoft-callback")]
    public async Task ProviderLogin_ShouldChallengeExternalProvider(
        string route,
        string expectedHost,
        string expectedCallbackPath)
    {
        var response = await Client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var location = response.Headers.Location;
        location.Should().NotBeNull();
        location!.Host.Should().Contain(expectedHost);

        var query = QueryHelpers.ParseQuery(location.Query);
        query["client_id"].Should().ContainSingle("test");
        query["redirect_uri"].ToString().Should().Contain(expectedCallbackPath);
        query["state"].ToString().Should().NotBeNullOrWhiteSpace();

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(cookie => cookie.Contains(".AspNetCore.Correlation", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("api/auth/github-login-complete", "GitHub")]
    [InlineData("api/auth/google-login-complete", "Google")]
    [InlineData("api/auth/microsoft-login-complete", "Microsoft")]
    [InlineData("api/auth/github-login-redirect", "GitHub")]
    [InlineData("api/auth/google-login-redirect", "Google")]
    [InlineData("api/auth/microsoft-login-redirect", "Microsoft")]
    public async Task ProviderLoginRedirect_ShouldCreateUserSetCookiesAndRedirectToClient(
        string route,
        string providerName)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{providerName.ToLowerInvariant()}-{unique}@example.com";
        var displayName = $"{providerName}{unique} External";
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Add("x-test-auth-email", email);
        request.Headers.Add("x-test-auth-provider-key", $"{providerName.ToLowerInvariant()}-{unique}");
        request.Headers.Add("x-test-auth-name", displayName);
        request.Headers.Add("x-test-auth-picture", $"https://example.com/{unique}.png");

        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, content);

        var location = response.Headers.Location;
        location.Should().NotBeNull();
        location!.GetLeftPart(UriPartial.Path).Should().Be("http://localhost:6400/auth/auth-provider-login");

        var query = QueryHelpers.ParseQuery(location.Query);
        query["displayName"].ToString().Should().Be(displayName);
        query["email"].ToString().Should().Be(email);
        query["pictureUrl"].ToString().Should().Be($"https://example.com/{unique}.png");
        query["userId"].ToString().Should().NotBeNullOrWhiteSpace();
        query["expires"].ToString().Should().NotBeNullOrWhiteSpace();

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(cookie => cookie.StartsWith("access_token=", StringComparison.Ordinal));
        cookies.Should().Contain(cookie => cookie.StartsWith("refresh_token=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProviderLoginRedirect_ShouldLinkProviderAndRedirect_WhenEmailBelongsToExistingUser()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/github-login-complete");
        request.Headers.Add("x-test-auth-email", SeedData.Users.First().Email);
        request.Headers.Add("x-test-auth-provider-key", $"github-conflict-{Guid.NewGuid():N}");
        request.Headers.Add("x-test-auth-name", "Conflicting User");

        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, content);

        var location = response.Headers.Location;
        location.Should().NotBeNull();
        location!.GetLeftPart(UriPartial.Path).Should().Be("http://localhost:6400/auth/auth-provider-login");

        var query = QueryHelpers.ParseQuery(location.Query);
        query["email"].ToString().Should().Be(SeedData.Users.First().Email);

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(cookie => cookie.StartsWith("access_token=", StringComparison.Ordinal));
        cookies.Should().Contain(cookie => cookie.StartsWith("refresh_token=", StringComparison.Ordinal));
    }
}
