using System.Net;

using FluentAssertions;

using Netptune.Core.Localization;
using Netptune.Core.SourceControl.Checks;
using Netptune.SourceControl.Providers.AzureDevOps;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Providers.AzureDevOps;

public sealed class AzureDevOpsConnectionCheckerTests : IDisposable
{
    private readonly AzureDevOpsTestHarness Harness = new();

    private static readonly string[] CheckOrder =
    [
        AzureDevOpsConnectionChecker.OrganisationKey,
        AzureDevOpsConnectionChecker.TokenKey,
        AzureDevOpsConnectionChecker.AdministratorKey,
        AzureDevOpsConnectionChecker.ProjectsKey,
        AzureDevOpsConnectionChecker.RepositoriesKey,
        AzureDevOpsConnectionChecker.WorkItemsKey,
        AzureDevOpsConnectionChecker.BuildsKey,
    ];

    [Fact]
    public async Task Check_ShouldPassEveryCheckInOrderWithTheStoredToken()
    {
        Harness.StubPassingChecks();

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        checks.Select(check => check.Key).Should().Equal(CheckOrder);
        checks.Should().OnlyContain(check => check.Status == ScmConnectionCheckStatus.Passed);
        Harness.Http.Requests.Should().OnlyContain(request => request.Authorization == AzureDevOpsTestHarness.BasicCredential(AzureDevOpsTestHarness.Token));
    }

    [Fact]
    public async Task Check_ShouldCaptureTheFactsShownInTheDetails()
    {
        Harness.StubPassingChecks();

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var organisation = Find(checks, AzureDevOpsConnectionChecker.OrganisationKey);
        var token = Find(checks, AzureDevOpsConnectionChecker.TokenKey);
        var repositories = Find(checks, AzureDevOpsConnectionChecker.RepositoriesKey);

        organisation.Facts[AzureDevOpsConnectionChecker.OrganisationFact].Should().Be("acme");
        token.Facts[AzureDevOpsConnectionChecker.UserFact].Should().Be(AzureDevOpsTestHarness.UserDisplayName);
        repositories.Facts[AzureDevOpsConnectionChecker.RepositoriesFact].Should().Be("3");
        repositories.Facts[AzureDevOpsConnectionChecker.ProjectsFact].Should().Be("2");
    }

