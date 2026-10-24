using System.Text.Json;

using FluentAssertions;

using Mediator;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Ai.Tools;
using Netptune.Automation.Actions;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.Services.Automations;
using Netptune.Core.ViewModels.Automations;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Handlers.Automations.Queries;
using Netptune.Handlers.Projects.Queries;
using Netptune.Handlers.ServiceAccounts.Queries;
using Netptune.Handlers.Statuses.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AutomationChangeToolTests
{
    private const int RuleId = 21;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly IAiChangeSetBuilder ChangeSet = Substitute.For<IAiChangeSetBuilder>();
    private readonly IAutomationActionRegistry ActionRegistry;
    private readonly List<AiChangeDraft> Drafts = [];
    private readonly AutomationChangeTool Tool;

    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public AutomationChangeToolTests()
    {
        var services = new ServiceCollection();
        services.AddNetptuneAutomationActions();
        ActionRegistry = services.BuildServiceProvider().GetRequiredService<IAutomationActionRegistry>();

        ChangeSet
            .When(builder => builder.Add(Arg.Any<AiChangeDraft>()))
            .Do(call => Drafts.Add(call.Arg<AiChangeDraft>()));
        Mediator
            .Send(Arg.Any<ValidateAutomationRuleQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Success);
        Mediator
            .Send(Arg.Any<GetServiceAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                ServiceAccount("zeta", "Zeta bot"),
                ServiceAccount("off", "Archived bot", disabled: true),
                ServiceAccount("alpha", "Alpha bot"),
            ]);
        Mediator
            .Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
            .Returns([new StatusViewModel { Id = 3, Name = "Done", Category = StatusCategory.Done }]);
        Mediator.Send(Arg.Any<GetProjectsQuery>(), Arg.Any<CancellationToken>()).Returns([]);

        Tool = new AutomationChangeTool(Mediator, ChangeSet, ActionRegistry);
    }

    [Fact]
    public async Task Create_ShouldProposeADisabledRule_RunAsTheFirstEnabledServiceAccount()
    {
        var result = await Execute(
            """
            {"action":"create","name":"Close reviewed tasks",
             "trigger":{"type":"TaskCreated"},
             "actions":[{"type":"UpdateTask","statusId":3}]}
            """);

        result.IsError.Should().BeFalse(result.Content);

        var draft = Drafts.Single();
        var rule = ReadPayloadRule(draft);

        draft.ToolName.Should().Be(CreateAutomationAction.Change);
        draft.EntityType.Should().Be("automationRule");
        rule.IsEnabled.Should().BeFalse("a new rule waits until the user turns it on");
        rule.ExecutionUserId.Should().Be("alpha");
        draft.Fields.Should().Contain(field => field.Name == "rule" && field.Kind == AiChangeValueKind.Automation);
        draft.Fields.Single(field => field.Name == "scope").After.Should().Be("Workspace");
    }

    [Fact]
    public async Task Create_ShouldKeepTheRuleLive_WhenTheUserAskedForIt()
    {
        await Execute(
            """
            {"action":"create","name":"Live rule","isEnabled":true,
             "trigger":{"type":"TaskCreated"},"actions":[{"type":"UpdateTask","statusId":3}]}
            """);

        ReadPayloadRule(Drafts.Single()).IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldPassOnTheValidationError_AndProposeNothing()
    {
        Mediator
            .Send(Arg.Any<ValidateAutomationRuleQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Failed("Status with id 3 was not found in the workspace."));

        var result = await Execute(
            """
            {"action":"create","name":"Rule","trigger":{"type":"TaskCreated"},
             "actions":[{"type":"UpdateTask","statusId":3}]}
            """);

        result.IsError.Should().BeTrue();
        result.Content.Should().Be("Status with id 3 was not found in the workspace.");
        Drafts.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ShouldFail_WhenTheTriggerTypeIsUnknown()
    {
        var result = await Execute(
            """{"action":"create","name":"Rule","trigger":{"type":"WhenTheMoonIsFull"},"actions":[]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("could not be read");
    }

    [Theory]
    [InlineData("""{"action":"create","trigger":{"type":"TaskCreated"},"actions":[]}""", "A rule name is required.")]
    [InlineData("""{"action":"create","name":"Rule","actions":[]}""", "A trigger is required.")]
    [InlineData("""{"action":"create","name":"Rule","trigger":{"type":"TaskCreated"},"actions":null}""", "An actions list is required.")]
    public async Task Create_ShouldNameTheMissingPart(string json, string expected)
    {
        var result = await Execute(json);

        result.IsError.Should().BeTrue();
        result.Content.Should().Be(expected);
        Drafts.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ShouldDefaultToAServiceAccountThatCanRunTheActions()
    {
        Mediator
            .Send(Arg.Any<GetServiceAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                ServiceAccount("alpha", "Alpha bot"),
                ServiceAccount("beta", "Beta bot", permissions: [.. NetptunePermissions.All]),
            ]);

        await Execute(
            """
            {"action":"create","name":"Close reviewed tasks","trigger":{"type":"TaskCreated"},
             "actions":[{"type":"UpdateTask","statusId":3}]}
            """);

        ReadPayloadRule(Drafts.Single()).ExecutionUserId.Should().Be("beta");
    }

    [Fact]
    public async Task Update_ShouldReplaceOnlyTheSectionsPassed()
    {
        ReturnRule(BuildRule());

        await Execute("""{"action":"update","ruleId":21,"trigger":{"type":"TaskUnassignedFor","durationDays":3}}""");

        var draft = Drafts.Single();
        var rule = ReadPayloadRule(draft);

        rule.Name.Should().Be("Close reviewed tasks");
        rule.Trigger.Type.Should().Be(AutomationTriggerType.TaskUnassignedFor);
        rule.Trigger.DurationDays.Should().Be(3);
        rule.Actions.Should().ContainSingle().Which.StatusId.Should().Be(3);
        draft.EntityId.Should().Be(RuleId);
    }

    [Fact]
    public async Task Update_ShouldWarnAboutCancelledDelayedActions_WhenMoreThanTheNameChanges()
    {
        ReturnRule(BuildRule());
        Mediator.Send(Arg.Any<CountPendingRuleActionsQuery>(), Arg.Any<CancellationToken>()).Returns(2);

        await Execute("""{"action":"update","ruleId":21,"actions":[{"type":"AddComment","comment":"Done"}]}""");

        Drafts.Single().ValidationMessage.Should().Be(
            "Applying this cancels 2 pending delayed actions, and undo will not bring them back.");
    }

    [Fact]
    public async Task Update_ShouldNotWarn_WhenOnlyTheNameChanges()
    {
        ReturnRule(BuildRule());
        Mediator.Send(Arg.Any<CountPendingRuleActionsQuery>(), Arg.Any<CancellationToken>()).Returns(2);

        await Execute("""{"action":"update","ruleId":21,"name":"Close finished tasks"}""");

        Drafts.Single().ValidationMessage.Should().BeNull();
        await Mediator.DidNotReceive().Send(Arg.Any<CountPendingRuleActionsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldFail_WhenNothingChanges()
    {
        ReturnRule(BuildRule());

        var result = await Execute("""{"action":"update","ruleId":21,"name":"Close reviewed tasks"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Be("No changes were supplied for this rule.");
    }

    [Fact]
    public async Task Update_ShouldClearTheScope_WhenAScopeIsPassedAsNull()
    {
        ReturnRule(BuildRule() with { ProjectId = 4 });

        await Execute("""{"action":"update","ruleId":21,"projectId":null}""");

        ReadPayloadRule(Drafts.Single()).ProjectId.Should().BeNull();
    }

    [Fact]
    public async Task SetEnabled_ShouldWarnAboutCancelledDelayedActions_WhenDisabling()
    {
        ReturnRule(BuildRule() with { IsEnabled = true });
        Mediator.Send(Arg.Any<CountPendingRuleActionsQuery>(), Arg.Any<CancellationToken>()).Returns(1);

        await Execute("""{"action":"set_enabled","ruleId":21,"isEnabled":false}""");

        var draft = Drafts.Single();

        draft.ToolName.Should().Be(SetAutomationEnabledAction.Change);
        draft.ValidationMessage.Should().Be(
            "Applying this cancels 1 pending delayed action, and undo will not bring it back.");
    }

    [Fact]
    public async Task SetEnabled_ShouldDescribeTheChangeInTheCurrentUiCulture()
    {
        ReturnRule(BuildRule() with { IsEnabled = true });

        using var culture = CultureScope.Use("fr");

        await Execute("""{"action":"set_enabled","ruleId":21,"isEnabled":false}""");

        var draft = Drafts.Single();
        var field = draft.Fields.Single();

        draft.Summary.Should().StartWith("Désactiver l’automatisation “");
        field.Before.Should().Be("Activée");
        field.After.Should().Be("Désactivée");
    }

    [Fact]
    public async Task SetEnabled_ShouldFail_WhenTheRuleIsAlreadyInThatState()
    {
        ReturnRule(BuildRule());

        var result = await Execute("""{"action":"set_enabled","ruleId":21,"isEnabled":false}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("already disabled");
    }

    [Fact]
    public void GetRequiredPermissions_ShouldAskToReadServiceAccounts_WhenCreating()
    {
        var arguments = JsonDocument.Parse("""{"action":"create"}""").RootElement;

        Tool.GetRequiredPermissions(arguments).Should().Contain(NetptunePermissions.ServiceAccounts.Read);
        Tool.IsAvailable(new HashSet<string> { NetptunePermissions.Automations.Manage }).Should().BeTrue();
        Tool.IsAvailable(new HashSet<string> { NetptunePermissions.Automations.Read }).Should().BeFalse();
    }

    [Fact]
    public void Normalize_ShouldLeaveAStoredRuleAsItIs()
    {
        var stored = AiAutomationJson.ToRequest(BuildRule());
        var normalized = AiAutomationJson.Normalize(stored, ActionRegistry);

        AiAutomationJson.AreSame(stored, normalized).Should().BeTrue();
    }

    private async Task<AiToolExecution> Execute(string json)
    {
        var arguments = JsonDocument.Parse(json).RootElement;

        return await Tool.Execute(arguments, CancellationToken);
    }

    private void ReturnRule(AutomationRuleViewModel rule)
    {
        Mediator
            .Send(Arg.Is<GetAutomationRuleQuery>(query => query.Id == RuleId), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<AutomationRuleViewModel>.Success(rule));
    }

    private static AutomationRuleRequest ReadPayloadRule(AiChangeDraft draft)
    {
        var rule = draft.Payload.RootElement.GetProperty("rule");

        return rule.Deserialize<AutomationRuleRequest>(AiAutomationJson.Options)!;
    }

    private static AutomationRuleViewModel BuildRule()
    {
        return new AutomationRuleViewModel
        {
            Id = RuleId,
            Name = "Close reviewed tasks",
            IsEnabled = false,
            ExecutionUserId = "alpha",
            Trigger = new AutomationTriggerViewModel { Type = AutomationTriggerType.TaskCreated },
            Actions =
            [
                new AutomationActionViewModel
                {
                    Id = 1,
                    Type = AutomationActionType.UpdateTask,
                    StatusId = 3,
                },
            ],
        };
    }

    private static ServiceAccountViewModel ServiceAccount(
        string userId,
        string name,
        bool disabled = false,
        IReadOnlyList<string>? permissions = null)
    {
        return new ServiceAccountViewModel
        {
            UserId = userId,
            Name = name,
            DisabledAt = disabled ? DateTime.UtcNow : null,
            OwnerUserIds = [],
            Permissions = permissions ?? [],
        };
    }
}
