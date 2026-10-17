using System.Text;
using System.Text.Json;

using FluentAssertions;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public sealed class GitHubWebhookReaderTests : IDisposable
{
    private readonly GitHubTestHarness Harness = new();

    [Fact]
    public void Verify_ShouldAcceptABodySignedWithTheWebhookSecret()
    {
        var request = GitHubTestHarness.CreateWebhookRequest("push", GitHubWebhookPayloads.Push);

        var verification = Harness.WebhookReader.Verify(request, null);

        verification.Should().Be(ScmWebhookVerification.Verified);
    }

    [Fact]
    public void Verify_ShouldRejectABodySignedWithAnotherSecret()
    {
        var request = GitHubTestHarness.CreateWebhookRequest("push", GitHubWebhookPayloads.Push, secret: "someone-else");

        var verification = Harness.WebhookReader.Verify(request, null);

        verification.Should().Be(ScmWebhookVerification.InvalidSignature);
    }

    [Fact]
    public void Verify_ShouldRejectABodyChangedAfterSigning()
    {
        var signed = GitHubTestHarness.CreateWebhookRequest("push", GitHubWebhookPayloads.Push);
        var tamperedBody = Encoding.UTF8.GetBytes(GitHubWebhookPayloads.Push.Replace("netp-564", "netp-999"));
        var tampered = signed with { Body = tamperedBody };

        var verification = Harness.WebhookReader.Verify(tampered, null);

        verification.Should().Be(ScmWebhookVerification.InvalidSignature);
    }

    [Fact]
    public void Verify_ShouldRejectAnUnsignedBody()
    {
        var request = GitHubTestHarness.CreateWebhookRequest("push", GitHubWebhookPayloads.Push, secret: null);

        var verification = Harness.WebhookReader.Verify(request, null);

        verification.Should().Be(ScmWebhookVerification.MissingSignature);
    }

    [Fact]
    public void Verify_ShouldRejectAMalformedSignature()
    {
        var signed = GitHubTestHarness.CreateWebhookRequest("push", GitHubWebhookPayloads.Push);

        var headers = new Dictionary<string, string>(signed.Headers)
        {
            ["X-Hub-Signature-256"] = "sha256=not-hex",
        };

        var verification = Harness.WebhookReader.Verify(signed with { Headers = headers }, null);

        verification.Should().Be(ScmWebhookVerification.InvalidSignature);
    }

    [Fact]
    public void Verify_ShouldReportAMissingSecretAsNotConfigured()
    {
        using var harness = new GitHubTestHarness(options => options.WebhookSecret = string.Empty);
        var request = GitHubTestHarness.CreateWebhookRequest("push", GitHubWebhookPayloads.Push);

        var verification = harness.WebhookReader.Verify(request, null);

        verification.Should().Be(ScmWebhookVerification.NotConfigured);
    }

    [Fact]
    public void ReadEnvelope_ShouldExtractTheRoutingFacts()
    {
        var request = GitHubTestHarness.CreateWebhookRequest("pull_request", GitHubWebhookPayloads.PullRequest("closed", merged: true));

        var envelope = Harness.WebhookReader.ReadEnvelope(request);

        envelope.Should().NotBeNull();
        envelope!.Provider.Should().Be(SourceControlProvider.GitHub);
        envelope.DeliveryId.Should().Be("delivery-1");
        envelope.EventName.Should().Be("pull_request");
        envelope.Action.Should().Be("closed");
        envelope.ExternalAccountId.Should().Be(GitHubTestHarness.InstallationId);
        envelope.ReceivedAt.Should().Be(Harness.Clock.GetUtcNow());
    }

    [Fact]
    public void ReadEnvelope_ShouldReturnNullWithoutDeliveryHeaders()
    {
        var request = new ScmWebhookRequest
        {
            Body = Encoding.UTF8.GetBytes(GitHubWebhookPayloads.Push),
            Headers = new Dictionary<string, string>(),
        };

        var envelope = Harness.WebhookReader.ReadEnvelope(request);

        envelope.Should().BeNull();
    }

    [Fact]
    public void ReadEnvelope_ShouldReturnNullForABodyThatIsNotJson()
    {
        var request = GitHubTestHarness.CreateWebhookRequest("push", "<html>nope</html>");

        var envelope = Harness.WebhookReader.ReadEnvelope(request);

        envelope.Should().BeNull();
    }

    [Fact]
    public void Normalise_ShouldTurnAPushIntoCommitsPushed()
    {
        var events = Normalise("push", GitHubWebhookPayloads.Push);

        var pushed = events.Should().ContainSingle().Which.Should().BeOfType<ScmCommitsPushed>().Subject;

        pushed.Branch.Should().Be("feature/netp-564-search");
        pushed.Repository.Should().Be(new ScmRepositoryRef("4242", "acme", "netptune"));
        pushed.ConnectionId.Should().Be(7);
        pushed.WorkspaceId.Should().Be(3);
        pushed.Actor!.Username.Should().Be("octocat");
        pushed.Commits.Select(commit => commit.Sha).Should().Equal(
            "2222222222222222222222222222222222222222",
            "3333333333333333333333333333333333333333");
        pushed.Commits[0].Message.Should().Be("Add search index netp-564");
        pushed.Commits[0].Author.Email.Should().Be("octo@example.com");
        pushed.OccurredAt.Should().Be(new DateTimeOffset(2026, 10, 16, 8, 20, 0, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void Normalise_ShouldGiveARedeliveryTheSameEventId()
    {
        var first = Normalise("push", GitHubWebhookPayloads.Push, "delivery-1");
        var redelivered = Normalise("push", GitHubWebhookPayloads.Push, "delivery-1");
        var another = Normalise("push", GitHubWebhookPayloads.Push, "delivery-2");

        redelivered[0].EventId.Should().Be(first[0].EventId);
        another[0].EventId.Should().NotBe(first[0].EventId);
    }

    [Fact]
    public void Normalise_ShouldIgnorePushesThatCarryNoBranchCommits()
    {
        Normalise("push", GitHubWebhookPayloads.PushToTag).Should().BeEmpty();
        Normalise("push", GitHubWebhookPayloads.PushDeletingBranch).Should().BeEmpty();
    }

    [Fact]
    public void Normalise_ShouldTurnABranchCreateIntoBranchCreated()
    {
        var events = Normalise("create", GitHubWebhookPayloads.CreateBranch);

        var created = events.Should().ContainSingle().Which.Should().BeOfType<ScmBranchCreated>().Subject;

        created.Branch.Should().Be("feature/netp-564-search");
        created.WebUrl.Should().Be("https://github.com/acme/netptune/tree/feature/netp-564-search");
    }

    [Fact]
    public void Normalise_ShouldIgnoreTagCreation()
    {
        Normalise("create", GitHubWebhookPayloads.CreateTag).Should().BeEmpty();
    }

    [Fact]
    public void Normalise_ShouldTreatAClosedAndMergedPullRequestAsMerged()
    {
        var events = Normalise("pull_request", GitHubWebhookPayloads.PullRequest("closed", merged: true));

        var changed = events.Should().ContainSingle().Which.Should().BeOfType<ScmPullRequestChanged>().Subject;

        changed.Action.Should().Be(ScmPullRequestAction.Merged);
        changed.PullRequest.State.Should().Be(ScmPullRequestState.Merged);
        changed.PullRequest.Number.Should().Be("42");
        changed.PullRequest.SourceBranch.Should().Be("feature/netp-564-search");
        changed.PullRequest.HeadSha.Should().Be("3333333333333333333333333333333333333333");
        changed.PullRequest.TargetBranch.Should().Be("main");
        changed.PullRequest.MergedAt.Should().Be(new DateTimeOffset(2026, 10, 16, 9, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Normalise_ShouldTreatAClosedUnmergedPullRequestAsClosed()
    {
        var events = Normalise("pull_request", GitHubWebhookPayloads.PullRequest("closed", merged: false));

        var changed = events.Should().ContainSingle().Which.Should().BeOfType<ScmPullRequestChanged>().Subject;

        changed.Action.Should().Be(ScmPullRequestAction.Closed);
        changed.PullRequest.State.Should().Be(ScmPullRequestState.Closed);
    }

    [Fact]
    public void Normalise_ShouldIgnorePullRequestActionsNetptuneDoesNotUse()
    {
        Normalise("pull_request", GitHubWebhookPayloads.PullRequest("labeled", merged: false, state: "open")).Should().BeEmpty();
    }

    [Fact]
    public void Normalise_ShouldTurnACompletedCheckRunIntoCheckRunChanged()
    {
        var events = Normalise("check_run", GitHubWebhookPayloads.CheckRun("completed"));

        var completed = events.Should().ContainSingle().Which.Should().BeOfType<ScmCheckRunChanged>().Subject;

        completed.CheckRun.Conclusion.Should().Be(ScmCheckRunConclusion.Failure);
        completed.CheckRun.Status.Should().Be(ScmCheckRunStatus.Completed);
        completed.PullRequestNumbers.Should().Equal("42");
    }

    [Fact]
    public void Normalise_ShouldTurnACreatedCheckRunIntoAQueuedCheckRun()
    {
        var events = Normalise("check_run", GitHubWebhookPayloads.CheckRun("created", status: "queued", conclusion: "null"));

        var created = events.Should().ContainSingle().Which.Should().BeOfType<ScmCheckRunChanged>().Subject;

        created.CheckRun.Status.Should().Be(ScmCheckRunStatus.Queued);
        created.CheckRun.Conclusion.Should().BeNull();
    }

    [Fact]
    public void Normalise_ShouldIgnoreARerequestedCheckRun()
    {
        Normalise("check_run", GitHubWebhookPayloads.CheckRun("rerequested")).Should().BeEmpty();
    }

    [Fact]
    public void Normalise_ShouldTreatAnUninstallAsARevokedConnection()
    {
        var events = Normalise("installation", GitHubWebhookPayloads.InstallationDeleted);

        var changed = events.Should().ContainSingle().Which.Should().BeOfType<ScmConnectionChanged>().Subject;

        changed.Action.Should().Be(ScmConnectionAction.Revoked);
    }

    [Fact]
    public void Normalise_ShouldListRepositoriesAddedToTheInstallation()
    {
        var events = Normalise("installation_repositories", GitHubWebhookPayloads.InstallationRepositoriesAdded);

        var changed = events.Should().ContainSingle().Which.Should().BeOfType<ScmConnectionChanged>().Subject;

        changed.Action.Should().Be(ScmConnectionAction.RepositoriesAdded);
        changed.Repositories.Should().Equal(new ScmRepositoryRef("5150", "acme", "website"));
    }

    [Fact]
    public void Normalise_ShouldIgnoreEventsNetptuneDoesNotSubscribeTo()
    {
        Normalise("star", GitHubWebhookPayloads.Star).Should().BeEmpty();
    }

    [Fact]
    public void Normalise_ShouldFailLoudlyOnAVerifiedBodyThatDoesNotParse()
    {
        var request = GitHubTestHarness.CreateWebhookRequest("push", GitHubWebhookPayloads.Push);
        var envelope = Harness.WebhookReader.ReadEnvelope(request)!;
        var broken = envelope with { Body = Encoding.UTF8.GetBytes("{ \"ref\": ") };

        var normalise = () => Harness.WebhookReader.Normalise(broken, GitHubTestHarness.CreateConnection());

        normalise.Should().Throw<JsonException>();
    }

    public void Dispose()
    {
        Harness.Dispose();
    }

    private IReadOnlyList<ScmEvent> Normalise(string eventName, string json, string deliveryId = "delivery-1")
    {
        var request = GitHubTestHarness.CreateWebhookRequest(eventName, json, deliveryId);
        var envelope = Harness.WebhookReader.ReadEnvelope(request)!;
        var connection = GitHubTestHarness.CreateConnection();

        return Harness.WebhookReader.Normalise(envelope, connection);
    }
}
