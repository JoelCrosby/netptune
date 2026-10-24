using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Automations;
using Netptune.Handlers.Automations.Commands;
using Netptune.Handlers.Automations.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AutomationChangeHandlerTests
{
    private const int RuleId = 30;

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public AutomationChangeHandlerTests()
    {
        Mediator
            .Send(Arg.Any<UpdateAutomationRuleCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<AutomationRuleViewModel>.Success(BuildRule("Updated")));
        Mediator
            .Send(Arg.Any<DeleteAutomationRuleCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Success);
        Mediator
            .Send(Arg.Any<SetAutomationRuleEnabledCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Success);
    }

    [Fact]
    public async Task UpdateApply_ShouldRefuse_WhenTheRuleWasEditedAfterTheProposal()
    {
        ReturnRule(BuildRule("Renamed by someone else"));
        var change = UpdateChange(previousName: "Original", newName: "Updated");
        var handler = new UpdateAutomationChangeHandler(Mediator);

        var result = await handler.Apply(ApplyContext(change), CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(UpdateAutomationChangeHandler.EditedBeforeApplyMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<UpdateAutomationRuleCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateApply_ShouldSaveTheProposedRule_WhenTheRuleIsAsProposed()
    {
        ReturnRule(BuildRule("Original"));
        var change = UpdateChange(previousName: "Original", newName: "Updated");
        var handler = new UpdateAutomationChangeHandler(Mediator);

        var result = await handler.Apply(ApplyContext(change), CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<UpdateAutomationRuleCommand>(command => command.Id == RuleId && command.Request.Name == "Updated"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRevert_ShouldRestoreTheCapturedRule()
    {
        var handler = new UpdateAutomationChangeHandler(Mediator);
        var change = UpdateChange(previousName: "Original", newName: "Updated");

        ReturnRule(BuildRule("Original"));
        change.UndoPayload = await handler.Capture(ApplyContext(change), CancellationToken);
        ReturnRule(BuildRule("Updated"));

        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied, result.Error);
        await Mediator.Received(1).Send(
            Arg.Is<UpdateAutomationRuleCommand>(command => command.Request.Name == "Original"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRevert_ShouldRefuse_WhenTheRuleWasEditedAfterTheChangeWasApplied()
    {
        var handler = new UpdateAutomationChangeHandler(Mediator);
        var change = UpdateChange(previousName: "Original", newName: "Updated");

        ReturnRule(BuildRule("Original"));
        change.UndoPayload = await handler.Capture(ApplyContext(change), CancellationToken);
        ReturnRule(BuildRule("Edited again"));

        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, CancellationToken);

        result.Error.Should().Be(UpdateAutomationChangeHandler.EditedSinceMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<UpdateAutomationRuleCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRevert_ShouldDeleteTheRule_WhenItIsUnchanged()
    {
        var created = AiAutomationJson.ToRequest(BuildRule("Created"));
        var change = Change(CreateAutomationAction.Change, new { rule = created });

        change.AppliedEntityId = RuleId;
        ReturnRule(BuildRule("Created"));

        var handler = new CreateAutomationChangeHandler(Mediator);
        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied, result.Error);
        await Mediator.Received(1).Send(
            Arg.Is<DeleteAutomationRuleCommand>(command => command.Id == RuleId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRevert_ShouldKeepTheRule_WhenItWasEditedSince()
    {
        var created = AiAutomationJson.ToRequest(BuildRule("Created"));
        var change = Change(CreateAutomationAction.Change, new { rule = created });

        change.AppliedEntityId = RuleId;
        ReturnRule(BuildRule("Edited"));

        var handler = new CreateAutomationChangeHandler(Mediator);
        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, CancellationToken);

        result.Error.Should().Be(CreateAutomationChangeHandler.EditedSinceMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<DeleteAutomationRuleCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEnabledRevert_ShouldTurnTheRuleBack()
    {
        var change = Change(SetAutomationEnabledAction.Change, new { ruleId = RuleId, isEnabled = false });
        var handler = new SetAutomationEnabledChangeHandler(Mediator);

        ReturnRule(BuildRule("Rule") with { IsEnabled = true });
        change.UndoPayload = await handler.Capture(ApplyContext(change), CancellationToken);
        ReturnRule(BuildRule("Rule") with { IsEnabled = false });

        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied, result.Error);
        await Mediator.Received(1).Send(
            Arg.Is<SetAutomationRuleEnabledCommand>(command => command.Id == RuleId && command.IsEnabled),
            Arg.Any<CancellationToken>());
    }

    private void ReturnRule(AutomationRuleViewModel rule)
    {
        Mediator
            .Send(Arg.Is<GetAutomationRuleQuery>(query => query.Id == RuleId), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<AutomationRuleViewModel>.Success(rule));
    }

    private static AiProposedChange UpdateChange(string previousName, string newName)
    {
        var previous = AiAutomationJson.ToRequest(BuildRule(previousName));
        var rule = previous with { Name = newName };

        return Change(UpdateAutomationAction.Change, new { ruleId = RuleId, rule, previous });
    }

    private static AiProposedChange Change(string toolName, object payload)
    {
        return new AiProposedChange
        {
            ToolName = toolName,
            EntityType = "automationRule",
            EntityId = RuleId,
            Summary = "Automation change",
            Payload = JsonSerializer.SerializeToDocument(payload, AiAutomationJson.Options),
        };
    }

    private static AiChangeApplyContext ApplyContext(AiProposedChange change)
    {
        return new AiChangeApplyContext
        {
            Change = change,
            ResolvedRefs = new Dictionary<string, int>(),
        };
    }

    private static AutomationRuleViewModel BuildRule(string name)
    {
        return new AutomationRuleViewModel
        {
            Id = RuleId,
            Name = name,
            ExecutionUserId = "service-user",
            Trigger = new AutomationTriggerViewModel { Type = AutomationTriggerType.TaskCreated },
            Actions =
            [
                new AutomationActionViewModel
                {
                    Id = 1,
                    Type = AutomationActionType.AddComment,
                    Comment = "Picked up",
                },
            ],
        };
    }
}
