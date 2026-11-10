using System.Net;
using System.Text;

using Microsoft.AspNetCore.DataProtection;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.SourceControl.Credentials;
using Netptune.SourceControl.Providers.AzureDevOps;

using NSubstitute;

using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Netptune.UnitTests.Netptune.SourceControl.Support;

internal sealed class AzureDevOpsTestHarness : IDisposable
{
    public const string Organisation = "acme";
    public const string InstanceId = "6f3e9a52-2c1d-4c47-9a63-5e1bba1d2f10";
    public const string Token = "pat-secret-value-wxyz";

    public const string ConnectionDataPath = "/acme/_apis/connectionData";

    public const string AdministratorPath =
        "/acme/_apis/permissions/3e65f728-f8bc-4ecd-8764-7e378b19bfa7/2?tokens=NAMESPACE:&api-version=7.1";

    public const string RepositoriesPath = "/acme/_apis/git/repositories?api-version=7.1";

    public StubHttpMessageHandler Http { get; } = new();

    public AdjustableTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));

    public ScmCredentialProtector CredentialProtector { get; } = new(new EphemeralDataProtectionProvider());

    public AzureDevOpsApi Api { get; }

    public AzureDevOpsConnector Connector { get; }

    public AzureDevOpsSourceControlClient Client { get; }

    public AzureDevOpsSourceControlProvider Provider { get; }

    public AzureDevOpsTestHarness()
    {
        var options = MicrosoftOptions.Create(new AzureDevOpsOptions { Enabled = true });
        var clientFactory = Substitute.For<IHttpClientFactory>();

        clientFactory.CreateClient(AzureDevOpsApi.ClientName).Returns(_ => new HttpClient(Http, disposeHandler: false));

        Api = new AzureDevOpsApi(clientFactory, options, Clock);
        Connector = new AzureDevOpsConnector(Api, CredentialProtector, Clock);
        Client = new AzureDevOpsSourceControlClient(Api);
        Provider = new AzureDevOpsSourceControlProvider(Connector, Client);
    }

    public static string BasicCredential(string token = Token)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{token}"));

        return $"Basic {encoded}";
    }

    public static ScmConnectCallback CreateCallback(
        string organisation = Organisation,
        string token = Token,
        string? expiresOn = null)
    {
        var parameters = new Dictionary<string, string>
        {
            ["organisation"] = organisation,
            ["token"] = token,
        };

        if (expiresOn is not null)
        {
            parameters["expiresOn"] = expiresOn;
        }

        return new ScmConnectCallback
        {
            WorkspaceId = 3,
            UserId = "user-1",
            Parameters = parameters,
        };
    }

    public ScmConnection CreateConnection(DateTime? credentialExpiresAt = null)
    {
        return new ScmConnection
        {
            Id = 9,
            WorkspaceId = 3,
            Provider = SourceControlProvider.AzureDevOps,
            ExternalAccountId = InstanceId,
            AccountLogin = Organisation,
            Status = ScmConnectionStatus.Active,
            GrantedCapabilities = SourceControlCapabilities.ListRepositories,
            CredentialKind = ScmCredentialKind.PersonalAccessToken,
            ProtectedCredential = CredentialProtector.Protect(Token),
            CredentialExpiresAt = credentialExpiresAt,
        };
    }

    public StubHttpMessageHandler StubConnectionData(string instanceId = InstanceId)
    {
        var json = $$"""{ "authenticatedUser": { "id": "user-guid" }, "instanceId": "{{instanceId}}" }""";

        return Http.On(HttpMethod.Get, ConnectionDataPath, HttpStatusCode.OK, json);
    }

    public StubHttpMessageHandler StubAdministrator(bool isAdministrator)
    {
        var answer = isAdministrator ? "true" : "false";
        var json = $$"""{ "count": 1, "value": [{{answer}}] }""";

        return Http.On(HttpMethod.Get, AdministratorPath, HttpStatusCode.OK, json);
    }

    public static void RespondWithSignInPage(HttpResponseMessage response)
    {
        response.Content = new StringContent("<html><body>Sign in</body></html>", Encoding.UTF8, "text/html");
    }

    public void Dispose()
    {
        Http.Dispose();
    }
}