    [Fact]
    public async Task Check_ShouldFailTheOrganisationAndSkipEverythingElseOnA404()
    {
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.ConnectionDataPath, HttpStatusCode.NotFound);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        checks.Select(check => check.Key).Should().Equal(CheckOrder);
        checks[0].Status.Should().Be(ScmConnectionCheckStatus.Failed);
        checks.Skip(1).Should().OnlyContain(check => check.Status == ScmConnectionCheckStatus.Skipped);
        Harness.Http.Requests.Should().ContainSingle();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NonAuthoritativeInformation)]
    [InlineData(HttpStatusCode.OK)]
    public async Task Check_ShouldFailTheTokenAndSkipTheChecksThatNeedIt(HttpStatusCode status)
    {
        Harness.Http.On(
            HttpMethod.Get,
            AzureDevOpsTestHarness.ConnectionDataPath,
            status,
            configure: AzureDevOpsTestHarness.RespondWithSignInPage);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        checks[0].Status.Should().Be(ScmConnectionCheckStatus.Passed);
        checks[1].Status.Should().Be(ScmConnectionCheckStatus.Failed);
        checks.Skip(2).Should().OnlyContain(check => check.Status == ScmConnectionCheckStatus.Skipped);
    }

    [Fact]
    public async Task Check_ShouldFailTheTokenWhenTheConnectionHasNoStoredToken()
    {
        var connection = Harness.CreateConnection() with { ProtectedCredential = null };

        var checks = await Harness.Checker.Check(connection, TestContext.Current.CancellationToken);

        Find(checks, AzureDevOpsConnectionChecker.TokenKey).Status.Should().Be(ScmConnectionCheckStatus.Failed);
        Harness.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Check_ShouldFallBackToTheNamespaceTokenWithoutAColon()
    {
        Harness.StubPassingChecks();
        Harness.StubAdministratorAnswer(AzureDevOpsTestHarness.AdministratorPath, isAdministrator: false);
        Harness.StubAdministratorAnswer(AzureDevOpsTestHarness.AdministratorFallbackPath, isAdministrator: true);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var paths = Harness.Http.Requests.Select(request => request.Uri.PathAndQuery).ToList();

        Find(checks, AzureDevOpsConnectionChecker.AdministratorKey).Status.Should().Be(ScmConnectionCheckStatus.Passed);
        paths.Should().ContainInOrder(AzureDevOpsTestHarness.AdministratorPath, AzureDevOpsTestHarness.AdministratorFallbackPath);
    }

    [Fact]
    public async Task Check_ShouldNotAskTheFallbackFormWhenTheFirstConfirmsAnAdministrator()
    {
        Harness.StubPassingChecks();

        await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var paths = Harness.Http.Requests.Select(request => request.Uri.PathAndQuery);

        paths.Should().NotContain(AzureDevOpsTestHarness.AdministratorFallbackPath);
    }

    [Fact]
    public async Task Check_ShouldUseTheFallbackFormWhenTheFirstCannotBeRead()
    {
        Harness.StubPassingChecks();
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.AdministratorPath, HttpStatusCode.BadRequest);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        Find(checks, AzureDevOpsConnectionChecker.AdministratorKey).Status.Should().Be(ScmConnectionCheckStatus.Passed);
    }

    [Fact]
    public async Task Check_ShouldDenyAnAccountBothFormsSayIsNotAnAdministrator()
    {
        Harness.StubPassingChecks();
        Harness.StubAdministrator(isAdministrator: false);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var administrator = Find(checks, AzureDevOpsConnectionChecker.AdministratorKey);

        administrator.Status.Should().Be(ScmConnectionCheckStatus.Failed);
        administrator.Facts[AzureDevOpsConnectionChecker.ReasonFact].Should().Be(AzureDevOpsConnectionChecker.DeniedReason);
        Find(checks, AzureDevOpsConnectionChecker.RepositoriesKey).Status.Should().Be(ScmConnectionCheckStatus.Passed);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Check_ShouldFailRepositoriesWithoutCodeReadButStillCheckBuilds(HttpStatusCode status)
    {
        Harness.StubPassingChecks();
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.RepositoriesPath, status);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        Find(checks, AzureDevOpsConnectionChecker.RepositoriesKey).Status.Should().Be(ScmConnectionCheckStatus.Failed);
        Find(checks, AzureDevOpsConnectionChecker.WorkItemsKey).Status.Should().Be(ScmConnectionCheckStatus.Passed);
        Find(checks, AzureDevOpsConnectionChecker.BuildsKey).Status.Should().Be(ScmConnectionCheckStatus.Passed);
    }

    [Fact]
    public async Task Check_ShouldPassProjectsAndProbeBuildsInTheFirstProject()
    {
        Harness.StubPassingChecks();

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var projects = Find(checks, AzureDevOpsConnectionChecker.ProjectsKey);
        var description = Harness.Checker.Describe(projects);
        var paths = Harness.Http.Requests.Select(request => request.Uri.PathAndQuery);

        projects.Status.Should().Be(ScmConnectionCheckStatus.Passed);
        description.Title.Should().Be(AppText.AzureDevOpsCheckProjectsTitle);
        description.Detail.Should().Be(AppText.AzureDevOpsCheckProjectsPassed);
        description.IsRequired.Should().BeTrue();
        paths.Should().Contain(AzureDevOpsTestHarness.ProjectsPath).And.Contain(AzureDevOpsTestHarness.BuildsProbePath);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Check_ShouldFailProjectsAndSkipBuildsWithoutProjectAndTeamRead(HttpStatusCode status)
    {
        Harness.StubPassingChecks();
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.ProjectsPath, status);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var projects = Find(checks, AzureDevOpsConnectionChecker.ProjectsKey);
        var builds = Find(checks, AzureDevOpsConnectionChecker.BuildsKey);

        projects.Status.Should().Be(ScmConnectionCheckStatus.Failed);
        projects.Facts[AzureDevOpsConnectionChecker.ReasonFact].Should().Be(AzureDevOpsConnectionChecker.DeniedReason);
        Harness.Checker.Describe(projects).Detail.Should().Be(AppText.AzureDevOpsCheckProjectsFailed);
        builds.Status.Should().Be(ScmConnectionCheckStatus.Skipped);
        Harness.Checker.Describe(builds).Detail.Should().Be(AppText.AzureDevOpsCheckSkipped);
        Find(checks, AzureDevOpsConnectionChecker.RepositoriesKey).Status.Should().Be(ScmConnectionCheckStatus.Passed);
        Find(checks, AzureDevOpsConnectionChecker.WorkItemsKey).Status.Should().Be(ScmConnectionCheckStatus.Passed);
        ScmConnectionChecks.HasFailure(checks).Should().BeTrue();
    }

    [Fact]
    public async Task Check_ShouldFailTheTokenWhenTheStoredTokenCannotBeRead()
    {
        var connection = Harness.CreateConnection() with { ProtectedCredential = [1, 2, 3, 4] };

        var checks = await Harness.Checker.Check(connection, TestContext.Current.CancellationToken);

        var token = Find(checks, AzureDevOpsConnectionChecker.TokenKey);

        checks.Select(check => check.Key).Should().Equal(CheckOrder);
        token.Status.Should().Be(ScmConnectionCheckStatus.Failed);
        token.Facts[AzureDevOpsConnectionChecker.ReasonFact].Should().Be(AzureDevOpsConnectionChecker.UnreadableReason);
        Harness.Checker.Describe(token).Detail.Should().Be(AppText.AzureDevOpsCheckTokenUnreadable);
        Harness.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Check_ShouldWarnWhenWorkItemsReadIsMissing()
    {
        Harness.StubPassingChecks();
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.WorkItemsProbePath, HttpStatusCode.Unauthorized);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var workItems = Find(checks, AzureDevOpsConnectionChecker.WorkItemsKey);

        workItems.Status.Should().Be(ScmConnectionCheckStatus.Warning);
        workItems.Facts[AzureDevOpsConnectionChecker.ReasonFact].Should().Be(AzureDevOpsConnectionChecker.DeniedReason);
        ScmConnectionChecks.HasFailure(checks).Should().BeFalse();
    }

    [Fact]
    public async Task Check_ShouldWarnWhenBuildReadIsMissing()
    {
        Harness.StubPassingChecks();
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.BuildsProbePath, HttpStatusCode.Forbidden);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        Find(checks, AzureDevOpsConnectionChecker.BuildsKey).Status.Should().Be(ScmConnectionCheckStatus.Warning);
    }

    [Fact]
    public async Task Check_ShouldWarnRatherThanFailWhenAnOptionalProbeGoesUnanswered()
    {
        Harness.StubPassingChecks();
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.BuildsProbePath, HttpStatusCode.InternalServerError);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var builds = Find(checks, AzureDevOpsConnectionChecker.BuildsKey);

        builds.Status.Should().Be(ScmConnectionCheckStatus.Warning);
        builds.Facts[AzureDevOpsConnectionChecker.ReasonFact].Should().Be(AzureDevOpsConnectionChecker.UnansweredReason);
    }

    [Fact]
    public async Task Check_ShouldSkipBuildsWhenTheOrganisationHasNoProjects()
    {
        Harness.StubPassingChecks();
        Harness.Http.On(
            HttpMethod.Get,
            AzureDevOpsTestHarness.ProjectsPath,
            HttpStatusCode.OK,
            """{ "count": 0, "value": [] }""");

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var builds = Find(checks, AzureDevOpsConnectionChecker.BuildsKey);

        builds.Status.Should().Be(ScmConnectionCheckStatus.Skipped);
        builds.Facts[AzureDevOpsConnectionChecker.ReasonFact].Should().Be(AzureDevOpsConnectionChecker.NoProjectsReason);
        Harness.Checker.Describe(builds).Detail.Should().Be(AppText.AzureDevOpsCheckNoProjects);
    }

    [Fact]
    public async Task Describe_ShouldLocalizeTheDetailsFromTheFacts()
    {
        Harness.StubPassingChecks();

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var details = checks.Select(check => Harness.Checker.Describe(check)).ToList();

        var repositories = Localized.Format(AppText.AzureDevOpsRepositoryCountOther, 3);
        var projects = Localized.Format(AppText.AzureDevOpsProjectCountOther, 2);

        details[0].Detail.Should().Be(Localized.Format(AppText.AzureDevOpsCheckOrganisationPassed, "acme"));
        details[1].Detail.Should().Be(Localized.Format(AppText.AzureDevOpsCheckTokenPassed, AzureDevOpsTestHarness.UserDisplayName));
        details[4].Detail.Should().Be(Localized.Format(AppText.AzureDevOpsCheckRepositoriesPassed, repositories, projects));
        details.Take(5).Should().OnlyContain(description => description.IsRequired);
        details.Skip(5).Should().OnlyContain(description => !description.IsRequired);
    }

    [Fact]
    public void Describe_ShouldUseTheSingularForOneRepositoryInOneProject()
    {
        var check = new ScmConnectionCheck
        {
            Key = AzureDevOpsConnectionChecker.RepositoriesKey,
            Status = ScmConnectionCheckStatus.Passed,
            Facts = new Dictionary<string, string>
            {
                [AzureDevOpsConnectionChecker.RepositoriesFact] = "1",
                [AzureDevOpsConnectionChecker.ProjectsFact] = "1",
            },
        };

        var expected = Localized.Format(
            AppText.AzureDevOpsCheckRepositoriesPassed,
            AppText.AzureDevOpsRepositoryCountOne,
            AppText.AzureDevOpsProjectCountOne);

        Harness.Checker.Describe(check).Detail.Should().Be(expected);
    }

    [Fact]
    public async Task Check_ShouldNeverPutTheTokenInAFactOrDetail()
    {
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.ConnectionDataPath, HttpStatusCode.Unauthorized);

        var checks = await Harness.Checker.Check(Harness.CreateConnection(), TestContext.Current.CancellationToken);

        var facts = checks.SelectMany(check => check.Facts.Values);
        var details = checks.Select(check => Harness.Checker.Describe(check).Detail);

        facts.Concat(details).Should().OnlyContain(text => !text.Contains(AzureDevOpsTestHarness.Token));
    }

    public void Dispose()
    {
        Harness.Dispose();
    }

    private static ScmConnectionCheck Find(IReadOnlyList<ScmConnectionCheck> checks, string key)
    {
        return checks.Single(check => check.Key == key);
    }
}
