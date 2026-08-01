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

        return new AiConversationRunner(factory, new AiToolRegistry(tools), options);
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

        public async IAsyncEnumerable<AiProviderStreamEvent> Stream(
            AiChatRequest request,
            string apiKey,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            LastRequest = request;
            RequestCount += 1;

            await Task.CompletedTask;

            yield return AiProviderStreamEvent.Delta("hi");

            var toolName = AlwaysCallTool ?? (RequestCount == 1 ? CallToolOnce : null);
            var hasToolCall = toolName is not null;

            if (!hasToolCall)
            {
                yield return AiProviderStreamEvent.Completed(new AiChatTurn { Text = "hi" });

                yield break;
            }

            yield return AiProviderStreamEvent.Completed(new AiChatTurn
            {
                Text = "hi",
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

        public AiToolKind Kind => AiToolKind.Read;

        public IReadOnlySet<string> RequiredPermissions { get; }

        public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

        public string Result { get; set; } = "ok";

        public Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
        {
            return Task.FromResult(AiToolExecution.Success(Result));
        }
    }
}
