using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiConversationRunnerTests
{
    private readonly StubChatProvider Provider = new();
    private readonly AiChangeSetBuilder ChangeSet = new();

    [Fact]
    public async Task Run_ShouldOnlyOfferTools_WhenPermissionsAreHeld()
    {
        var allowed = new StubTool("allowed_tool", NetptunePermissions.Tasks.Read);
        var denied = new StubTool("denied_tool", NetptunePermissions.Projects.Read);
        var runner = CreateRunner([allowed, denied]);
        var context = CreateContext(NetptunePermissions.Tasks.Read);

        await Drain(runner, context);

        var offered = Provider.LastRequest!.Tools.Select(tool => tool.Name);

        offered.Should().BeEquivalentTo("allowed_tool");
    }

    [Fact]
    public async Task Run_ShouldOfferWriteTools_WhenThePermissionIsHeld()
    {
        var write = new StubTool("propose_change", NetptunePermissions.Tasks.Create)
        {
            ToolKind = AiToolKind.Write,
        };

        var runner = CreateRunner([write]);
        var context = CreateContext(NetptunePermissions.Tasks.Create);

        await Drain(runner, context);

        var offered = Provider.LastRequest!.Tools.Select(tool => tool.Name);

        offered.Should().BeEquivalentTo("propose_change");
    }

    [Fact]
    public async Task Run_ShouldNotOfferWriteTools_WhenThePermissionIsMissing()
    {
        var write = new StubTool("propose_change", NetptunePermissions.Tasks.Create)
        {
            ToolKind = AiToolKind.Write,
        };

        var runner = CreateRunner([write]);
        var context = CreateContext(NetptunePermissions.Tasks.Read);

        await Drain(runner, context);

        Provider.LastRequest!.Tools.Should().BeEmpty();
    }

    [Fact]
    public async Task Run_ShouldStopAndReportError_WhenToolIterationLimitIsReached()
    {
        var tool = new StubTool("allowed_tool", NetptunePermissions.Tasks.Read);
        var runner = CreateRunner([tool], maxToolIterations: 3);

        Provider.AlwaysCallTool = "allowed_tool";

        var context = CreateContext(NetptunePermissions.Tasks.Read);
        var events = await Drain(runner, context);
        var lastEvent = events[^1];

        lastEvent.Type.Should().Be(AiStreamEventType.Error);
        Provider.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task Run_ShouldTruncateToolResult_WhenResultExceedsLimit()
    {
        var tool = new StubTool("allowed_tool", NetptunePermissions.Tasks.Read)
        {
            Result = new string('x', 500),
        };

        var runner = CreateRunner([tool], maxToolResultCharacters: 100);

        Provider.CallToolOnce = "allowed_tool";

        var context = CreateContext(NetptunePermissions.Tasks.Read);

        await Drain(runner, context);

        var invocation = context.Invocations.Single();

        invocation.Truncated.Should().BeTrue();
        invocation.Result.Should().Contain("[result truncated]");
    }

    [Fact]
    public async Task Run_ShouldReportError_WhenModelCallsAnUnavailableTool()
    {
        var tool = new StubTool("allowed_tool", NetptunePermissions.Tasks.Read);
        var runner = CreateRunner([tool]);

        Provider.CallToolOnce = "denied_tool";

        var context = CreateContext(NetptunePermissions.Tasks.Read);

        await Drain(runner, context);

        var invocation = context.Invocations.Single();

        invocation.IsError.Should().BeTrue();
        invocation.Result.Should().Contain("not available");
    }

    [Fact]
    public async Task Run_ShouldAskTheModelToProposeAgain_WhenItClaimsAProposalItNeverMade()
    {
        var tool = new StubTool("propose_change", NetptunePermissions.Tasks.Create)
        {
            ToolKind = AiToolKind.Write,
        };

        var runner = CreateRunner([tool]);

        Provider.ReplyText = "I have proposed renaming the task.";

        var context = CreateContext(NetptunePermissions.Tasks.Create);
        var events = await Drain(runner, context);

        Provider.RequestCount.Should().Be(2, "the model gets one chance to back the claim with a tool call");
        events.Should().Contain(item => item.Type == AiStreamEventType.ReplyReset);

        var correction = Provider.LastRequest!.Messages[^1];

        correction.Role.Should().Be(AiMessageRole.User);
        correction.Text.Should().Contain("no propose_ tool ran this turn");
    }

    [Fact]
    public async Task Run_ShouldCorrectAClaimOnlyOnce_SoAStubbornModelCannotLoop()
    {
        var tool = new StubTool("propose_change", NetptunePermissions.Tasks.Create)
        {
            ToolKind = AiToolKind.Write,
        };

        var runner = CreateRunner([tool]);

        Provider.ReplyText = "I have proposed renaming the task.";

        var context = CreateContext(NetptunePermissions.Tasks.Create);
        var events = await Drain(runner, context);

        Provider.RequestCount.Should().Be(2);
        events[^1].Type.Should().Be(AiStreamEventType.TurnCompleted);
    }

    [Fact]
    public async Task Run_ShouldNotCorrect_WhenTheClaimIsBackedByAProposal()
    {
        var tool = new StubTool("propose_change", NetptunePermissions.Tasks.Create)
        {
            ToolKind = AiToolKind.Write,
        };

        var runner = CreateRunner([tool]);

        Provider.ReplyText = "I have proposed renaming the task.";

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = "propose_change",
            EntityType = "task",
            Summary = "Rename task",
            Payload = JsonDocument.Parse("{}"),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        var context = CreateContext(NetptunePermissions.Tasks.Create);
        var events = await Drain(runner, context);

        Provider.RequestCount.Should().Be(1);
        events.Should().NotContain(item => item.Type == AiStreamEventType.ReplyReset);
    }

    [Fact]
    public async Task Run_ShouldNotCorrect_WhenTheReplyMakesNoClaim()
    {
        var tool = new StubTool("propose_change", NetptunePermissions.Tasks.Create)
        {
            ToolKind = AiToolKind.Write,
        };

        var runner = CreateRunner([tool]);

        Provider.ReplyText = "There are four tasks in the sprint.";

        var context = CreateContext(NetptunePermissions.Tasks.Create);
        var events = await Drain(runner, context);

        Provider.RequestCount.Should().Be(1);
        events.Should().NotContain(item => item.Type == AiStreamEventType.ReplyReset);
    }

    [Fact]
    public async Task Run_ShouldReportTokensSpent_AsEachProviderCallCompletes()
    {
        var tool = new StubTool("allowed_tool", NetptunePermissions.Tasks.Read);
        var runner = CreateRunner([tool]);

        Provider.CallToolOnce = "allowed_tool";
        Provider.Usage = new AiUsage { InputTokens = 100, OutputTokens = 20 };

        var context = CreateContext(NetptunePermissions.Tasks.Read);
        var events = await Drain(runner, context);
        var reported = events
            .Where(item => item.Type == AiStreamEventType.TurnUsage)
            .Select(item => item.Usage!.InputTokens + item.Usage.OutputTokens)
            .ToList();

        reported.Should().Equal(
            [120, 240],
            "the count covers every provider call the turn has made so far");
    }

    [Fact]
    public async Task Run_ShouldNotReportTokensSpent_WhenTheProviderReportsNone()
    {
        var tool = new StubTool("allowed_tool", NetptunePermissions.Tasks.Read);
        var runner = CreateRunner([tool]);

        var context = CreateContext(NetptunePermissions.Tasks.Read);
        var events = await Drain(runner, context);

        events.Should().NotContain(item => item.Type == AiStreamEventType.TurnUsage);
    }

    private AiConversationRunner CreateRunner(
        IReadOnlyList<IAiTool> tools,
        int maxToolIterations = 12,
        int maxToolResultCharacters = 32000)
    {
        var factory = Substitute.For<IAiChatProviderFactory>();

        factory.Resolve(AiProvider.Anthropic).Returns(Provider);

        var options = Options.Create(new AiOptions
        {
            MaxToolIterations = maxToolIterations,
            MaxToolResultCharacters = maxToolResultCharacters,
        });

        return new AiConversationRunner(factory, new AiToolRegistry(tools), ChangeSet, options);
    }

    private static AiRunContext CreateContext(params string[] permissions)
    {
        return new AiRunContext
        {
            Provider = AiProvider.Anthropic,
            Model = "test-model",
            ApiKey = "test-key",
            SystemPrompt = "system",
            History = [new AiChatMessage { Role = AiMessageRole.User, Text = "hello" }],
            Permissions = new HashSet<string>(permissions, StringComparer.Ordinal),
        };
    }

    private static async Task<List<AiStreamEvent>> Drain(AiConversationRunner runner, AiRunContext context)
    {
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in runner.Run(context, CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        return events;
    }

    private sealed class StubChatProvider : IAiChatProvider
    {
        public AiProvider Provider => AiProvider.Anthropic;

        public string DefaultModel => "test-model";

        public AiChatRequest? LastRequest { get; private set; }

        public int RequestCount { get; private set; }

        public string? AlwaysCallTool { get; set; }

        public string? CallToolOnce { get; set; }

        public string ReplyText { get; set; } = "hi";

        public AiUsage Usage { get; set; } = new();

        public async IAsyncEnumerable<AiProviderStreamEvent> Stream(
            AiChatRequest request,
            string apiKey,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            LastRequest = request;
            RequestCount += 1;

            await Task.CompletedTask;

            yield return AiProviderStreamEvent.Delta(ReplyText);

            var toolName = AlwaysCallTool ?? (RequestCount == 1 ? CallToolOnce : null);
            var hasToolCall = toolName is not null;

            if (!hasToolCall)
            {
                yield return AiProviderStreamEvent.Completed(new AiChatTurn { Text = ReplyText, Usage = Usage });

                yield break;
            }

            yield return AiProviderStreamEvent.Completed(new AiChatTurn
            {
                Text = ReplyText,
                Usage = Usage,
                ToolCalls =
                [
                    new AiToolCall
                    {
                        Id = $"call-{RequestCount}",
                        Name = toolName!,
                        Arguments = JsonDocument.Parse("{}"),
                    },
                ],
            });
        }
    }

    private sealed class StubTool : IAiTool
    {
        public StubTool(string name, string permission)
        {
            Name = name;
            RequiredPermissions = new HashSet<string>(StringComparer.Ordinal) { permission };
        }

        public string Name { get; }

        public string Description => "stub";

        public AiToolKind ToolKind { get; set; } = AiToolKind.Read;

        public AiToolKind Kind => ToolKind;

        public IReadOnlySet<string> RequiredPermissions { get; }

        public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

        public string Result { get; set; } = "ok";

        public Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
        {
            return Task.FromResult(AiToolExecution.Success(Result));
        }
    }
}
