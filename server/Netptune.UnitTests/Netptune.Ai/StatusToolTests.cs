using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Handlers.Statuses.Commands;
using Netptune.Handlers.Statuses.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class StatusToolTests
{
    private const int InProgressId = 5;
    private const int DoneId = 6;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public StatusToolTests()
    {
        GivenStatuses();
        GivenUpdateSucceeds();
    }

    [Fact]
    public async Task UpdateStatus_ShouldProposeOnlyTheChangedFields()
    {
        var tool = new UpdateStatusTool(Mediator, ChangeSet);
        var json = $$"""{"statusId":{{InProgressId}},"name":"Doing","category":"Active","color":"#ff0000"}""";
        var result = await Execute(tool, json);

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;
        var payload = change.Payload.RootElement;

        change.ToolName.Should().Be("propose_update_status");
        change.EntityType.Should().Be("status");
        change.EntityId.Should().Be(InProgressId);
        change.Fields.Select(field => field.Name).Should().Equal("name", "color");
        change.Fields.Should().Contain(field => field.Name == "name" && field.Before == "In progress" && field.After == "Doing");
        payload.GetProperty("name").GetString().Should().Be("Doing");
        payload.TryGetProperty("category", out _).Should().BeFalse();
        payload.TryGetProperty("description", out _).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStatus_ShouldProposeTheCategory()
    {
        var tool = new UpdateStatusTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"statusId":{{InProgressId}},"category":"todo"}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes[0].Payload.RootElement.GetProperty("category").GetString().Should().Be("Todo");
    }

    [Fact]
    public async Task UpdateStatus_ShouldFail_WhenTheNameIsTakenByAnotherStatus()
    {
        var tool = new UpdateStatusTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"statusId":{{InProgressId}},"name":"done"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateStatus_ShouldFail_WhenTheCategoryIsUnknown()
    {
        var tool = new UpdateStatusTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"statusId":{{InProgressId}},"category":"Blocked"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateStatus_ShouldFail_WhenNothingWouldChange()
    {
        var tool = new UpdateStatusTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"statusId":{{InProgressId}},"name":"In progress","category":"Active"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateStatus_ShouldFail_WhenTheStatusIsNotInTheWorkspace()
    {
        var tool = new UpdateStatusTool(Mediator, ChangeSet);
        var result = await Execute(tool, """{"statusId":404,"name":"Doing"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyUpdateStatus_ShouldKeepTheFieldsTheChangeLeavesAlone()
    {
        var handler = new UpdateStatusChangeHandler(Mediator);
        var context = CreateContext($$"""{"statusId":{{InProgressId}},"name":"Doing"}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);

        var request = CapturedUpdates().Should().ContainSingle().Subject;

        request.Id.Should().Be(InProgressId);
        request.Name.Should().Be("Doing");
        request.Category.Should().Be(StatusCategory.Active);
        request.Color.Should().Be("blue");
        request.Description.Should().Be("Being worked on");
    }

    [Fact]
    public async Task ApplyUpdateStatus_ShouldCarryTheNewCategory()
    {
        var handler = new UpdateStatusChangeHandler(Mediator);
        var context = CreateContext($$"""{"statusId":{{InProgressId}},"category":"Done"}""");

        await handler.Apply(context, TestContext.Current.CancellationToken);

        CapturedUpdates().Should().ContainSingle().Which.Category.Should().Be(StatusCategory.Done);
    }

    [Fact]
    public async Task ApplyUpdateStatus_ShouldFail_WhenTheStatusNoLongerExists()
    {
        var handler = new UpdateStatusChangeHandler(Mediator);
        var context = CreateContext("""{"statusId":404,"name":"Doing"}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        CapturedUpdates().Should().BeEmpty();
    }

    private static async Task<AiToolExecution> Execute(IAiTool tool, string json)
    {
        var arguments = JsonDocument.Parse(json).RootElement;

        return await tool.Execute(arguments, TestContext.Current.CancellationToken);
    }

    private void GivenStatuses()
    {
        var statuses = new List<StatusViewModel>
        {
            new()
            {
                Id = InProgressId,
                Name = "In progress",
                Key = "in-progress",
                Description = "Being worked on",
                Color = "blue",
                Category = StatusCategory.Active,
            },
            new()
            {
                Id = DoneId,
                Name = "Done",
                Key = "done",
                Category = StatusCategory.Done,
            },
        };

        Mediator
            .Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
            .Returns(statuses);
    }

    private void GivenUpdateSucceeds()
    {
        var response = ClientResponse<StatusViewModel>.Success(new StatusViewModel { Id = InProgressId, Name = "Doing", Key = "doing" });

        Mediator
            .Send(Arg.Any<UpdateStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);
    }

    private List<UpdateStatusRequest> CapturedUpdates()
    {
        return Mediator.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<UpdateStatusCommand>()
            .Select(command => command.Request)
            .ToList();
    }

    private static AiChangeApplyContext CreateContext(string payload)
    {
        var change = new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = "propose_update_status",
            EntityType = "status",
            EntityId = InProgressId,
            Summary = "Update a status",
            Payload = JsonDocument.Parse(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Pending,
        };

        return new AiChangeApplyContext
        {
            Change = change,
            ResolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal),
        };
    }
}
