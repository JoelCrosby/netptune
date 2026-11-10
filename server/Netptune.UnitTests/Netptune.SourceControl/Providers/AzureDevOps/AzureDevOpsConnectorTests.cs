using System.Net;

using FluentAssertions;

using Netptune.Core.Localization;
using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Checks;
using Netptune.Core.SourceControl.Connect;
using Netptune.SourceControl.Providers.AzureDevOps;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Providers.AzureDevOps;

public sealed class AzureDevOpsConnectorTests : IDisposable
{
    private readonly AzureDevOpsTestHarness Harness = new();

    [Theory]
    [InlineData("acme")]
    [InlineData("https://dev.azure.com/acme/Platform")]
    [InlineData("acme.visualstudio.com")]
    [InlineData("https://acme.visualstudio.com")]
    public async Task CompleteConnect_ShouldStoreTheBareOrganisationName(string organisation)
    {
        Harness.StubPassingChecks();

        var callback = AzureDevOpsTestHarness.CreateCallback(organisation);
        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.Connection!.Account.Login.Should().Be(AzureDevOpsTestHarness.Organisation);
        Harness.Http.Requests.Should().OnlyContain(request => request.Uri.Host == "dev.azure.com");
    }

    [Fact]
    public async Task CompleteConnect_ShouldRejectAnOrganisationItCannotRecognise()
    {
        var callback = AzureDevOpsTestHarness.CreateCallback("https://github.com/acme");
        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(AppText.ErrorAzureDevOpsOrganisationNotRecognised);
        Harness.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteConnect_ShouldRequireAToken()
    {
        var callback = AzureDevOpsTestHarness.CreateCallback(token: "  ");
        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.Message.Should().Be(AppText.ErrorAzureDevOpsTokenRequired);
        Harness.Http.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("10/12/2026")]
    [InlineData("2026-11-08")]
    [InlineData("soon")]
    public async Task CompleteConnect_ShouldRejectAnExpiryThatIsInvalidOrPassed(string expiresOn)
    {
        var callback = AzureDevOpsTestHarness.CreateCallback(expiresOn: expiresOn);
        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.Message.Should().Be(AppText.ErrorAzureDevOpsExpiryInvalid);
        Harness.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteConnect_ShouldValidateTheTokenWithBasicAuthAndAnEmptyUserName()
    {
        Harness.StubPassingChecks();

        await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        var connectionData = Harness.Http.Requests.First();

        connectionData.Uri.AbsoluteUri.Should().Be($"https://dev.azure.com{AzureDevOpsTestHarness.ConnectionDataPath}");
        connectionData.Authorization.Should().Be(AzureDevOpsTestHarness.BasicCredential());
    }

    [Fact]
    public async Task CompleteConnect_ShouldSayTheTokenWasRejectedOnA401()
    {
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.ConnectionDataPath, HttpStatusCode.Unauthorized);

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        ShouldBeRefusedBy(result, AzureDevOpsConnectionChecker.TokenKey);
    }

    [Fact]
    public async Task CompleteConnect_ShouldSayTheTokenWasRejectedWhenAzureDevOpsAnswersWithItsSignInPage()
    {
        Harness.Http.On(
            HttpMethod.Get,
            AzureDevOpsTestHarness.ConnectionDataPath,
            HttpStatusCode.NonAuthoritativeInformation,
            configure: AzureDevOpsTestHarness.RespondWithSignInPage);

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        ShouldBeRefusedBy(result, AzureDevOpsConnectionChecker.TokenKey);
    }

    [Fact]
    public async Task CompleteConnect_ShouldSayTheTokenWasRejectedWhenASuccessIsNotJson()
    {
        Harness.Http.On(
            HttpMethod.Get,
            AzureDevOpsTestHarness.ConnectionDataPath,
            HttpStatusCode.OK,
            configure: AzureDevOpsTestHarness.RespondWithSignInPage);

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        ShouldBeRefusedBy(result, AzureDevOpsConnectionChecker.TokenKey);
    }

    [Fact]
    public async Task CompleteConnect_ShouldSayTheOrganisationWasNotFoundOnA404()
    {
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.ConnectionDataPath, HttpStatusCode.NotFound);

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        ShouldBeRefusedBy(result, AzureDevOpsConnectionChecker.OrganisationKey);
    }

