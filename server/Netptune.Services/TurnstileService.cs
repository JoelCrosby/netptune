using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;

using Netptune.Core.Extensions;
using Netptune.Core.Services;

namespace Netptune.Services;

public class TurnstileService : ITurnstileService
{
    private readonly IHttpClientFactory HttpClientFactory;
    private readonly string SecretKey;

    private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public TurnstileService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        HttpClientFactory = httpClientFactory;
        SecretKey = configuration.GetEnvironmentVariable("NETPTUNE_TURNSTILE_SECRET_KEY");
    }

    public async Task<bool> ValidateAsync(string? token, string? remoteIp = null)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(remoteIp))
        {
            return false;
        }

        var parameters = new Dictionary<string, string>
        {
            ["secret"] = SecretKey,
            ["response"] = token,
        };

        if (!string.IsNullOrEmpty(remoteIp))
        {
            parameters["remoteip"] = remoteIp;
        }

        using var client = HttpClientFactory.CreateClient();

        var response = await client.PostAsync(SiteVerifyUrl, new FormUrlEncodedContent(parameters));
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<TurnstileVerifyResponse>(content, JsonOptions);

        return result?.Success is true;
    }

    private sealed class TurnstileVerifyResponse
    {
        public bool Success { get; init; }

        [JsonPropertyName("error-codes")]
        public string[] ErrorCodes { get; init; } = [];
    }
}
