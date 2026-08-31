using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authentication.Models;
using Netptune.Core.Entities;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

// Login and registration are rate limited per remote address, so every test here talks to the API
// through its own forwarded IP and therefore its own rate limit partition.
public sealed class AuthEndpointTests
{
    private const string Password = "Integration-Password-1!";

    private readonly NetptuneFixture Fixture;

    public AuthEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Register_ShouldReturnTheAuthenticatedUser_AndSetAuthCookies()
    {
        var client = CreateClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("api/auth/register", NewRegisterRequest(email));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        result!.Email.Should().Be(email);
        result.UserId.Should().NotBeNullOrWhiteSpace();

        SetCookieNames(response).Should().Contain(["access_token", "refresh_token"]);
    }

    [Fact]
    public async Task Register_ShouldReturnUnauthorized_WhenEmailAlreadyExists()
    {
        var client = CreateClient();
        var email = NewEmail();

        (await client.PostAsJsonAsync("api/auth/register", NewRegisterRequest(email))).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("api/auth/register", NewRegisterRequest(email));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ShouldReturnUnauthorized_WhenTurnstileTokenIsRejected()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Email = NewEmail(),
            Password = Password,
            Firstname = "Auth",
            Lastname = "Tester",
            Turnstile = "not-a-valid-token",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturnTheAuthenticatedUser_WhenCredentialsAreValid()
    {
        var client = CreateClient();
        var email = await Register(client);

        var response = await client.PostAsJsonAsync("api/auth/login", new TokenRequest
        {
            Email = email,
            Password = Password,
            Turnstile = TestTurnstileService.ValidToken,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        result!.Email.Should().Be(email);

        SetCookieNames(response).Should().Contain(["access_token", "refresh_token"]);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        var client = CreateClient();
        var email = await Register(client);

        var response = await client.PostAsJsonAsync("api/auth/login", new TokenRequest
        {
            Email = email,
            Password = "the-wrong-password",
            Turnstile = TestTurnstileService.ValidToken,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenTurnstileTokenIsRejected()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/login", new TokenRequest
        {
            Email = "someone@netptune.test",
            Password = Password,
            Turnstile = "not-a-valid-token",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldIssueNewCookies_WhenTheRefreshCookieIsPresent()
    {
        var client = CreateClient();
        var email = await Register(client);
        var login = await client.PostAsJsonAsync("api/auth/login", new TokenRequest
        {
            Email = email,
            Password = Password,
            Turnstile = TestTurnstileService.ValidToken,
        });

        var refreshToken = GetCookieValue(login, "refresh_token");

        var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh");

        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        result!.Email.Should().Be(email);

        SetCookieNames(response).Should().Contain("access_token");
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenTheRefreshCookieIsMissing()
    {
        var client = CreateClient();

        var response = await client.PostAsync("api/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldClearTheAuthCookies()
    {
        var client = CreateClient();

        var response = await client.PostAsync("api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        SetCookieNames(response).Should().Contain(["access_token", "refresh_token"]);
    }

    [Fact]
    public async Task ConfirmEmail_ShouldSignTheUserIn_WhenTheCodeIsValid()
    {
        var client = CreateClient();
        var email = await Register(client);
        var user = await GetUser(email);
        var code = await GenerateToken(user.Id, (manager, tracked) => manager.GenerateEmailConfirmationTokenAsync(tracked));

        var response = await client.PostAsJsonAsync("api/auth/confirm-email", new AuthCodeRequest
        {
            userId = user.Id,
            code = code,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        result!.Email.Should().Be(email);
    }

    [Fact]
    public async Task ConfirmEmail_ShouldReturnUnauthorized_WhenTheCodeIsMissing()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/confirm-email", new AuthCodeRequest
        {
            userId = string.Empty,
            code = string.Empty,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestPasswordReset_ShouldReturnCorrectly_WhenTheAccountExists()
    {
        var client = CreateClient();
        var email = await Register(client);

        var response = await client.GetAsync(
            $"api/auth/request-password-reset?email={Uri.EscapeDataString(email)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RequestPasswordReset_ShouldFail_WhenTheAccountDoesNotExist()
    {
        var client = CreateClient();

        var response = await client.GetAsync("api/auth/request-password-reset?email=nobody@netptune.test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPassword_ShouldReplaceThePassword_WhenTheCodeIsValid()
    {
        var client = CreateClient();
        var email = await Register(client);
        var user = await GetUser(email);
        var code = await GenerateToken(user.Id, (manager, tracked) => manager.GeneratePasswordResetTokenAsync(tracked));
        var newPassword = "Reset-Password-2!";

        var response = await client.PostAsJsonAsync("api/auth/reset-password", new ResetPasswordRequest
        {
            UserId = user.Id,
            Code = code,
            Password = newPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var login = await client.PostAsJsonAsync("api/auth/login", new TokenRequest
        {
            Email = email,
            Password = newPassword,
            Turnstile = TestTurnstileService.ValidToken,
        });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnUnauthorized_WhenTheCodeIsInvalid()
    {
        var client = CreateClient();
        var email = await Register(client);
        var user = await GetUser(email);

        var response = await client.PostAsJsonAsync("api/auth/reset-password", new ResetPasswordRequest
        {
            UserId = user.Id,
            Code = "not-a-real-reset-code",
            Password = "Reset-Password-3!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_ShouldReplaceThePassword_WhenTheCurrentPasswordMatches()
    {
        var client = CreateClient();
        var email = await Register(client);
        var user = await GetUser(email);
        var newPassword = "Changed-Password-4!";

        var response = await client.PatchAsJsonAsync("api/auth/change-password", new ChangePasswordRequest
        {
            UserId = user.Id,
            CurrentPassword = Password,
            NewPassword = newPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var login = await client.PostAsJsonAsync("api/auth/login", new TokenRequest
        {
            Email = email,
            Password = newPassword,
            Turnstile = TestTurnstileService.ValidToken,
        });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnUnauthorized_WhenTheCurrentPasswordIsWrong()
    {
        var client = CreateClient();
        var email = await Register(client);
        var user = await GetUser(email);

        var response = await client.PatchAsJsonAsync("api/auth/change-password", new ChangePasswordRequest
        {
            UserId = user.Id,
            CurrentPassword = "the-wrong-password",
            NewPassword = "Changed-Password-5!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateWorkspaceInvite_ShouldReturnTheInvite_WhenTheCodeIsPending()
    {
        var client = CreateClient();
        var email = NewEmail();

        (await client.PostAsJsonAsync("api/users/invite", new InviteUsersRequest
        {
            EmailAddresses = [email],
        })).EnsureSuccessStatusCode();

        var code = await GetInviteCode(email);

        var response = await client.GetAsync($"api/auth/validate-workspace-invite?code={Uri.EscapeDataString(code)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<WorkspaceInvite>();

        result!.Email.Should().Be(email);
        result.Code.Should().Be(code);
    }

    [Fact]
    public async Task ValidateWorkspaceInvite_ShouldReturnUnauthorized_WhenTheCodeIsUnknown()
    {
        var client = CreateClient();

        var response = await client.GetAsync("api/auth/validate-workspace-invite?code=not-a-real-invite-code");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient()
    {
        var client = Fixture.CreateNetptuneClient();

        // Each test gets its own forwarded address so the auth and register rate limits, which
        // partition on the remote IP, cannot leak between tests.
        client.DefaultRequestHeaders.Add("X-Forwarded-For", NewIpAddress());

        return client;
    }

    private static string NewIpAddress()
    {
        var bytes = Guid.NewGuid().ToByteArray();

        return $"10.{bytes[0]}.{bytes[1]}.{bytes[2]}";
    }

    private static string NewEmail() => $"auth-{Guid.NewGuid():N}@netptune.test";

    private static RegisterRequest NewRegisterRequest(string email) => new()
    {
        Email = email,
        Password = Password,
        Firstname = "Auth",
        Lastname = "Tester",
        Turnstile = TestTurnstileService.ValidToken,
    };

    private static async Task<string> Register(HttpClient client)
    {
        var email = NewEmail();
        var response = await client.PostAsJsonAsync("api/auth/register", NewRegisterRequest(email));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return email;
    }

    private async Task<AppUser> GetUser(string email)
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Users.SingleAsync(user => user.Email == email);
    }

    private async Task<string> GetInviteCode(string email)
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.WorkspaceInvites
            .Where(invite => invite.Email == email && invite.AcceptedAt == null)
            .Select(invite => invite.Code)
            .SingleAsync();
    }

    private async Task<string> GenerateToken(string userId, Func<UserManager<AppUser>, AppUser, Task<string>> generate)
    {
        using var scope = Fixture.CreateScope();

        var manager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await manager.FindByIdAsync(userId);

        return await generate(manager, user!);
    }

    private static IReadOnlyList<string> SetCookieNames(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return [];
        }

        return cookies.Select(cookie => cookie.Split('=', 2)[0]).ToList();
    }

    private static string GetCookieValue(HttpResponseMessage response, string name)
    {
        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(item => item.StartsWith($"{name}=", StringComparison.Ordinal));

        return cookie[(name.Length + 1)..].Split(';', 2)[0];
    }
}