    [Fact]
    public async Task CompleteConnect_ShouldStoreTheInstanceIdTheEncryptedTokenItsHintAndExpiry()
    {
        Harness.StubPassingChecks();

        var callback = AzureDevOpsTestHarness.CreateCallback(expiresOn: "2026-12-31");
        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        var connection = result.Payload!.Connection!;
        var storedToken = Harness.CredentialProtector.Unprotect(connection.ProtectedCredential!);

        connection.ExternalAccountId.Should().Be(AzureDevOpsTestHarness.InstanceId);
        connection.Account.ExternalId.Should().Be(AzureDevOpsTestHarness.InstanceId);
        connection.Account.AccountType.Should().Be(ScmAccountType.Organisation);
        connection.CredentialKind.Should().Be(ScmCredentialKind.PersonalAccessToken);
        connection.CredentialHint.Should().Be("wxyz");
        connection.CredentialExpiresAt.Should().Be(new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc));
        connection.CredentialExpiresAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        connection.GrantedCapabilities.Should().Be(SourceControlCapabilities.ListRepositories);
        connection.ProtectedCredential.Should().NotEqual(System.Text.Encoding.UTF8.GetBytes(AzureDevOpsTestHarness.Token));
        storedToken.Should().Be(AzureDevOpsTestHarness.Token);
    }

    [Fact]
    public async Task CompleteConnect_ShouldAcceptAnExpiryOfToday()
    {
        Harness.StubPassingChecks();

        var callback = AzureDevOpsTestHarness.CreateCallback(expiresOn: "2026-11-10");
        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.Payload!.Connection!.CredentialExpiresAt.Should().Be(new DateTime(2026, 11, 10, 23, 59, 59, DateTimeKind.Utc));
    }

    [Fact]
    public async Task CompleteConnect_ShouldAcceptYesterdaysUtcDateForUsersBehindUtc()
    {
        Harness.StubPassingChecks();

        var callback = AzureDevOpsTestHarness.CreateCallback(expiresOn: "2026-11-09");
        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.Payload!.Connection!.CredentialExpiresAt.Should().Be(new DateTime(2026, 11, 9, 23, 59, 59, DateTimeKind.Utc));
    }

    [Fact]
    public async Task CompleteConnect_ShouldLeaveTheExpiryEmptyWhenNoneIsGiven()
    {
        Harness.StubPassingChecks();

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        result.Payload!.Connection!.CredentialExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task CompleteConnect_ShouldAskTheCollectionSecurityNamespaceWhetherTheUserIsAnAdministrator()
    {
        Harness.StubPassingChecks();

        await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        var permissionCheck = Harness.Http.Requests.First(request => request.Uri.AbsolutePath.Contains("permissions"));

        permissionCheck.Uri.PathAndQuery.Should().Be(AzureDevOpsTestHarness.AdministratorPath);
        permissionCheck.Authorization.Should().Be(AzureDevOpsTestHarness.BasicCredential());
    }

    [Fact]
    public async Task CompleteConnect_ShouldRefuseATokenThatIsNotAnAdministrators()
    {
        Harness.StubPassingChecks();
        Harness.StubAdministrator(isAdministrator: false);

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        var administrator = ShouldBeRefusedBy(result, AzureDevOpsConnectionChecker.AdministratorKey);

        administrator.Facts[AzureDevOpsConnectionChecker.ReasonFact].Should().Be(AzureDevOpsConnectionChecker.DeniedReason);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.NonAuthoritativeInformation)]
    public async Task CompleteConnect_ShouldRefuseWhenThePermissionCannotBeRead(HttpStatusCode status)
    {
        Harness.StubPassingChecks();
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.AdministratorPath, status);
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.AdministratorFallbackPath, status);

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        ShouldBeRefusedAsAdministratorUnavailable(result);
    }

    [Fact]
    public async Task CompleteConnect_ShouldRefuseWhenThePermissionAnswerIsEmpty()
    {
        Harness.StubPassingChecks();

        foreach (var path in new[] { AzureDevOpsTestHarness.AdministratorPath, AzureDevOpsTestHarness.AdministratorFallbackPath })
        {
            Harness.Http.On(HttpMethod.Get, path, HttpStatusCode.OK, """{ "count": 0, "value": [] }""");
        }

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        ShouldBeRefusedAsAdministratorUnavailable(result);
    }

    [Fact]
    public async Task CompleteConnect_ShouldRefuseWhenThePermissionCallFails()
    {
        Harness.StubPassingChecks();

        foreach (var path in new[] { AzureDevOpsTestHarness.AdministratorPath, AzureDevOpsTestHarness.AdministratorFallbackPath })
        {
            Harness.Http.On(HttpMethod.Get, path, HttpStatusCode.InternalServerError, """{ "message": "Something went wrong" }""");
        }

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        ShouldBeRefusedAsAdministratorUnavailable(result);
    }

    [Fact]
    public async Task CompleteConnect_ShouldReturnTheChecksWithTheConnection()
    {
        Harness.StubPassingChecks();

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        result.Payload!.Connection.Should().NotBeNull();
        result.Payload.Checks.Should().HaveCount(7);
        result.Payload.Checks.Should().OnlyContain(check => check.Status == ScmConnectionCheckStatus.Passed);
    }

    [Fact]
    public async Task CompleteConnect_ShouldStillOfferTheConnectionWhenOnlyAnOptionalScopeIsMissing()
    {
        Harness.StubPassingChecks();
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.BuildsProbePath, HttpStatusCode.Unauthorized);

        var result = await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        var builds = result.Payload!.Checks!.Single(check => check.Key == AzureDevOpsConnectionChecker.BuildsKey);

        result.Payload.Connection.Should().NotBeNull();
        builds.Status.Should().Be(ScmConnectionCheckStatus.Warning);
    }

    [Fact]
    public async Task CompleteConnect_ShouldReportRateLimitingFromATooManyRequestsResponse()
    {
        Harness.Http.On(
            HttpMethod.Get,
            AzureDevOpsTestHarness.ConnectionDataPath,
            HttpStatusCode.TooManyRequests,
            configure: response => response.Headers.Add("Retry-After", "30"));

        var connect = async () => await Harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        var thrown = await connect.Should().ThrowAsync<ScmRateLimitedException>();

        thrown.Which.RetryAt.Should().Be(Harness.Clock.GetUtcNow().AddSeconds(30));
    }

    [Fact]
    public async Task CompleteConnect_ShouldNeverPutTheTokenInAFailureMessage()
    {
        string[] messages =
        [
            await FailureMessage(HttpStatusCode.Unauthorized),
            await FailureMessage(HttpStatusCode.NotFound),
            await FailureMessage(HttpStatusCode.NonAuthoritativeInformation),
        ];

        messages.Should().OnlyContain(message => !message.Contains(AzureDevOpsTestHarness.Token));
    }

    [Fact]
    public void BeginConnect_ShouldFailRatherThanRedirect()
    {
        var started = Harness.Connector.BeginConnect(new ScmConnectRequest(3, "user-1"));

        started.IsSuccess.Should().BeFalse();
        started.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetAccessToken_ShouldDecryptTheStoredTokenAndUseTheStoredExpiry()
    {
        var expiresAt = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var connection = Harness.CreateConnection(expiresAt);

        var accessToken = await Harness.Connector.GetAccessToken(connection, TestContext.Current.CancellationToken);

        accessToken.Token.Should().Be(AzureDevOpsTestHarness.Token);
        accessToken.ExpiresAt.Should().Be(new DateTimeOffset(expiresAt));
    }

    [Fact]
    public async Task GetAccessToken_ShouldNeverExpireWithoutAStoredExpiry()
    {
        var connection = Harness.CreateConnection();

        var accessToken = await Harness.Connector.GetAccessToken(connection, TestContext.Current.CancellationToken);

        accessToken.ExpiresAt.Should().Be(DateTimeOffset.MaxValue);
    }

    [Fact]
    public void Provider_ShouldConnectWithATokenAndOnlyListRepositories()
    {
        Harness.Provider.ConnectMethod.Should().Be(ScmConnectMethod.Token);
        Harness.Provider.Capabilities.Should().Be(SourceControlCapabilities.ListRepositories);
        Harness.Provider.WebhookReader.Should().BeNull();
        Harness.Provider.ConnectionChecker.Should().BeSameAs(Harness.Checker);
    }

    public void Dispose()
    {
        Harness.Dispose();
    }

    private static async Task<string> FailureMessage(HttpStatusCode status)
    {
        using var harness = new AzureDevOpsTestHarness();

        harness.Http.On(
            HttpMethod.Get,
            AzureDevOpsTestHarness.ConnectionDataPath,
            status,
            configure: AzureDevOpsTestHarness.RespondWithSignInPage);

        var result = await harness.Connector.CompleteConnect(
            AzureDevOpsTestHarness.CreateCallback(),
            TestContext.Current.CancellationToken);

        var details = result.Payload!.Checks!.Select(check => harness.Checker.Describe(check).Detail);

        return string.Join(" ", details);
    }

    private static ScmConnectionCheck ShouldBeRefusedBy(ClientResponse<ScmConnectOutcome> result, string checkKey)
    {
        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.Connection.Should().BeNull();

        var check = result.Payload.Checks!.Single(candidate => candidate.Key == checkKey);

        check.Status.Should().Be(ScmConnectionCheckStatus.Failed);

        return check;
    }

    private static void ShouldBeRefusedAsAdministratorUnavailable(ClientResponse<ScmConnectOutcome> result)
    {
        var administrator = ShouldBeRefusedBy(result, AzureDevOpsConnectionChecker.AdministratorKey);
        var reason = administrator.Facts[AzureDevOpsConnectionChecker.ReasonFact];

        reason.Should().Be(AzureDevOpsConnectionChecker.UnavailableReason);
    }
}
