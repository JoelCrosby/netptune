using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.Tags;
using Netptune.Core.ViewModels.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class TaskReviewEndpointTests
{
    private const string Description = "Add CSV export to the reports page.\n\nIt should be fast.";
    private const string MeasurableTarget = "It should export 10,000 rows in under 2 seconds.";
    private const string EmptyReview = """{"summary":"Ready.","findings":[]}""";
    private const string UserCredentials = "api/ai/credentials";
    private const string WorkspaceCredentials = "api/ai/workspace-credentials";

    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public TaskReviewEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Run_ShouldBeForbidden_WithoutAssistantUse()
    {
        var task = await CreateTask();
        var member = SeedData.Users.ElementAt(2);

        await using var revoked = await MemberPermissions.Revoke(Client, member.Id, NetptunePermissions.Assistant.Use);

        var memberClient = Fixture.CreateNetptuneClientAs(member.Email);
        var response = await memberClient.PostAsync($"api/tasks/{task.Id}/reviews", null, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Run_ShouldStoreTheValidFindings_AndShowThemAsTheLatestReview()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            Script().Enqueue(new AiChatTurn { Text = ReviewReply(), Usage = new AiUsage { OutputTokens = 40 } });

            var frames = await Run(task.Id);
            var completed = frames.Last();

            completed.GetProperty("type").GetInt32().Should().Be((int)AiReviewStreamEventType.Completed);

            var latest = await Latest(task.Id);
            var findings = latest.Current!.Findings;

            latest.Readiness.Should().Be(AiReviewReadiness.NeedsWork);
            latest.IsStale.Should().BeFalse();
            latest.OpenCount.Should().Be(2);
            latest.Current.Run.Model.Should().Be(AiModels.AnthropicReviewModel);
            findings.Select(finding => finding.Title).Should().Equal("Speed is not measurable", "No error cases");
            findings[0].Suggestion!.Value.GetProperty("find").GetString().Should().Be("It should be fast.");
            findings[1].Suggestion.Should().BeNull();

            var sent = Script().Requests.Should().ContainSingle().Subject;

            sent.Messages.Single().Text.Should().Contain("<description field=\"description\">");

            var reviewEvents = await ReviewEvents(task.Id);

            reviewEvents.Should().ContainSingle();
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Run_ShouldOfferEveryReadTool_AndKeepWhatItCalled_AndTheTasksItFound()
    {
        var task = await CreateTask();
        var overlapping = await CreateTask($"Quarterly invoice archive {Guid.NewGuid():N}");

        await SaveCredential();

        try
        {
            var lookup = new AiChatTurn
            {
                ToolCalls =
                [
                    new AiToolCall
                    {
                        Id = "call-1",
                        Name = "get_task",
                        Arguments = JsonDocument.Parse($$"""{"systemId":"{{overlapping.SystemId}}"}"""),
                    },
                ],
                Usage = new AiUsage { InputTokens = 100, OutputTokens = 30 },
            };

            Script().Enqueue(lookup);
            Script().Enqueue(new AiChatTurn
            {
                Text = OverlapReply(overlapping.SystemId),
                Usage = new AiUsage { InputTokens = 200, OutputTokens = 40 },
            });

            var frames = await Run(task.Id);
            var types = frames.Select(frame => frame.GetProperty("type").GetInt32()).ToList();

            types.Should().Contain((int)AiReviewStreamEventType.Tool);
            types.Last().Should().Be((int)AiReviewStreamEventType.Completed);

            var offered = Script().Requests[0].Tools.Select(tool => tool.Name).ToList();

            offered.Should().BeEquivalentTo(ReadToolNames());
            offered.Should().Contain(["read_repository_file", "search_repository_code", "get_task", "web_search"]);
            Script().Requests.Should().HaveCount(2);
            Script().Requests[0].Messages.Single().Text.Should().NotContain(overlapping.SystemId);

            var latest = await Latest(task.Id);
            var toolCall = latest.Current!.Run.ToolCalls.Should().ContainSingle().Subject;
            var finding = latest.Current.Findings.Should().ContainSingle().Subject;

            toolCall.Name.Should().Be("get_task");
            toolCall.IsError.Should().BeFalse();
            finding.References.Should().ContainSingle().Which.EntityId.Should().Be(overlapping.Id);
            finding.Suggestion!.Value.GetProperty("taskId").GetInt32().Should().Be(overlapping.Id);

            var usage = await LatestReviewUsage();

            usage.InputTokens.Should().Be(300);
            usage.OutputTokens.Should().Be(70);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Run_ShouldListTheLinkedCode_OnlyForThoseWhoCanReadSourceControl()
    {
        var task = await CreateTask();
        var member = SeedData.Users.ElementAt(2);
        var connectionId = await SeedPullRequestLink(task);

        await SaveCredential(WorkspaceCredentials);

        try
        {
            Script().Enqueue(new AiChatTurn { Text = EmptyReview });
            await Run(task.Id);

            var ownerRequest = Script().Requests.Last();

            await using (await MemberPermissions.Revoke(Client, member.Id, NetptunePermissions.SourceControl.Read))
            {
                var memberClient = Fixture.CreateNetptuneClientAs(member.Email);

                Script().Enqueue(new AiChatTurn { Text = EmptyReview });
                await Run(task.Id, memberClient);
            }

            var memberRequest = Script().Requests.Last();
            var memberTools = memberRequest.Tools.Select(tool => tool.Name);

            ownerRequest.Messages.Single().Text.Should().Contain("- pull request acme/web 18 (Open): Fix the redirect");
            memberRequest.Messages.Single().Text.Should().NotContain("Fix the redirect");
            memberTools.Should().NotContain("read_repository_file").And.Contain("get_task");
        }
        finally
        {
            await DeleteCredentials(WorkspaceCredentials);
            await RemoveSourceControl(connectionId);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Run_ShouldStoreAFailedRun_WithItsToolCalls_WhenItRunsOutOfToolRounds()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            for (var round = 1; round <= 12; round++)
            {
                Script().Enqueue(new AiChatTurn
                {
                    ToolCalls =
                    [
                        new AiToolCall
                        {
                            Id = $"call-{round}",
                            Name = "get_task",
                            Arguments = JsonDocument.Parse($$"""{"systemId":"{{task.SystemId}}"}"""),
                        },
                    ],
                });
            }

            var frames = await Run(task.Id);
            var failed = frames.Last();

            failed.GetProperty("type").GetInt32().Should().Be((int)AiReviewStreamEventType.Failed);
            failed.GetProperty("message").GetString().Should().StartWith("The review stopped after 12 rounds");

            var latest = await Latest(task.Id);

            latest.LatestFailure!.ErrorCode.Should().Be(AiReviewErrorCodes.ToolRounds);
            latest.LatestFailure.ToolCalls.Should().HaveCount(12);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Run_ShouldStoreAFailedRun_WhenTheReplyIsUnreadable()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var unreadable = new AiChatTurn
            {
                Text = "I could not review this.",
                Usage = new AiUsage { OutputTokens = 5 },
            };

            Script().Enqueue(unreadable);

            var frames = await Run(task.Id);
            var failed = frames.Last();

            failed.GetProperty("type").GetInt32().Should().Be((int)AiReviewStreamEventType.Failed);
            failed.GetProperty("runId").GetInt32().Should().BePositive();

            var latest = await Latest(task.Id);

            latest.Current.Should().BeNull();
            latest.LatestFailure!.ErrorCode.Should().Be(AiReviewErrorCodes.UnreadableReply);
            latest.Readiness.Should().Be(AiReviewReadiness.Unreviewed);

            var stored = await GetRun(failed.GetProperty("runId").GetInt32());

            stored.FailureDetail.Should().Contain("no complete JSON object");
            stored.RawReply.Should().Be("I could not review this.");
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Theory]
    [InlineData("", "end_turn", AiReviewErrorCodes.EmptyReply, "The assistant replied without any review text. Try again.")]
    [InlineData("I can't help with that.", "refusal", AiReviewErrorCodes.Refused, "The assistant declined to review this item.")]
    public async Task Run_ShouldStoreWhyTheReplyHadNoReview(
        string text,
        string finishReason,
        string errorCode,
        string messageStart)
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var turn = new AiChatTurn
            {
                Text = text,
                FinishReason = finishReason,
                Usage = new AiUsage { OutputTokens = 5 },
            };

            Script().Enqueue(turn);

            var frames = await Run(task.Id);
            var failed = frames.Last();

            failed.GetProperty("message").GetString().Should().StartWith(messageStart);

            var stored = await GetRun(failed.GetProperty("runId").GetInt32());

            stored.ErrorCode.Should().Be(errorCode);
            stored.FailureDetail.Should().Be($"Finish reason: {finishReason}.");
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Run_ShouldUseTheReviewModelSavedOnTheConnection()
    {
        var task = await CreateTask();

        await SaveCredential(reviewModel: "claude-opus-5");

        try
        {
            Script().Enqueue(new AiChatTurn { Text = EmptyReview });

            await Run(task.Id);

            Script().Requests.Should().ContainSingle().Which.Model.Should().Be("claude-opus-5");
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Run_ShouldRemindTheReviewer_OfFindingsDismissedInAnyEarlierRun()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var finding = await ReviewWithOneEdit(task.Id);

            await Decide(finding, new { decision = AiReviewDecision.Dismiss, reason = "Out of scope." });

            Script().Enqueue(new AiChatTurn { Text = EmptyReview });
            await Run(task.Id);
            Script().Enqueue(new AiChatTurn { Text = EmptyReview });
            await Run(task.Id);

            var thirdPrompt = Script().Requests.Last().Messages.Single().Text;

            thirdPrompt.Should().Contain("- testability: Speed is not measurable (reason: Out of scope.)");
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Review_ShouldTreatADeletedTaskAsMissing()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var deleted = await Client.DeleteAsync($"api/tasks/{task.Id}", CancellationToken);

            deleted.EnsureSuccessStatusCode();

            var frames = await Run(task.Id);
            var latest = await Client.GetAsync($"api/tasks/{task.Id}/reviews/latest", CancellationToken);

            frames.Last().GetProperty("type").GetInt32().Should().Be((int)AiReviewStreamEventType.Failed);
            latest.StatusCode.Should().Be(HttpStatusCode.NotFound);
            Script().Requests.Should().BeEmpty();
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Latest_ShouldBeStale_OnceTheDescriptionChanges()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            Script().Enqueue(new AiChatTurn { Text = EmptyReview });

            await Run(task.Id);

            var beforeEdit = await Latest(task.Id);

            beforeEdit.Readiness.Should().Be(AiReviewReadiness.Ready);

            await UpdateDescription(task.Id, "Something else entirely.");

            var afterEdit = await Latest(task.Id);

            afterEdit.IsStale.Should().BeTrue();
            afterEdit.Readiness.Should().Be(AiReviewReadiness.Stale);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task UpdateModels_ShouldChangeTheReviewModel_WithoutTheKey()
    {
        await SaveCredential();

        try
        {
            var credentials = await Credentials(UserCredentials);
            var url = $"{UserCredentials}/{credentials.Single().Id}";
            var update = new { model = (string?)null, reviewModel = "claude-haiku-4-5" };
            var response = await Client.PatchAsJsonAsync(url, update, CancellationToken);
            var updated = await Credentials(UserCredentials);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            updated.Single().ReviewModel.Should().Be("claude-haiku-4-5");

            var unsupported = new { model = (string?)null, reviewModel = "gpt-5.6-terra" };
            var rejected = await Client.PatchAsJsonAsync(url, unsupported, CancellationToken);

            rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
        }
    }

    [Fact]
    public async Task UpdateWorkspaceModels_ShouldChangeTheReviewModel_WithoutTheKey()
    {
        await SaveCredential(WorkspaceCredentials);

        try
        {
            var credentials = await Credentials(WorkspaceCredentials);
            var credential = credentials.Single(item => item.Provider == AiProvider.Anthropic);
            var url = $"{WorkspaceCredentials}/{credential.Id}";
            var update = new { model = (string?)null, reviewModel = "claude-haiku-4-5" };
            var response = await Client.PatchAsJsonAsync(url, update, CancellationToken);
            var updated = await Credentials(WorkspaceCredentials);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            updated.Single(item => item.Id == credential.Id).ReviewModel.Should().Be("claude-haiku-4-5");
        }
        finally
        {
            await DeleteCredentials(WorkspaceCredentials);
        }
    }

    [Fact]
    public async Task Accept_ShouldApplyTheEdit_AsTheAssistant_WithoutMarkingTheReviewStale()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var finding = await ReviewWithOneEdit(task.Id);
            var response = await Decide(finding, new { decision = AiReviewDecision.Accept });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var description = await GetDescription(task.Id);
            var latest = await Latest(task.Id);
            var accepted = latest.Current!.Findings.Single();

            description.Should().Be($"Add CSV export to the reports page.\n\n{MeasurableTarget}");
            accepted.Status.Should().Be(AiReviewFindingStatus.Accepted);
            accepted.AppliedChange!.Value.GetProperty("before").GetString().Should().Be(Description);
            latest.IsStale.Should().BeFalse();
            latest.Readiness.Should().Be(AiReviewReadiness.Ready);

            var edit = await DescriptionEvent(task.Id);
            var eventKeys = await EventKeysFor(task.Id);

            edit.OriginType.Should().Be(EventOriginType.Assistant);
            edit.Agent.Should().Be(AiModels.AnthropicReviewModel);
            eventKeys.Should().Contain(EventKeys.AssistantReviewFindingAccepted);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Accept_ShouldUseTheReviewersEditedText_WhenTheyChangedIt()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var finding = await ReviewWithOneEdit(task.Id);
            var request = new { decision = AiReviewDecision.Accept, replace = "It should export in under 5 seconds." };
            var response = await Decide(finding, request);
            var description = await GetDescription(task.Id);
            var latest = await Latest(task.Id);
            var appliedChange = latest.Current!.Findings.Single().AppliedChange!.Value;

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            description.Should().EndWith("It should export in under 5 seconds.");
            appliedChange.GetProperty("edited").GetBoolean().Should().BeTrue();
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Accept_ShouldRefuse_WhenThePassageIsNoLongerInTheDescription_OrItWasAlreadyAccepted()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var finding = await ReviewWithOneEdit(task.Id);

            await UpdateDescription(task.Id, "Rewritten by hand.");

            var changed = await Decide(finding, new { decision = AiReviewDecision.Accept });
            var untouched = await GetDescription(task.Id);

            changed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            untouched.Should().Be("Rewritten by hand.");

            var appended = await ReviewWithOneEdit(task.Id, find: null);
            var first = await Decide(appended, new { decision = AiReviewDecision.Accept });
            var second = await Decide(appended, new { decision = AiReviewDecision.Accept });
            var description = await GetDescription(task.Id);

            first.StatusCode.Should().Be(HttpStatusCode.OK);
            second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            description.Should().Be($"Rewritten by hand.\n\n{MeasurableTarget}");
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Dismiss_ShouldStoreTheReason_AndReopenShouldClearIt()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var finding = await ReviewWithOneEdit(task.Id);
            var request = new { decision = AiReviewDecision.Dismiss, reason = "Speed is out of scope." };
            var dismissed = await Decide(finding, request);

            dismissed.StatusCode.Should().Be(HttpStatusCode.OK);

            var afterDismiss = await Latest(task.Id);
            var dismissedFinding = afterDismiss.Current!.Findings.Single();

            dismissedFinding.Status.Should().Be(AiReviewFindingStatus.Dismissed);
            dismissedFinding.DismissReason.Should().Be("Speed is out of scope.");

            var reopened = await Decide(finding, new { decision = AiReviewDecision.Reopen });
            var afterReopen = await Latest(task.Id);
            var eventKeys = await EventKeysFor(task.Id);
            string[] decisionKeys =
            [
                EventKeys.AssistantReviewFindingDismissed,
                EventKeys.AssistantReviewFindingReopened,
            ];

            reopened.StatusCode.Should().Be(HttpStatusCode.OK);
            afterReopen.Current!.Findings.Single().Status.Should().Be(AiReviewFindingStatus.Open);
            eventKeys.Should().Contain(decisionKeys);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Reopen_ShouldRevertTheEdit_OnlyWhileTheDescriptionIsUnchanged()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var finding = await ReviewWithOneEdit(task.Id);

            await Decide(finding, new { decision = AiReviewDecision.Accept });

            var reverted = await Decide(finding, new { decision = AiReviewDecision.Reopen, revert = true });
            var revertedDescription = await GetDescription(task.Id);
            var afterRevert = await Latest(task.Id);

            reverted.StatusCode.Should().Be(HttpStatusCode.OK);
            revertedDescription.Should().Be(Description);
            afterRevert.IsStale.Should().BeFalse();

            await Decide(finding, new { decision = AiReviewDecision.Accept });
            await UpdateDescription(task.Id, "Edited after accepting.");

            var refused = await Decide(finding, new { decision = AiReviewDecision.Reopen, revert = true });
            var description = await GetDescription(task.Id);

            refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            description.Should().Be("Edited after accepting.");
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Decide_ShouldRefuse_OnceANewerReviewHasReplacedTheRun()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var older = await ReviewWithOneEdit(task.Id);

            await ReviewWithOneEdit(task.Id);

            var response = await Decide(older, new { decision = AiReviewDecision.Dismiss });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Decide_ShouldBeForbidden_WithoutTasksUpdate()
    {
        var task = await CreateTask();
        var member = SeedData.Users.ElementAt(2);

        await SaveCredential();

        try
        {
            var finding = await ReviewWithOneEdit(task.Id);

            const string taskUpdate = NetptunePermissions.Tasks.Update;

            await using var revoked = await MemberPermissions.Revoke(Client, member.Id, taskUpdate);

            var memberClient = Fixture.CreateNetptuneClientAs(member.Email);
            var dismissed = await Decide(finding, new { decision = AiReviewDecision.Dismiss }, memberClient);
            var accepted = await Decide(finding, new { decision = AiReviewDecision.Accept }, memberClient);
            var description = await GetDescription(task.Id);

            dismissed.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            accepted.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            description.Should().Be(Description);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Accept_ShouldCreateTheKeptSplitTasks_AsChildren_WithTheirProvenance()
    {
        var task = await CreateTask();
        var childName = $"CSV export {Guid.NewGuid():N}";

        await SaveCredential();

        try
        {
            var finding = await ReviewWithSuggestion(task.Id, "scope", SplitSuggestion());
            var decision = new
            {
                decision = AiReviewDecision.Accept,
                splitTasks = new[] { new { name = childName, description = "Only the CSV part." } },
            };

            var response = await Decide(finding, decision);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await using var scope = Fixture.Services.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var child = await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(item => item.Name == childName && item.ProjectId == 1, CancellationToken);
            var isLinkedToParent = await context.ProjectTaskRelations.AnyAsync(
                relation => relation.SourceTaskId == task.Id && relation.TargetTaskId == child.Id,
                CancellationToken);

            isLinkedToParent.Should().BeTrue();

            var childReview = await Latest(child.Id);

            childReview.Origin!.ParentTaskId.Should().Be(task.Id);
            childReview.Origin.FindingTitle.Should().Be("Suggested change");
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Accept_ShouldCreateNoTasks_WhenAKeptSplitTaskIsTooLong()
    {
        var task = await CreateTask();
        var prefix = $"Split {Guid.NewGuid():N}";

        await SaveCredential();

        try
        {
            var finding = await ReviewWithSuggestion(task.Id, "scope", SplitSuggestion());
            var decision = new
            {
                decision = AiReviewDecision.Accept,
                splitTasks = new[]
                {
                    new { name = $"{prefix} first", description = "Fine." },
                    new { name = $"{prefix} {new string('x', 200)}", description = "Too long." },
                },
            };

            var response = await Decide(finding, decision);
            var createdCount = await CountTasksNamed(prefix);
            var latest = await Latest(task.Id);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            createdCount.Should().Be(0);
            latest.Current!.Findings.Single().Status.Should().Be(AiReviewFindingStatus.Open);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Reopen_ShouldRefuse_OnceASplitHasBeenApplied()
    {
        var task = await CreateTask();

        await SaveCredential();

        try
        {
            var finding = await ReviewWithSuggestion(task.Id, "scope", SplitSuggestion());
            var accepted = await Decide(finding, new { decision = AiReviewDecision.Accept });
            var reopened = await Decide(finding, new { decision = AiReviewDecision.Reopen });
            var latest = await Latest(task.Id);

            accepted.StatusCode.Should().Be(HttpStatusCode.OK);
            reopened.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            latest.Current!.Findings.Single().Status.Should().Be(AiReviewFindingStatus.Accepted);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Accept_ShouldLinkTheTasks_ForALinkSuggestion()
    {
        var task = await CreateTask();
        var duplicate = await CreateTask();
        var link = new AiReviewLinkSuggestion
        {
            SystemId = duplicate.SystemId,
            TaskId = duplicate.Id,
            Category = RelationCategory.Duplicate,
        };

        var finding = await SeedFinding(task.Id, AiReviewFindingCategory.Overlap, link);
        var response = await Decide(finding, new { decision = AiReviewDecision.Accept });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var isLinked = await context.ProjectTaskRelations.AnyAsync(
            relation => relation.SourceTaskId == task.Id && relation.TargetTaskId == duplicate.Id,
            CancellationToken);
        var latest = await Latest(task.Id);

        isLinked.Should().BeTrue();
        latest.Current!.Findings.Single().Status.Should().Be(AiReviewFindingStatus.Accepted);
    }

    [Fact]
    public async Task Accept_ShouldCreateANewTag_OnlyForSomeoneAllowedToCreateTags()
    {
        var task = await CreateTask();
        var newTag = $"Review tag {Guid.NewGuid():N}"[..20];
        var member = SeedData.Users.ElementAt(2);

        await SaveCredential();

        try
        {
            var metadata = new { kind = "metadata", tags = new[] { newTag }, priority = "high" };
            var finding = await ReviewWithSuggestion(task.Id, "gap", metadata);
            var accept = new { decision = AiReviewDecision.Accept };

            const string tagCreate = NetptunePermissions.Tags.Create;

            await using (var revoked = await MemberPermissions.Revoke(Client, member.Id, tagCreate))
            {
                var memberClient = Fixture.CreateNetptuneClientAs(member.Email);
                var refused = await Decide(finding, accept, memberClient);

                refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            var accepted = await Decide(finding, accept);

            accepted.StatusCode.Should().Be(HttpStatusCode.OK);

            await using var scope = Fixture.Services.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var tagNames = await context.ProjectTaskTags
                .Where(link => link.ProjectTaskId == task.Id)
                .Select(link => link.Tag!.Name)
                .ToListAsync(CancellationToken);
            var priority = await context.ProjectTasks
                .Where(item => item.Id == task.Id)
                .Select(item => item.Priority)
                .SingleAsync(CancellationToken);

            tagNames.Should().Contain(TagNames.Normalize(newTag));
            priority.Should().Be(TaskPriority.High);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    [Fact]
    public async Task Accept_ShouldRefuse_MoreNewTagsThanASuggestionMayCreate()
    {
        var task = await CreateTask();
        var prefix = $"Tag {Guid.NewGuid():N}"[..16];

        await SaveCredential();

        try
        {
            var metadata = new { kind = "metadata", tags = new[] { $"{prefix} a" }, priority = "high" };
            var finding = await ReviewWithSuggestion(task.Id, "gap", metadata);
            var tags = Enumerable.Range(1, 4).Select(index => $"{prefix} {index}").ToArray();
            var response = await Decide(finding, new { decision = AiReviewDecision.Accept, tags });
            var createdCount = await CountTagsNamed(prefix);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            createdCount.Should().Be(0);
        }
        finally
        {
            await DeleteCredentials(UserCredentials);
            Script().Reset();
        }
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string ReviewReply()
    {
        var reply = new
        {
            summary = "Needs a measurable target.",
            findings = new object[]
            {
                new
                {
                    category = "testability",
                    title = "Speed is not measurable",
                    explanation = "Fast has no threshold.",
                    quote = "It should be fast.",
                    suggestion = new
                    {
                        kind = "edit",
                        field = "description",
                        find = "It should be fast.",
                        replace = MeasurableTarget,
                    },
                    relatedTasks = Array.Empty<string>(),
                },
                new
                {
                    category = "gap",
                    title = "No error cases",
                    explanation = "Nothing says what happens when the export fails.",
                    quote = (string?)null,
                    suggestion = new { kind = "edit", field = "description", find = "Not in the text", replace = "x" },
                    relatedTasks = new[] { "NOPE-1" },
                },
                new
                {
                    category = "vibes",
                    title = "Unknown category",
                    explanation = "Dropped.",
                    quote = (string?)null,
                    suggestion = (object?)null,
                    relatedTasks = Array.Empty<string>(),
                },
            },
        };

        return $"```json\n{JsonSerializer.Serialize(reply)}\n```";
    }

    private static string OverlapReply(string systemId)
    {
        var reply = new
        {
            summary = "Overlaps another task.",
            findings = new object[]
            {
                new
                {
                    category = "overlap",
                    title = "Same export as another task",
                    explanation = $"{systemId} already covers CSV export.",
                    quote = (string?)null,
                    suggestion = new { kind = "link", systemId, category = "duplicate" },
                    relatedTasks = new[] { systemId },
                },
            },
        };

        return JsonSerializer.Serialize(reply);
    }

    private List<string> ReadToolNames()
    {
        using var scope = Fixture.CreateScope();

        return scope.ServiceProvider.GetServices<IAiTool>()
            .Where(tool => tool.Kind == AiToolKind.Read && tool.IsAvailable(NetptunePermissions.All))
            .Select(tool => tool.Name)
            .ToList();
    }

    private async Task<int> SeedPullRequestLink(TaskViewModel task)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var workspaceId = task.WorkspaceId!.Value;
        var connection = new SourceControlConnection
        {
            WorkspaceId = workspaceId,
            OwnerId = task.OwnerId,
            CreatedByUserId = task.OwnerId,
            ExternalAccountId = Guid.NewGuid().ToString("N"),
            AccountLogin = "acme",
        };

        context.SourceControlConnections.Add(connection);

        await context.SaveChangesAsync(CancellationToken);

        var repo = new SourceControlRepo
        {
            WorkspaceId = workspaceId,
            ConnectionId = connection.Id,
            ExternalId = Guid.NewGuid().ToString("N"),
            Owner = "acme",
            Name = "web",
            FullName = "acme/web",
            DefaultBranch = "main",
            WebUrl = "https://example.com/acme/web",
            IsLinked = true,
            SyncedAt = DateTime.UtcNow,
        };

        context.SourceControlRepos.Add(repo);

        await context.SaveChangesAsync(CancellationToken);

        context.TaskCodeLinks.Add(new TaskCodeLink
        {
            WorkspaceId = workspaceId,
            TaskId = task.Id,
            RepoId = repo.Id,
            Kind = TaskCodeLinkKind.PullRequest,
            ExternalRef = "18",
            Title = "Fix the redirect",
            WebUrl = "https://example.com/acme/web/pull/18",
            PullRequestState = ScmPullRequestState.Open,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        await context.SaveChangesAsync(CancellationToken);

        return connection.Id;
    }

    private async Task RemoveSourceControl(int connectionId)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await context.TaskCodeLinks
            .Where(link => link.Repo.ConnectionId == connectionId)
            .ExecuteDeleteAsync(CancellationToken);
        await context.SourceControlRepos
            .Where(repo => repo.ConnectionId == connectionId)
            .ExecuteDeleteAsync(CancellationToken);
        await context.SourceControlConnections
            .Where(connection => connection.Id == connectionId)
            .ExecuteDeleteAsync(CancellationToken);
    }

    private async Task<AiUsageRecord> LatestReviewUsage()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.AiUsageRecords
            .AsNoTracking()
            .Where(record => record.Purpose == AiUsagePurposes.TaskReview)
            .OrderByDescending(record => record.Id)
            .FirstAsync(CancellationToken);
    }

    private static object SplitSuggestion()
    {
        return new
        {
            kind = "split",
            tasks = new[]
            {
                new { name = $"Export rows as CSV {Guid.NewGuid():N}", description = "The CSV part." },
                new { name = $"Export charts as PDF {Guid.NewGuid():N}", description = "The PDF part." },
            },
        };
    }

    private async Task<ReviewedFinding> ReviewWithOneEdit(int taskId, string? find = "It should be fast.")
    {
        var reply = new
        {
            summary = "Needs a target.",
            findings = new[]
            {
                new
                {
                    category = "testability",
                    title = "Speed is not measurable",
                    explanation = "Fast has no threshold.",
                    suggestion = new
                    {
                        kind = "edit",
                        field = "description",
                        find,
                        replace = MeasurableTarget,
                    },
                },
            },
        };

        Script().Enqueue(new AiChatTurn { Text = JsonSerializer.Serialize(reply) });

        var frames = await Run(taskId);
        var runId = frames.Last().GetProperty("runId").GetInt32();
        var latest = await Latest(taskId);
        var finding = latest.Current!.Findings.Single();

        return new ReviewedFinding(runId, finding.Id);
    }

    private async Task<ReviewedFinding> ReviewWithSuggestion(int taskId, string category, object suggestion)
    {
        var reply = new
        {
            summary = "Needs work.",
            findings = new[]
            {
                new { category, title = "Suggested change", explanation = "Because.", suggestion },
            },
        };

        Script().Enqueue(new AiChatTurn { Text = JsonSerializer.Serialize(reply) });

        var frames = await Run(taskId);
        var runId = frames.Last().GetProperty("runId").GetInt32();
        var latest = await Latest(taskId);
        var finding = latest.Current!.Findings.Single();

        finding.Suggestion.Should().NotBeNull();

        return new ReviewedFinding(runId, finding.Id);
    }

    private async Task<ReviewedFinding> SeedFinding<TSuggestion>(
        int taskId,
        AiReviewFindingCategory category,
        TSuggestion suggestion)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var workspaceId = await context.ProjectTasks
            .Where(task => task.Id == taskId)
            .Select(task => task.WorkspaceId)
            .SingleAsync(CancellationToken);
        var fingerprint = new string('0', 64);
        var run = new AiReviewRun
        {
            WorkspaceId = workspaceId,
            SubjectType = EntityType.Task,
            SubjectId = taskId,
            Status = AiReviewRunStatus.Succeeded,
            CorrelationId = Guid.NewGuid(),
            ReviewedFingerprint = fingerprint,
            BaselineFingerprint = fingerprint,
            Provider = AiProvider.Anthropic,
            Model = AiModels.AnthropicReviewModel,
            StartedAt = DateTime.UtcNow,
        };

        var finding = new AiReviewFinding
        {
            Sequence = 1,
            TargetType = EntityType.Task,
            TargetId = taskId,
            Category = category,
            Title = "Seeded finding",
            Explanation = "Because.",
            Suggestion = JsonSerializer.SerializeToDocument(suggestion, JsonOptions.Default),
            Status = AiReviewFindingStatus.Open,
        };

        run.Findings.Add(finding);
        context.AiReviewRuns.Add(run);

        await context.SaveChangesAsync(CancellationToken);

        return new ReviewedFinding(run.Id, finding.Id);
    }

    private Task<HttpResponseMessage> Decide(ReviewedFinding finding, object request, HttpClient? client = null)
    {
        var url = $"api/ai/reviews/{finding.RunId}/findings/{finding.FindingId}/decide";
        var sender = client ?? Client;

        return sender.PostAsJsonAsync(url, request, CancellationToken);
    }

    private async Task UpdateDescription(int taskId, string description)
    {
        var update = new UpdateProjectTaskRequest { Id = taskId, Description = description };
        var response = await Client.PutAsJsonAsync("api/tasks", update, CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private async Task<string?> GetDescription(int taskId)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.ProjectTasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => task.Description)
            .SingleAsync(CancellationToken);
    }

    private async Task<AiReviewRun> GetRun(int runId)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.AiReviewRuns
            .AsNoTracking()
            .SingleAsync(run => run.Id == runId, CancellationToken);
    }

    private async Task<int> CountTasksNamed(string prefix)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.ProjectTasks.CountAsync(task => task.Name.StartsWith(prefix), CancellationToken);
    }

    private async Task<int> CountTagsNamed(string prefix)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Tags.CountAsync(tag => tag.Name.StartsWith(prefix), CancellationToken);
    }

    private async Task<EventRecord> DescriptionEvent(int taskId)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var subjectId = taskId.ToString();
        var records = await context.EventRecords
            .AsNoTracking()
            .Where(record => record.EventKey == EventKeys.EntityFieldTransitioned && record.SubjectId == subjectId)
            .OrderByDescending(record => record.Id)
            .ToListAsync(CancellationToken);

        return records.First(record => record.Payload.RootElement.GetProperty("field").GetString() == "description");
    }

    private async Task<List<string>> EventKeysFor(int taskId)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var subjectId = taskId.ToString();

        return await context.EventRecords
            .AsNoTracking()
            .Where(record => record.SubjectId == subjectId && record.EventKey.StartsWith("assistant.review"))
            .Select(record => record.EventKey)
            .ToListAsync(CancellationToken);
    }

    private sealed record ReviewedFinding(int RunId, long FindingId);

    private async Task<List<JsonElement>> Run(int taskId, HttpClient? client = null)
    {
        var sender = client ?? Client;
        var response = await sender.PostAsync($"api/tasks/{taskId}/reviews", null, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stream = await response.Content.ReadAsStringAsync(CancellationToken);

        return stream
            .Split('\n')
            .Where(line => line.StartsWith("data: ", StringComparison.Ordinal))
            .Select(line => JsonDocument.Parse(line[6..]).RootElement)
            .ToList();
    }

    private async Task<AiReviewLatestViewModel> Latest(int taskId)
    {
        var response = await Client.GetFromJsonAsync<ClientResponse<AiReviewLatestViewModel>>(
            $"api/tasks/{taskId}/reviews/latest",
            CancellationToken);

        return response!.Payload!;
    }

    private async Task<List<string>> ReviewEvents(int taskId)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var subjectId = taskId.ToString();

        return await context.EventRecords
            .AsNoTracking()
            .Where(record => record.EventKey == EventKeys.AssistantReviewCompleted && record.SubjectId == subjectId)
            .Select(record => record.EventKey)
            .ToListAsync(CancellationToken);
    }

    private async Task<TaskViewModel> CreateTask(string? name = null)
    {
        var request = new AddProjectTaskRequest
        {
            Name = name ?? $"Review target {Guid.NewGuid():N}",
            Description = Description,
            ProjectId = 1,
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request, CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>(CancellationToken);

        return result!.Payload!;
    }

    private TestAiChatScript Script()
    {
        using var scope = Fixture.CreateScope();

        return scope.ServiceProvider.GetRequiredService<TestAiChatScript>();
    }

    private async Task SaveCredential(string url = UserCredentials, string? reviewModel = null)
    {
        var request = new
        {
            provider = AiProvider.Anthropic,
            label = "Anthropic",
            secret = "sk-ant-integration-secret-value",
            reviewModel,
        };

        var response = await Client.PutAsJsonAsync(url, request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<List<AiCredentialViewModel>> Credentials(string url)
    {
        var credentials = await Client.GetFromJsonAsync<List<AiCredentialViewModel>>(url, CancellationToken);

        return credentials ?? [];
    }

    private async Task DeleteCredentials(string url)
    {
        var credentials = await Credentials(url);

        foreach (var credential in credentials)
        {
            await Client.DeleteAsync($"{url}/{credential.Id}", CancellationToken);
        }
    }
}
