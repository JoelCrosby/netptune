using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Tags.Commands;
using Netptune.Handlers.Tags.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class TagToolTests
{
    private const int BugId = 3;
    private const int FeatureId = 4;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public TagToolTests()
    {
        GivenTags(("bug", BugId), ("feature", FeatureId));
        GivenUpdateSucceeds();
    }

    [Fact]
    public async Task UpdateTag_ShouldProposeTheRename()
    {
        var tool = new TagChangeTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"action":"update","tagId":{{BugId}},"name":" defect "}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;
        var payload = change.Payload.RootElement;

        change.ToolName.Should().Be(UpdateTagAction.Change);
        change.EntityType.Should().Be("tag");
        change.EntityId.Should().Be(BugId);
        change.Fields.Should().ContainSingle(field => field.Name == "name" && field.Before == "bug" && field.After == "defect");
        payload.GetProperty("tagId").GetInt32().Should().Be(BugId);
        payload.GetProperty("name").GetString().Should().Be("defect");
    }

    [Fact]
    public async Task UpdateTag_ShouldFail_WhenAnotherTagHasTheName()
    {
        var tool = new TagChangeTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"action":"update","tagId":{{BugId}},"name":"Feature"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTag_ShouldFail_WhenTheNameIsAlreadyProposedForCreation()
    {
        var createTag = new TagChangeTool(Mediator, ChangeSet);
        var updateTag = new TagChangeTool(Mediator, ChangeSet);

        await Execute(createTag, """{"action":"create","name":"defect"}""");

        var result = await Execute(updateTag, $$"""{"action":"update","tagId":{{BugId}},"name":"Defect"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateTag_ShouldFail_WhenNothingWouldChange()
    {
        var tool = new TagChangeTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"action":"update","tagId":{{BugId}},"name":"bug"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTag_ShouldFail_WhenTheNameIsTooShort()
    {
        var tool = new TagChangeTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"action":"update","tagId":{{BugId}},"name":"x"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTag_ShouldFail_WhenTheTagIsNotInTheWorkspace()
    {
        var tool = new TagChangeTool(Mediator, ChangeSet);
        var result = await Execute(tool, """{"action":"update","tagId":404,"name":"defect"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyUpdateTag_ShouldRenameFromTheCurrentName()
    {
        var handler = new UpdateTagChangeHandler(Mediator);
        var context = CreateContext($$"""{"tagId":{{BugId}},"name":"defect"}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        result.AppliedEntityId.Should().Be(BugId);

        var request = CapturedUpdates().Should().ContainSingle().Subject;

        request.CurrentValue.Should().Be("bug");
        request.NewValue.Should().Be("defect");
    }

    [Fact]
    public async Task ApplyUpdateTag_ShouldFail_WhenTheNameWasTakenSinceProposing()
    {
        var handler = new UpdateTagChangeHandler(Mediator);
        var context = CreateContext($$"""{"tagId":{{BugId}},"name":"FEATURE"}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        CapturedUpdates().Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyUpdateTag_ShouldFail_WhenTheTagNoLongerExists()
    {
        var handler = new UpdateTagChangeHandler(Mediator);
        var context = CreateContext("""{"tagId":404,"name":"defect"}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        CapturedUpdates().Should().BeEmpty();
    }

    [Fact]
    public async Task UndoUpdateTag_ShouldRenameTheTagBack()
    {
        var handler = new UpdateTagChangeHandler(Mediator);
        var context = CreateContext($$"""{"tagId":{{BugId}},"name":"defect"}""");

        context.Change.UndoPayload = await handler.Capture(context, TestContext.Current.CancellationToken);
        GivenTags(("defect", BugId), ("feature", FeatureId));

        var undoContext = new AiChangeUndoContext { Change = context.Change };
        var result = await handler.Revert(undoContext, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);

        var request = CapturedUpdates().Should().ContainSingle().Subject;

        request.CurrentValue.Should().Be("defect");
        request.NewValue.Should().Be("bug");
    }

    [Fact]
    public async Task UndoUpdateTag_ShouldLeaveTheTag_WhenItWasRenamedSince()
    {
        var handler = new UpdateTagChangeHandler(Mediator);
        var context = CreateContext($$"""{"tagId":{{BugId}},"name":"defect"}""");

        context.Change.UndoPayload = await handler.Capture(context, TestContext.Current.CancellationToken);
        GivenTags(("issue", BugId), ("feature", FeatureId));

        var undoContext = new AiChangeUndoContext { Change = context.Change };
        var result = await handler.Revert(undoContext, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(UpdateTagChangeHandler.ChangedSinceMessage);
        CapturedUpdates().Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTag_ShouldProposeTheDeletionWithItsTaskCount()
    {
        var tool = new TagChangeTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"action":"delete","tagId":{{FeatureId}},"reason":"Unused"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be(DeleteTagAction.Change);
        change.EntityId.Should().Be(FeatureId);
        change.Fields.Should().Contain(field => field.Name == "tasks" && field.Before == "2");
        change.Payload.RootElement.GetProperty("tagId").GetInt32().Should().Be(FeatureId);
    }

    [Fact]
    public async Task TagChange_ShouldFail_WhenTheActionIsUnknown()
    {
        var tool = new TagChangeTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"action":"merge","tagId":{{BugId}}}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public void TagChange_ShouldOfferEveryTagChange()
    {
        var tool = new TagChangeTool(Mediator, ChangeSet);

        tool.ProposedChanges.Should().BeEquivalentTo(CreateTagAction.Change, UpdateTagAction.Change, DeleteTagAction.Change);
    }

    [Fact]
    public async Task ApplyDeleteTag_ShouldDeleteTheTagByItsCurrentName()
    {
        var handler = new DeleteTagChangeHandler(Mediator);
        var context = CreateContext($$"""{"tagId":{{FeatureId}}}""");

        Mediator
            .Send(Arg.Any<DeleteTagsCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Success);

        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);

        var command = Mediator.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<DeleteTagsCommand>()
            .Should().ContainSingle().Subject;

        command.Request.Tags.Should().Equal("feature");
    }

    [Fact]
    public async Task ApplyDeleteTag_ShouldFail_WhenTheTagNoLongerExists()
    {
        var handler = new DeleteTagChangeHandler(Mediator);
        var context = CreateContext("""{"tagId":404}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
    }

    private static async Task<AiToolExecution> Execute(IAiTool tool, string json)
    {
        var arguments = JsonDocument.Parse(json).RootElement;

        return await tool.Execute(arguments, TestContext.Current.CancellationToken);
    }

    private void GivenTags(params (string Name, int Id)[] tags)
    {
        var viewModels = tags
            .Select(tag => new TagViewModel { Id = tag.Id, Name = tag.Name, OwnerId = "owner", OwnerName = "Owner", TaskCount = 2 })
            .ToList();

        Mediator
            .Send(Arg.Any<GetTagsForWorkspaceQuery>(), Arg.Any<CancellationToken>())
            .Returns(viewModels);
    }

    private void GivenUpdateSucceeds()
    {
        var response = ClientResponse<TagViewModel>.Success(new TagViewModel { Id = BugId, Name = "defect" });

        Mediator
            .Send(Arg.Any<UpdateTagCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);
    }

    private List<UpdateTagRequest> CapturedUpdates()
    {
        return Mediator.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<UpdateTagCommand>()
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
            ToolName = UpdateTagAction.Change,
            EntityType = "tag",
            EntityId = BugId,
            Summary = "Rename a tag",
            Payload = JsonDocument.Parse(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Applied,
        };

        return new AiChangeApplyContext
        {
            Change = change,
            ResolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal),
        };
    }
}
