using Netptune.Core.Services;

namespace Netptune.IntegrationTests.TestServices;

public sealed class TestTurnstileService : ITurnstileService
{
    public const string ValidToken = "test-turnstile-token";

    public Task<bool> ValidateAsync(string? token, string? remoteIp = null)
    {
        return Task.FromResult(token == ValidToken);
    }
}
