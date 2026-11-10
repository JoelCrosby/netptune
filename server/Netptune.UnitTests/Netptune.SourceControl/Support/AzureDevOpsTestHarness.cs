using System.Net;
using System.Text;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

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

    public const string AdministratorFallbackPath =
        "/acme/_apis/permissions/3e65f728-f8bc-4ecd-8764-7e378b19bfa7/2?tokens=NAMESPACE&api-version=7.1";

    public const string ProjectsPath = "/acme/_apis/projects?$top=1&api-version=7.1";

    public const string ProjectsJson = """
        { "count": 1, "value": [ { "id": "project-1", "name": "Platform" } ] }
        """;

    public const string RepositoriesPath = "/acme/_apis/git/repositories?api-version=7.1";

    public const string WorkItemsProbePath = "/acme/_apis/wit/fields/System.Title?api-version=7.1";

    public const string ProjectId = "project-1";

    public const string BuildsProbePath = "/acme/project-1/_apis/build/definitions?$top=1&api-version=7.1";

    public const string UserDisplayName = "Joel Crosby";

    public const string RepositoriesJson = """
        {
          "count": 4,
          "value": [
            { "id": "r1", "name": "api", "isDisabled": false, "project": { "id": "project-1", "name": "Platform" } },
            { "id": "r2", "name": "web", "isDisabled": false, "project": { "id": "project-1", "name": "Platform" } },
            { "id": "r3", "name": "docs", "isDisabled": false, "project": { "id": "project-2", "name": "Docs" } },
            { "id": "r4", "name": "old", "isDisabled": true, "project": { "id": "project-2", "name": "Docs" } }
          ]
        }
        """;

    public StubHttpMessageHandler Http { get; } = new();

    public AdjustableTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));

    public ScmCredentialProtector CredentialProtector { get; } = new(new EphemeralDataProtectionProvider());

    public AzureDevOpsApi Api { get; }

    public AzureDevOpsConnectionChecker Checker { get; }

    public AzureDevOpsConnector Connector { get; }

    public AzureDevOpsSourceControlClient Client { get; }

    public AzureDevOpsSourceControlProvider Provider { get; }

    public AzureDevOpsTestHarness()
    {
        var options = MicrosoftOptions.Create(new AzureDevOpsOptions { Enabled = true });
        var clientFactory = Substitute.For<IHttpClientFactory>();

        clientFactory.CreateClient(AzureDevOpsApi.ClientName).Returns(_ => new HttpClient(Http, disposeHandler: false));

        Api = new AzureDevOpsApi(clientFactory, options, Clock);
        Checker = new AzureDevOpsConnectionChecker(
            Api,
            CredentialProtector,
            NullLogger<AzureDevOpsConnectionChecker>.Instance);
        Connector = new AzureDevOpsConnector(Checker, CredentialProtector, Clock);
        Client = new AzureDevOpsSourceControlClient(Api);
        Provider = new AzureDevOpsSourceControlProvider(Connector, Client, Checker);
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
        var json = $$"""
            {
              "authenticatedUser": { "id": "user-guid", "providerDisplayName": "{{UserDisplayName}}" },
              "instanceId": "{{instanceId}}"
            }
            """;

        return Http.On(HttpMethod.Get, ConnectionDataPath, HttpStatusCode.OK, json);
    }

    public StubHttpMessageHandler StubAdministrator(bool isAdministrator)
    {
        StubAdministratorAnswer(AdministratorPath, isAdministrator);

        return StubAdministratorAnswer(AdministratorFallbackPath, isAdministrator);
    }

    public StubHttpMessageHandler StubAdministratorAnswer(string path, bool isAdministrator)
    {
        var answer = isAdministrator ? "true" : "false";
        var json = $$"""{ "count": 1, "value": [{{answer}}] }""";

        return Http.On(HttpMethod.Get, path, HttpStatusCode.OK, json);
    }

    public StubHttpMessageHandler StubPassingChecks()
    {
        StubConnectionData();
        StubAdministrator(isAdministrator: true);
        Http.On(HttpMethod.Get, ProjectsPath, HttpStatusCode.OK, ProjectsJson);
        Http.On(HttpMethod.Get, RepositoriesPath, HttpStatusCode.OK, RepositoriesJson);
        Http.On(HttpMethod.Get, WorkItemsProbePath, HttpStatusCode.OK, """{ "referenceName": "System.Title" }""");

        return Http.On(HttpMethod.Get, BuildsProbePath, HttpStatusCode.OK, """{ "count": 0, "value": [] }""");
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
