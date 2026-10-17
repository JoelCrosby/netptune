using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.SourceControl.Connect;
using Netptune.SourceControl.Providers.GitHub;

using NSubstitute;

using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Netptune.UnitTests.Netptune.SourceControl.Support;

internal sealed class GitHubTestHarness : IDisposable
{
    public const string WebhookSecret = "test-webhook-secret";
    public const string AppId = "123456";
    public const string InstallationId = "987654";

    public static readonly string PrivateKeyPem = CreatePrivateKeyPem();

    public StubHttpMessageHandler Http { get; } = new();

    public AdjustableTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));

    public GitHubAppOptions Options { get; }

    public GitHubApi Api { get; }

    public GitHubAppTokenMinter TokenMinter { get; }

    public ScmConnectStateProtector StateProtector { get; }

    public GitHubConnector Connector { get; }

    public GitHubSourceControlClient Client { get; }

    public GitHubWebhookReader WebhookReader { get; }

    public GitHubSourceControlProvider Provider { get; }

    private readonly MemoryCache Cache = new(new MemoryCacheOptions());

    public GitHubTestHarness(Action<GitHubAppOptions>? configure = null)
    {
        Options = new GitHubAppOptions
        {
            Enabled = true,
            AppId = AppId,
            AppSlug = "netptune-test",
            ClientId = "Iv1.client",
            ClientSecret = "client-secret",
            PrivateKey = PrivateKeyPem,
            WebhookSecret = WebhookSecret,
        };

        configure?.Invoke(Options);

        var options = MicrosoftOptions.Create(Options);
        var clientFactory = Substitute.For<IHttpClientFactory>();

        clientFactory.CreateClient(GitHubApi.ClientName).Returns(_ => new HttpClient(Http, disposeHandler: false));

        Api = new GitHubApi(clientFactory, options, Clock);
        TokenMinter = new GitHubAppTokenMinter(options, Clock);
        StateProtector = new ScmConnectStateProtector(new EphemeralDataProtectionProvider());
        Connector = new GitHubConnector(Api, TokenMinter, StateProtector, Cache, options, Clock);
        Client = new GitHubSourceControlClient(Api);
        WebhookReader = new GitHubWebhookReader(options, Clock);
        Provider = new GitHubSourceControlProvider(Connector, Client, WebhookReader);
    }

    public static ScmConnection CreateConnection(SourceControlCapabilities granted = (SourceControlCapabilities)~0)
    {
        return new ScmConnection
        {
            Id = 7,
            WorkspaceId = 3,
            Provider = SourceControlProvider.GitHub,
            ExternalAccountId = InstallationId,
            Status = ScmConnectionStatus.Active,
            GrantedCapabilities = granted,
            CredentialKind = ScmCredentialKind.AppInstallation,
        };
    }

    public StubHttpMessageHandler StubInstallationToken(
        string token = "ghs_installation",
        DateTimeOffset? expiresAt = null)
    {
        var expiry = expiresAt ?? Clock.GetUtcNow().AddHours(1);
        var json = $$"""{ "token": "{{token}}", "expires_at": "{{expiry:O}}" }""";

        var tokenPath = $"/app/installations/{InstallationId}/access_tokens";

        return Http.On(HttpMethod.Post, tokenPath, System.Net.HttpStatusCode.Created, json);
    }

    public static ScmWebhookRequest CreateWebhookRequest(
        string eventName,
        string json,
        string deliveryId = "delivery-1",
        string? secret = WebhookSecret)
    {
        var body = Encoding.UTF8.GetBytes(json);

        var headers = new Dictionary<string, string>
        {
            ["X-GitHub-Event"] = eventName,
            ["X-GitHub-Delivery"] = deliveryId,
        };

        if (secret is not null)
        {
            var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);

            headers["X-Hub-Signature-256"] = $"sha256={Convert.ToHexStringLower(signature)}";
        }

        return new ScmWebhookRequest
        {
            Body = body,
            Headers = headers,
        };
    }

    public void Dispose()
    {
        TokenMinter.Dispose();
        Cache.Dispose();
        Http.Dispose();
    }

    private static string CreatePrivateKeyPem()
    {
        using var rsa = RSA.Create(2048);

        return rsa.ExportRSAPrivateKeyPem();
    }
}
