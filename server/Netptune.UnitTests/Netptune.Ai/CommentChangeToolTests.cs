using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments.Commands;
using Netptune.Handlers.Comments.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class CommentChangeToolTests
{
    private const string SystemId = "NPT-42";
    private const string CurrentUserId = "user-1";
    private const int OwnCommentId = 11;
    private const int OtherCommentId = 12;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public CommentChangeToolTests()
    {
        Identity.GetCurrentUserId().Returns(CurrentUserId);
        GivenComments("First draft");

        Mediator
            .Send(Arg.Any<UpdateCommentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<CommentViewModel>.Success(new CommentViewModel()));
    }

    [Fact]
    public async Task Update_ShouldProposeTheEdit_OnTheUsersOwnComment()
    {
        var result = await Execute($$"""{"action":"update","systemId":"{{SystemId}}","commentId":{{OwnCommentId}},"comment":"Second draft"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be(UpdateCommentAction.Change);
        change.Fields.Should().ContainSingle(field => field.Before == "First draft" && field.After == "Second draft");
    }

    [Fact]
    public async Task Update_ShouldFail_OnSomeoneElsesComment()
    {
        var result = await Execute($$"""{"action":"update","systemId":"{{SystemId}}","commentId":{{OtherCommentId}},"comment":"Rewritten"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldAskForDeleteAny_OnSomeoneElsesComment()
    {
        var tool = new CommentChangeTool(Mediator, ChangeSet, Identity);

        await Execute($$"""{"action":"delete","systemId":"{{SystemId}}","commentId":{{OtherCommentId}}}""");
        await Execute($$"""{"action":"delete","systemId":"{{SystemId}}","commentId":{{OwnCommentId}}}""");

        var others = ChangeSet.Changes[0].Payload.RootElement;
        var own = ChangeSet.Changes[1].Payload.RootElement;

        tool.GetChangePermissions(DeleteCommentAction.Change, others).Should().Equal(NetptunePermissions.Comments.DeleteAny);
        tool.GetChangePermissions(DeleteCommentAction.Change, own).Should().Equal(NetptunePermissions.Comments.DeleteOwn);
    }

    [Fact]
    public async Task ApplyUpdate_ShouldKeepTheCommentsMentions()
    {
        var handler = new UpdateCommentChangeHandler(Mediator);
        var context = CreateContext($$"""{"systemId":"{{SystemId}}","commentId":{{OwnCommentId}},"comment":"Second draft"}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);

        var request = CapturedUpdates().Should().ContainSingle().Subject;

        request.Comment.Should().Be("Second draft");
        request.Mentions.Should().Equal("user-2");
    }

    [Fact]
    public async Task UndoUpdate_ShouldRestoreThePreviousBody()
    {
        var handler = new UpdateCommentChangeHandler(Mediator);
        var context = CreateContext($$"""{"systemId":"{{SystemId}}","commentId":{{OwnCommentId}},"comment":"Second draft"}""");

        context.Change.UndoPayload = await handler.Capture(context, TestContext.Current.CancellationToken);
        GivenComments("Second draft");

        var result = await handler.Revert(new AiChangeUndoContext { Change = context.Change }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        CapturedUpdates().Should().ContainSingle().Which.Comment.Should().Be("First draft");
    }

    [Fact]
    public async Task UndoUpdate_ShouldLeaveTheComment_WhenItWasEditedSince()
    {
        var handler = new UpdateCommentChangeHandler(Mediator);
        var context = CreateContext($$"""{"systemId":"{{SystemId}}","commentId":{{OwnCommentId}},"comment":"Second draft"}""");

        context.Change.UndoPayload = await handler.Capture(context, TestContext.Current.CancellationToken);
        GivenComments("Third draft");

        var result = await handler.Revert(new AiChangeUndoContext { Change = context.Change }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(UpdateCommentChangeHandler.ChangedSinceMessage);
        CapturedUpdates().Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyDelete_ShouldDeleteTheComment()
    {
        Mediator
            .Send(Arg.Any<DeleteCommentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Success);

        var handler = new DeleteCommentChangeHandler(Mediator);
        var context = CreateContext($$"""{"systemId":"{{SystemId}}","commentId":{{OtherCommentId}},"isOwn":false}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(Arg.Is<DeleteCommentCommand>(command => command.Id == OtherCommentId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyDelete_ShouldFail_WhenTheCommandForbidsIt()
    {
        Mediator
            .Send(Arg.Any<DeleteCommentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Forbidden);

        var handler = new DeleteCommentChangeHandler(Mediator);
        var context = CreateContext($$"""{"systemId":"{{SystemId}}","commentId":{{OtherCommentId}},"isOwn":false}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
    }

    [Fact]
    public async Task Delete_ShouldWarn_WhenTheCommentIsSomeoneElses()
    {
        var result = await Execute($$"""{"action":"delete","systemId":"{{SystemId}}","commentId":{{OtherCommentId}}}""");

        result.IsError.Should().BeFalse();
        result.Content.Should().Contain("Sam's comment");
    }

    private async Task<AiToolExecution> Execute(string json)
    {
        var tool = new CommentChangeTool(Mediator, ChangeSet, Identity);
        var arguments = JsonDocument.Parse(json).RootElement;

        return await tool.Execute(arguments, TestContext.Current.CancellationToken);
    }

    private void GivenComments(string ownBody)
    {
        var comments = new List<CommentViewModel>
        {
            new()
            {
                Id = OwnCommentId,
                UserId = CurrentUserId,
                UserDisplayName = "Joel",
                Body = ownBody,
                Mentions = [new CommentMentionViewModel { UserId = "user-2", DisplayName = "Sam" }],
            },
            new() { Id = OtherCommentId, UserId = "user-2", UserDisplayName = "Sam", Body = "Looks good" },
        };

        foreach (var comment in comments)
        {
            Mediator
                .Send(Arg.Is<GetTaskCommentQuery>(query => query.CommentId == comment.Id), Arg.Any<CancellationToken>())
                .Returns(comment);
        }
    }

    private List<UpdateCommentRequest> CapturedUpdates()
    {
        return Mediator.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<UpdateCommentCommand>()
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
            ToolName = UpdateCommentAction.Change,
            EntityType = AiCommentLookup.EntityType,
            EntityId = OwnCommentId,
            Summary = "Edit a comment",
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
