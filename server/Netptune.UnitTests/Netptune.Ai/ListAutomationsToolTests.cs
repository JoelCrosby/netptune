using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Automations;
using Netptune.Handlers.Automations.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class ListAutomationsToolTests
{
    private const int RuleId = 12;

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task Execute_ShouldReadOneRuleInFull_WithEnumsByNameAndAReference()
    {
        ReturnRule(BuildRule());

        var root = await ExecuteForRule(includeReference: true);
        var rule = root.GetProperty("rule");
        var action = rule.GetProperty("actions")[0];

        rule.GetProperty("isEnabled").GetBoolean().Should().BeFalse("a disabled rule must still say so");
        rule.GetProperty("trigger").GetProperty("type").GetString().Should().Be("TaskChanged");
        action.GetProperty("type").GetString().Should().Be("UpdateTask");
        action.GetProperty("statusId").GetInt32().Should().Be(3);
        action.TryGetProperty("clearDescription", out _).Should().BeFalse("false flags are left out");
        action.TryGetProperty("addTags", out _).Should().BeFalse("empty lists are left out");
        root.GetProperty("warnings")[0].GetString().Should().Be("Status 3 was deleted.");
        root.GetProperty("reference").GetProperty("triggers").TryGetProperty("TaskCreated", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Execute_ShouldReturnARuleTheRequestTypesCanReadBack()
    {
        var original = BuildRule();

        ReturnRule(original);

        var root = await ExecuteForRule();
        var rule = root.GetProperty("rule");
        var trigger = rule.GetProperty("trigger").Deserialize<AutomationTriggerRequest>(AiAutomationJson.Options);
        var action = rule.GetProperty("actions")[0].Deserialize<AutomationActionRequest>(AiAutomationJson.Options);

        trigger!.Type.Should().Be(AutomationTriggerType.TaskChanged);
        trigger.Fields.Should().Equal(TaskChangeField.Status);
        trigger.ConditionGroup.Should().BeEquivalentTo(original.Trigger.ConditionGroup);
        action!.Type.Should().Be(AutomationActionType.UpdateTask);
        action.StatusId.Should().Be(3);
        action.AddTags.Should().BeEmpty();
        action.ClearDescription.Should().BeFalse();
    }

    [Fact]
    public async Task Execute_ShouldLeaveTheReferenceOut_UnlessItIsAskedFor()
    {
        ReturnRule(BuildRule());

        var root = await ExecuteForRule();

        root.TryGetProperty("reference", out _).Should().BeFalse("the reference costs tokens on every read");
    }

    [Fact]
    public async Task Execute_ShouldReturnOnlyTheReference_WhenNoRuleIsNamed()
    {
        var tool = new ListAutomationsTool(Mediator);
        var arguments = JsonDocument.Parse("""{"includeReference":true}""").RootElement;
        var result = await tool.Execute(arguments, TestContext.Current.CancellationToken);

        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;

        root.GetProperty("reference").GetProperty("actions").TryGetProperty("CreateTask", out _).Should().BeTrue();
        root.EnumerateObject().Should().ContainSingle("a workspace with no rules still needs the reference");
        await Mediator.DidNotReceiveWithAnyArgs().Send(default(GetAutomationRulesPagedQuery)!, default);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheRuleIsNotInTheWorkspace()
    {
        Mediator
            .Send(Arg.Any<GetAutomationRuleQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<AutomationRuleViewModel>.NotFound);

        var tool = new ListAutomationsTool(Mediator);
        var arguments = JsonDocument.Parse($$"""{"ruleId":{{RuleId}}}""").RootElement;
        var result = await tool.Execute(arguments, TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain($"rule {RuleId}");
    }

    private async Task<JsonElement> ExecuteForRule(bool includeReference = false)
    {
        var tool = new ListAutomationsTool(Mediator);
        var flag = includeReference ? "true" : "false";
        var arguments = JsonDocument.Parse($$"""{"ruleId":{{RuleId}},"includeReference":{{flag}}}""").RootElement;
        var result = await tool.Execute(arguments, TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse(result.Content);

        return JsonDocument.Parse(result.Content).RootElement;
    }

    private void ReturnRule(AutomationRuleViewModel rule)
    {
        Mediator
            .Send(Arg.Is<GetAutomationRuleQuery>(query => query.Id == RuleId), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<AutomationRuleViewModel>.Success(rule));
    }

    private static AutomationRuleViewModel BuildRule()
    {
        return new AutomationRuleViewModel
        {
            Id = RuleId,
            Name = "Close review tasks",
            IsEnabled = false,
            ExecutionUserId = "service-user",
            Trigger = new AutomationTriggerViewModel
            {
                Type = AutomationTriggerType.TaskChanged,
                Fields = [TaskChangeField.Status],
                ConditionGroup = new AutomationConditionGroup
                {
                    Operator = AutomationConditionGroupOperator.All,
                    Conditions =
                    [
                        new AutomationFieldCondition
                        {
                            Field = TaskChangeField.Priority,
                            Operator = AutomationConditionOperator.Equals,
                            Value = "High",
                        },
                    ],
                },
            },
            Actions =
            [
                new AutomationActionViewModel
                {
                    Id = 4,
                    Type = AutomationActionType.UpdateTask,
                    StatusId = 3,
                },
            ],
            Warnings =
            [
                new AutomationRuleWarning
                {
                    Code = AutomationWarningCode.MissingStatus,
                    Message = "Status 3 was deleted.",
                },
            ],
        };
    }
}
