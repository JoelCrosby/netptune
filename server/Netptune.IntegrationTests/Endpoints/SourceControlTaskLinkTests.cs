using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SourceControlTaskLinkTests
{
    private const string WebhookUrl = "api/webhooks/source-control/gitlab";

    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;
    private readonly HttpClient Anonymous;

    public SourceControlTaskLinkTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
        Anonymous = fixture.CreateAnonymousNetptuneClient("netptune");
    }

    [Fact]
    public async Task PullRequest_ShouldLinkTheTaskItMentions()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();

        await ProcessPullRequest(
            account,
            repo.ExternalId,
            "7",
            $"Fix search ({task.SystemId.ToUpperInvariant()})", "Open");

        var links = await LinksFor(task.Id, repo.Id);
        var link = links.Should().ContainSingle().Subject;

        link.Kind.Should().Be(TaskCodeLinkKind.PullRequest);
        link.ExternalRef.Should().Be("7");
        link.PullRequestState.Should().Be(ScmPullRequestState.Open);
        link.AuthorLogin.Should().Be("tester");
    }

    [Fact]
    public async Task PullRequest_ShouldUpdateTheExistingLinkWhenItMerges()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();

        await ProcessPullRequest(account, repo.ExternalId, "8", $"Fix {task.SystemId}", "Open");
        await ProcessPullRequest(account, repo.ExternalId, "8", "Retitled without the id", "Merged");

        var link = (await LinksFor(task.Id, repo.Id)).Should().ContainSingle().Subject;

        link.PullRequestState.Should().Be(ScmPullRequestState.Merged);
        link.Title.Should().Be("Retitled without the id");
    }

    [Fact]
    public async Task PullRequest_ShouldNotLinkFromARepoThatIsNotLinked()
    {
        var (account, linkedRepo) = await ConnectWithLinkedRepo();
        var unlinkedRepo = (await GetRepos(linkedRepo.ConnectionId)).Single(repo => !repo.IsLinked);
        var task = await AnyTask();

        await ProcessPullRequest(account, unlinkedRepo.ExternalId, "9", $"Fix {task.SystemId}", "Open");

        (await LinksFor(task.Id, unlinkedRepo.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task PullRequest_ShouldIgnoreAnIdWithNoMatchingTask()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var missingId = $"{task.SystemId.Split('-')[0]}-999999";

        await ProcessPullRequest(account, repo.ExternalId, "10", $"Fix {missingId}", "Open");

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var links = await context.TaskCodeLinks
            .Where(link => link.RepoId == repo.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        links.Should().BeEmpty();
    }

    [Fact]
    public async Task TaskLinks_ShouldReturnThePullRequestWithItsLatestChecks()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var sha = Guid.NewGuid().ToString("N");

        await ProcessPullRequest(account, repo.ExternalId, "11", $"Fix {task.SystemId}", "Open", sha);
        await ProcessCheckRun(account, repo.ExternalId, "1", "build", sha, "Completed", "Failure");
        await ProcessCheckRun(account, repo.ExternalId, "2", "build", sha, "Completed", "Success");
        await ProcessCheckRun(account, repo.ExternalId, "3", "lint", sha, "InProgress", null);

        var links = await GetTaskLinks(task.SystemId);

        var link = links.Should().ContainSingle(candidate => candidate.ExternalRef == "11"
            && candidate.Kind == TaskCodeLinkKind.PullRequest).Subject;

        link.Provider.Should().Be(SourceControlProvider.GitLab);
        link.Checks.Should().NotBeNull();
        link.Checks!.State.Should().Be(ScmCheckState.Pending);
        link.Checks.Total.Should().Be(2);
        link.Checks.Passed.Should().Be(1);
        link.Checks.Pending.Should().Be(1);
    }

    [Fact]
    public async Task TaskLinks_ShouldFollowThePullRequestToItsNewHead()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var firstSha = Guid.NewGuid().ToString("N");
        var secondSha = Guid.NewGuid().ToString("N");

        await ProcessPullRequest(account, repo.ExternalId, "12", $"Fix {task.SystemId}", "Open", firstSha);
        await ProcessCheckRun(account, repo.ExternalId, "31", "build", firstSha, "Completed", "Failure");
        await ProcessPullRequest(account, repo.ExternalId, "12", $"Fix {task.SystemId}", "Open", secondSha);
        await ProcessCheckRun(account, repo.ExternalId, "32", "build", secondSha, "Completed", "Success");

        var links = await GetTaskLinks(task.SystemId);
        var link = links.Should().ContainSingle(candidate => candidate.ExternalRef == "12").Subject;

        link.Checks!.State.Should().Be(ScmCheckState.Success);
        link.Checks.Total.Should().Be(1);
    }

    [Fact]
    public async Task CheckRun_ShouldNotBeStoredForARepoThatIsNotLinked()
    {
        var (account, linkedRepo) = await ConnectWithLinkedRepo();
        var unlinkedRepo = (await GetRepos(linkedRepo.ConnectionId)).Single(repo => !repo.IsLinked);

        await ProcessCheckRun(account, unlinkedRepo.ExternalId, "41", "build", "abc", "Completed", "Success");

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var stored = await context.SourceControlCheckRuns
            .AsNoTracking()
            .Where(checkRun => checkRun.RepoId == unlinkedRepo.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        stored.Should().BeEmpty();
    }

    [Fact]
    public async Task Processing_ShouldPublishCodeLinksWhenAPullRequestLinksATask()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();

        var published = await WorkspaceEventProbe.Published(
            Fixture.Services,
            string.Empty,
            WorkspaceEventScopes.CodeLinks,
            () => ProcessPullRequest(account, repo.ExternalId, "13", $"Fix {task.SystemId}", "Open"));

        published.Should().BeTrue();
    }

    [Fact]
    public async Task TaskLinks_ShouldReturnNotFoundForAnUnknownTask()
    {
        var response = await Client.GetAsync(
            "api/source-control/tasks/nope-999999/links",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckRun_ShouldNotGoBackToRunningOnceItHasFinished()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var sha = Guid.NewGuid().ToString("N");

        await ProcessCheckRun(account, repo.ExternalId, "21", "build", sha, "Completed", "Success");
        await ProcessCheckRun(account, repo.ExternalId, "21", "build", sha, "Queued", null);

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var stored = await context.SourceControlCheckRuns
            .AsNoTracking()
            .SingleAsync(
                checkRun => checkRun.RepoId == repo.Id && checkRun.ExternalId == "21",
                TestContext.Current.CancellationToken);

        stored.Status.Should().Be(ScmCheckRunStatus.Completed);
        stored.Conclusion.Should().Be(ScmCheckRunConclusion.Success);
    }

    [Fact]
    public async Task Push_ShouldLinkCommitsOnABranchNamedAfterTheTask()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var sha = Guid.NewGuid().ToString("N");

        await ProcessPush(account, repo.ExternalId, $"feature/{task.SystemId}-search", sha, "Tidy up");

        var links = await LinksFor(task.Id, repo.Id);

        links.Should().ContainSingle(link => link.Kind == TaskCodeLinkKind.Commit && link.ExternalRef == sha);
    }

    [Fact]
    public async Task Push_ShouldNotLinkFromTheDefaultBranchName()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var defaultBranch = $"release/{task.SystemId}";
        var sha = Guid.NewGuid().ToString("N");

        await SetDefaultBranch(repo.Id, defaultBranch);
        await ProcessPush(account, repo.ExternalId, defaultBranch, sha, "Merge without an id");

        (await LinksForRepo(repo.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Branch_ShouldBeMarkedRemovedWhenItIsDeleted()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var branch = $"feature/{task.SystemId}-{Guid.NewGuid():N}"[..40];

        await ProcessBranch(account, repo.ExternalId, TestSourceControlProvider.BranchCreatedEvent, branch);
        await ProcessBranch(account, repo.ExternalId, TestSourceControlProvider.BranchDeletedEvent, branch);

        var links = await LinksFor(task.Id, repo.Id);
        var link = links.Should().ContainSingle(candidate => candidate.ExternalRef == branch).Subject;

        link.IsRemoved.Should().BeTrue();
    }

    [Fact]
    public async Task Branch_ShouldBeSkippedWhenItsNameIsTooLongToStore()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var branch = $"feature/{task.SystemId}-{new string('x', 300)}";

        await ProcessBranch(account, repo.ExternalId, TestSourceControlProvider.BranchCreatedEvent, branch);

        (await LinksForRepo(repo.Id)).Should().BeEmpty();
        (await PendingDeliveries(account)).Should().BeEmpty();
    }

    [Fact]
    public async Task PullRequest_ShouldIgnoreAnUpdateOlderThanTheStoredState()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var mergedAt = DateTimeOffset.UtcNow;

        await ProcessPullRequest(account, repo.ExternalId, "14", $"Fix {task.SystemId}", "Merged", updatedAt: mergedAt);
        await ProcessPullRequest(
            account,
            repo.ExternalId,
            "14",
            $"Fix {task.SystemId}", "Open", updatedAt: mergedAt.AddMinutes(-5));

        var link = (await LinksFor(task.Id, repo.Id)).Should().ContainSingle().Subject;

        link.PullRequestState.Should().Be(ScmPullRequestState.Merged);
    }

    [Fact]
    public async Task Processing_ShouldRecordAFailedAttempt()
    {
        var (account, _) = await ConnectWithLinkedRepo();

        await PostDelivery(account, TestSourceControlProvider.FailingEvent, $$"""{ "account": "{{account}}" }""");

        var deliveryId = (await PendingDeliveries(account)).Single();

        using (var scope = Fixture.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IScmWebhookProcessor>();
            var process = async () => await processor.Process(deliveryId, TestContext.Current.CancellationToken);

            await process.Should().ThrowAsync<InvalidOperationException>();
        }

        using var verifyScope = Fixture.CreateScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<DataContext>();
        var delivery = await context.ScmWebhookDeliveries
            .AsNoTracking()
            .SingleAsync(stored => stored.Id == deliveryId, TestContext.Current.CancellationToken);

        delivery.Attempts.Should().Be(1);
        delivery.LastError.Should().Contain("could not read");
        delivery.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task PullRequest_ShouldLinkEachWorkspaceSharingAnInstallationToItsOwnTask()
    {
        var linuxClient = await CreateOwnerClient("linux");
        var (account, netptuneRepo) = await ConnectWithLinkedRepo(Client);
        var (_, linuxRepo) = await ConnectWithLinkedRepo(linuxClient, account);
        var netptuneTask = await AnyTask();
        var linuxTask = await AnyTask("linux");

        await ProcessPullRequest(
            account,
            netptuneRepo.ExternalId,
            "15",
            $"Fix {netptuneTask.SystemId} and {linuxTask.SystemId}", "Open");

        (await LinksForRepo(netptuneRepo.Id)).Select(link => link.TaskId).Should().Equal(netptuneTask.Id);
        (await LinksForRepo(linuxRepo.Id)).Select(link => link.TaskId).Should().Equal(linuxTask.Id);
    }

    [Fact]
    public async Task TaskLinks_ShouldAttributeTheAuthorToTheMemberWhoMappedTheirUsername()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var username = $"dev-{Guid.NewGuid():N}"[..20];
        var identity = await AddIdentity(username);

        var shoutedUsername = username.ToUpperInvariant();

        var title = $"Fix {task.SystemId}";

        await ProcessPullRequest(account, repo.ExternalId, "21", title, "Open", author: shoutedUsername);

        var link = (await GetTaskLinks(task.SystemId)).Single(candidate => candidate.ExternalRef == "21");

        link.Author.Should().NotBeNull();
        link.Author!.Id.Should().Be(identity.UserId);
    }

    [Fact]
    public async Task TaskLinks_ShouldLeaveAnUnmappedAuthorAsTheirUsername()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var username = $"nobody-{Guid.NewGuid():N}"[..20];

        await ProcessPullRequest(account, repo.ExternalId, "22", $"Fix {task.SystemId}", "Open", author: username);

        var link = (await GetTaskLinks(task.SystemId)).Single(candidate => candidate.ExternalRef == "22");

        link.Author.Should().BeNull();
        link.AuthorLogin.Should().Be(username);
    }

    [Fact]
    public async Task TaskLinks_ShouldFollowARenamedAccountByItsId()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var externalId = Guid.NewGuid().ToString("N");
        var renamed = $"new-{Guid.NewGuid():N}"[..20];
        var userId = await CurrentUserId();

        await RecordVerified(userId, externalId, $"old-{Guid.NewGuid():N}"[..20]);
        await ProcessPullRequest(
            account,
            repo.ExternalId,
            "23",
            $"Fix {task.SystemId}",
            "Open",
            author: renamed,
            authorId: externalId);

        var link = (await GetTaskLinks(task.SystemId)).Single(candidate => candidate.ExternalRef == "23");
        var identity = await StoredIdentity(externalId);

        link.Author!.Id.Should().Be(userId);
        identity.Username.Should().Be(renamed);
    }

    [Fact]
    public async Task Processing_ShouldMoveAUsernameToTheAccountThatNowHoldsIt()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var username = $"moved-{Guid.NewGuid():N}"[..20];
        var formerId = Guid.NewGuid().ToString("N");
        var currentId = Guid.NewGuid().ToString("N");
        var userId = await CurrentUserId();
        var otherUserId = await UserIdOf(SeedData.Users.ElementAt(2).Email);

        await RecordVerified(otherUserId, formerId, username);
        await RecordVerified(userId, currentId, $"before-{Guid.NewGuid():N}"[..20]);
        await ProcessPullRequest(
            account,
            repo.ExternalId,
            "24",
            $"Fix {task.SystemId}",
            "Open",
            author: username,
            authorId: currentId);

        var former = await StoredIdentity(formerId);
        var current = await StoredIdentity(currentId);

        former.Username.Should().BeNull();
        current.Username.Should().Be(username);
    }

    [Fact]
    public async Task Processing_ShouldRecordPullRequestActivityOnTheLinkedTask()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var author = $"nobody-{Guid.NewGuid():N}"[..20];

        await ProcessPullRequest(account, repo.ExternalId, "31", $"Fix {task.SystemId}", "Merged", author: author);

        var record = (await ActivityRecords(task.Id, EventKeys.SourceControlPullRequestMerged))
            .Should().ContainSingle(candidate => ReadPayload(candidate, "reference") == "31").Subject;

        record.ActorUserId.Should().BeNull();
        ReadPayload(record, "actorLogin").Should().Be(author);
        ReadPayload(record, "repoFullName").Should().StartWith(account);
    }

    [Fact]
    public async Task Processing_ShouldAttributePullRequestActivityToTheMappedMember()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var username = $"opener-{Guid.NewGuid():N}"[..20];
        var identity = await AddIdentity(username);

        await ProcessPullRequest(account, repo.ExternalId, "32", $"Fix {task.SystemId}", "Open", author: username);

        var record = (await ActivityRecords(task.Id, EventKeys.SourceControlPullRequestOpened))
            .Should().ContainSingle(candidate => ReadPayload(candidate, "reference") == "32").Subject;

        record.ActorUserId.Should().Be(identity.UserId);
    }

    [Fact]
    public async Task Processing_ShouldRecordAPushOnceForCommitsLinkedForTheFirstTime()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var branch = $"feature/{task.SystemId}-{Guid.NewGuid():N}"[..40];
        var sha = Guid.NewGuid().ToString("N");

        await ProcessPush(account, repo.ExternalId, branch, sha, "Tidy");
        await ProcessPush(account, repo.ExternalId, branch, sha, "Tidy");

        var records = (await ActivityRecords(task.Id, EventKeys.SourceControlCommitsPushed))
            .Where(candidate => ReadPayload(candidate, "reference") == branch)
            .ToList();

        records.Should().ContainSingle();
        ReadPayload(records[0], "commitCount").Should().Be("1");
    }

    [Fact]
    public async Task Processing_ShouldRecordChecksOnlyWhenTheOverallResultChanges()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var sha = Guid.NewGuid().ToString("N");

        await ProcessPullRequest(account, repo.ExternalId, "33", $"Fix {task.SystemId}", "Open", sha);
        await ProcessCheckRun(account, repo.ExternalId, "51", "build", sha, "InProgress", null);
        await ProcessCheckRun(account, repo.ExternalId, "52", "lint", sha, "Completed", "Success");
        await ProcessCheckRun(account, repo.ExternalId, "51", "build", sha, "Completed", "Success");
        await ProcessCheckRun(account, repo.ExternalId, "53", "deploy", sha, "Completed", "Success");
        await ProcessCheckRun(account, repo.ExternalId, "54", "e2e", sha, "Completed", "Failure");

        var passed = (await ActivityRecords(task.Id, EventKeys.SourceControlChecksPassed))
            .Where(candidate => ReadPayload(candidate, "reference") == "#33")
            .ToList();
        var failed = (await ActivityRecords(task.Id, EventKeys.SourceControlChecksFailed))
            .Where(candidate => ReadPayload(candidate, "reference") == "#33")
            .ToList();

        passed.Should().ContainSingle();
        failed.Should().ContainSingle();
        ReadPayload(failed[0], "checksFailed").Should().Be("1");
    }

    [Fact]
    public async Task Processing_ShouldNotRecordPassedAgainWhenADependentJobStartsAndPasses()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var sha = Guid.NewGuid().ToString("N");

        await ProcessPullRequest(account, repo.ExternalId, "34", $"Fix {task.SystemId}", "Open", sha);
        await ProcessCheckRun(account, repo.ExternalId, "61", "build", sha, "Completed", "Success");
        await ProcessCheckRun(account, repo.ExternalId, "62", "deploy", sha, "Queued", null);
        await ProcessCheckRun(account, repo.ExternalId, "62", "deploy", sha, "Completed", "Success");

        var passed = (await ActivityRecords(task.Id, EventKeys.SourceControlChecksPassed))
            .Where(candidate => ReadPayload(candidate, "reference") == "#34")
            .ToList();

        passed.Should().ContainSingle();
    }

    [Fact]
    public async Task Processing_ShouldRecordChecksAgainForANewPullRequestHead()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var firstSha = Guid.NewGuid().ToString("N");
        var secondSha = Guid.NewGuid().ToString("N");

        await ProcessPullRequest(account, repo.ExternalId, "35", $"Fix {task.SystemId}", "Open", firstSha);
        await ProcessCheckRun(account, repo.ExternalId, "71", "build", firstSha, "Completed", "Success");
        await ProcessPullRequest(account, repo.ExternalId, "35", $"Fix {task.SystemId}", "Open", secondSha);
        await ProcessCheckRun(account, repo.ExternalId, "72", "build", secondSha, "Completed", "Success");

        var passed = (await ActivityRecords(task.Id, EventKeys.SourceControlChecksPassed))
            .Where(candidate => ReadPayload(candidate, "reference") == "#35")
            .ToList();

        passed.Should().HaveCount(2);
    }

    [Fact]
    public async Task TaskLinks_ShouldAttributeACommitByTheAuthorsEmail()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var sha = Guid.NewGuid().ToString("N");
        var userId = await CurrentUserId();
        var email = await EmailOf(userId);

        var branch = $"feature/{task.SystemId}-email";

        await ProcessPush(account, repo.ExternalId, branch, sha, "Tidy", email.ToUpperInvariant());

        var link = (await GetTaskLinks(task.SystemId)).Single(candidate => candidate.ExternalRef == sha);

        link.Author!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task RecordVerified_ShouldReplaceAnotherMembersManualClaimOnTheUsername()
    {
        var username = $"claim-{Guid.NewGuid():N}"[..20];
        var memberClient = Fixture.CreateNetptuneClientAs(SeedData.Users.ElementAt(2).Email);
        var userId = await CurrentUserId();
        var externalId = Guid.NewGuid().ToString("N");

        var claim = await memberClient.PostAsJsonAsync(
            "api/source-control/identities",
            new AddSourceControlIdentityRequest { Provider = SourceControlProvider.GitLab, Username = username },
            TestContext.Current.CancellationToken);

        await RecordVerified(userId, externalId, username);

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var holders = await context.UserSourceControlIdentities
            .AsNoTracking()
            .Where(identity => identity.NormalizedUsername == username)
            .ToListAsync(TestContext.Current.CancellationToken);

        claim.StatusCode.Should().Be(HttpStatusCode.OK);
        holders.Should().ContainSingle().Which.UserId.Should().Be(userId);
    }

    private async Task<UserSourceControlIdentity> AddIdentity(string username)
    {
        var response = await Client.PostAsJsonAsync(
            "api/source-control/identities",
            new AddSourceControlIdentityRequest { Provider = SourceControlProvider.GitLab, Username = username },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var normalized = UserSourceControlIdentity.Normalize(username);

        var identity = await context.UserSourceControlIdentities
            .AsNoTracking()
            .SingleAsync(stored => stored.NormalizedUsername == normalized, TestContext.Current.CancellationToken);

        return identity;
    }

    private async Task<string> CurrentUserId()
    {
        var identity = await AddIdentity($"whoami-{Guid.NewGuid():N}"[..20]);

        return identity.UserId;
    }

    private async Task<List<EventRecord>> ActivityRecords(int taskId, string eventKey)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var subjectId = taskId.ToString();

        var records = await context.EventRecords
            .AsNoTracking()
            .Where(record => record.EventKey == eventKey && record.SubjectType == "task" && record.SubjectId == subjectId)
            .ToListAsync(TestContext.Current.CancellationToken);

        return records;
    }

    private static string? ReadPayload(EventRecord record, string property)
    {
        var hasProperty = record.Payload.RootElement.TryGetProperty(property, out var value);

        return hasProperty && value.ValueKind != JsonValueKind.Null ? value.ToString() : null;
    }

    private async Task<string> UserIdOf(string email)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var userId = await context.Users
            .AsNoTracking()
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        return userId;
    }

    private async Task<string> EmailOf(string userId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var email = await context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Email!)
            .SingleAsync(TestContext.Current.CancellationToken);

        return email;
    }

    private async Task RecordVerified(string userId, string externalId, string username)
    {
        using var scope = Fixture.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();

        await unitOfWork.UserSourceControlIdentities.RecordVerified(
            userId,
            SourceControlProvider.GitLab,
            externalId,
            username,
            DateTime.UtcNow,
            TestContext.Current.CancellationToken);

        await unitOfWork.CompleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task<UserSourceControlIdentity> StoredIdentity(string externalId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var identity = await context.UserSourceControlIdentities
            .AsNoTracking()
            .SingleAsync(stored => stored.ExternalId == externalId, TestContext.Current.CancellationToken);

        return identity;
    }

    private async Task SetDefaultBranch(int repoId, string branch)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await context.SourceControlRepos
            .Where(repo => repo.Id == repoId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(repo => repo.DefaultBranch, branch),
                TestContext.Current.CancellationToken);
    }

    private async Task<HttpClient> CreateOwnerClient(string slug)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var ownerEmail = await context.Workspaces
            .AsNoTracking()
            .Where(workspace => workspace.Slug == slug)
            .Select(workspace => workspace.Owner!.Email)
            .SingleAsync(TestContext.Current.CancellationToken);

        return Fixture.CreateNetptuneClientAs(ownerEmail!, slug);
    }

    private async Task ProcessPush(
        string account,
        string repoExternalId,
        string branch,
        string sha,
        string message,
        string? email = null)
    {
        var emailField = email is null ? string.Empty : $$""", "email": "{{email}}" """;
        var body = $$"""{ "account": "{{account}}", "repo": "{{repoExternalId}}", "branch": "{{branch}}", "commits": [ { "sha": "{{sha}}", "message": "{{message}}"{{emailField}} } ] }""";

        await Process(account, TestSourceControlProvider.PushEvent, body);
    }

    private async Task ProcessBranch(string account, string repoExternalId, string eventName, string branch)
    {
        var body = $$"""{ "account": "{{account}}", "repo": "{{repoExternalId}}", "branch": "{{branch}}" }""";

        await Process(account, eventName, body);
    }

    private async Task<List<long>> PendingDeliveries(string account)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var pending = await context.ScmWebhookDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.ExternalAccountId == account && delivery.ProcessedAt == null)
            .Select(delivery => delivery.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        return pending;
    }

    private async Task<List<TaskCodeLink>> LinksForRepo(int repoId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var links = await context.TaskCodeLinks
            .AsNoTracking()
            .Where(link => link.RepoId == repoId)
            .ToListAsync(TestContext.Current.CancellationToken);

        return links;
    }

    private async Task<List<TaskCodeLinkViewModel>> GetTaskLinks(string systemId)
    {
        var response = await Client.GetAsync(
            $"api/source-control/tasks/{systemId}/links",
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<TaskCodeLinkViewModel>>>(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return result.Payload!;
    }

    private Task<(string Account, RepoRef Repo)> ConnectWithLinkedRepo()
    {
        return ConnectWithLinkedRepo(Client);
    }

    private async Task<(string Account, RepoRef Repo)> ConnectWithLinkedRepo(
        HttpClient client,
        string? sharedAccount = null)
    {
        var account = sharedAccount ?? $"link-{Guid.NewGuid():N}"[..20];

        var connect = await client.PostAsJsonAsync(
            "api/source-control/connections/complete",
            new CompleteSourceControlConnectRequest
            {
                Provider = SourceControlProvider.GitLab,
                Parameters = new Dictionary<string, string> { ["account"] = account },
            },
            TestContext.Current.CancellationToken);

        var connection = await connect.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectionViewModel>>(
            TestContext.Current.CancellationToken);

        var repos = await GetRepos(connection.Payload!.Id);
        var repo = repos.First();

        var linked = await client.PutAsJsonAsync(
            $"api/source-control/repos/{repo.Id}/linked",
            new SetSourceControlRepoLinkedRequest { IsLinked = true },
            TestContext.Current.CancellationToken);

        linked.StatusCode.Should().Be(HttpStatusCode.OK);

        return (account, repo with { IsLinked = true });
    }

    private async Task<List<RepoRef>> GetRepos(int connectionId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var repos = await context.SourceControlRepos
            .AsNoTracking()
            .Where(repo => repo.ConnectionId == connectionId)
            .OrderBy(repo => repo.FullName)
            .Select(repo => new RepoRef(repo.Id, repo.ConnectionId, repo.ExternalId, repo.IsLinked))
            .ToListAsync(TestContext.Current.CancellationToken);

        return repos;
    }

    private async Task<(int Id, string SystemId)> AnyTask(string workspace = "netptune")
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var task = await context.ProjectTasks
            .AsNoTracking()
            .Where(task => !task.IsDeleted && task.Workspace!.Slug == workspace)
            .OrderBy(task => task.Id)
            .Select(task => new { task.Id, task.Project!.Key, task.ProjectScopeId })
            .FirstAsync(TestContext.Current.CancellationToken);

        return (task.Id, $"{task.Key}-{task.ProjectScopeId}");
    }

    private async Task ProcessPullRequest(
        string account,
        string repoExternalId,
        string number,
        string title,
        string state,
        string? headSha = null,
        DateTimeOffset? updatedAt = null,
        string? author = null,
        string? authorId = null)
    {
        var sha = headSha is null ? string.Empty : $$""", "sha": "{{headSha}}" """;
        var updated = updatedAt is null ? string.Empty : $$""", "updatedAt": "{{updatedAt:O}}" """;
        var authorName = author is null ? string.Empty : $$""", "author": "{{author}}" """;
        var authorAccount = authorId is null ? string.Empty : $$""", "authorId": "{{authorId}}" """;
        var body = $$"""{ "account": "{{account}}", "repo": "{{repoExternalId}}", "number": "{{number}}", "title": "{{title}}", "state": "{{state}}"{{sha}}{{updated}}{{authorName}}{{authorAccount}} }""";

        await Process(account, TestSourceControlProvider.PullRequestEvent, body);
    }

    private async Task ProcessCheckRun(
        string account,
        string repoExternalId,
        string runId,
        string name,
        string sha,
        string status,
        string? conclusion)
    {
        var conclusionField = conclusion is null ? string.Empty : $$""", "conclusion": "{{conclusion}}" """;
        var body = $$"""{ "account": "{{account}}", "repo": "{{repoExternalId}}", "id": "{{runId}}", "name": "{{name}}", "sha": "{{sha}}", "status": "{{status}}"{{conclusionField}} }""";

        await Process(account, TestSourceControlProvider.CheckRunEvent, body);
    }

    private async Task Process(string account, string eventName, string body)
    {
        await PostDelivery(account, eventName, body);

        using var scope = Fixture.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IScmWebhookProcessor>();
        var pending = await PendingDeliveries(account);

        foreach (var deliveryId in pending)
        {
            await processor.Process(deliveryId, TestContext.Current.CancellationToken);
        }
    }

    private async Task PostDelivery(string account, string eventName, string body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl);

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.Add(TestSourceControlProvider.DeliveryHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TestSourceControlProvider.EventHeader, eventName);
        request.Headers.Add(TestSourceControlProvider.SignatureHeader, TestSourceControlProvider.ValidSignature);

        var response = await Anonymous.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    private async Task<List<TaskCodeLink>> LinksFor(int taskId, int repoId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var links = await context.TaskCodeLinks
            .AsNoTracking()
            .Where(link => link.TaskId == taskId && link.RepoId == repoId)
            .ToListAsync(TestContext.Current.CancellationToken);

        return links;
    }

    private sealed record RepoRef(int Id, int ConnectionId, string ExternalId, bool IsLinked);
}
