using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Pages.Commands;
using Netptune.Handlers.Pages.Queries;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class PageChangeHandlerTests
{
    private const int RfcId = 11;

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    public PageChangeHandlerTests()
    {
        PageTestTree.Given(Mediator);
    }

    [Fact]
    public async Task Create_ShouldCreateThePageWithItsBody_InOneCommand()
    {
        GivenCreated(50, revision: 1);

        var context = CreateContext("propose_create_page", null, """{"title":"RFC-015","kind":"document","body":"# Draft"}""");
        var result = await new CreatePageChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        result.AppliedEntityId.Should().Be(50);
        await Mediator.Received(1).Send(
            Arg.Is<CreatePageCommand>(command => command.Request.Body == "# Draft" && command.Request.Title == "RFC-015"),
            Arg.Any<CancellationToken>());
        await Mediator.DidNotReceive().Send(Arg.Any<SavePageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldFail_WhenThePageCannotBeCreated()
    {
        Mediator
            .Send(Arg.Any<CreatePageCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.Failed("Pages can be at most 1 MB of text."));

        var context = CreateContext("propose_create_page", null, """{"title":"RFC-015","kind":"document","body":"x"}""");
        var result = await new CreatePageChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be("Pages can be at most 1 MB of text.");
    }

    [Fact]
    public async Task Create_Undo_ShouldArchiveTheCreatedPage()
    {
        Mediator.Send(Arg.Any<ArchivePageCommand>(), Arg.Any<CancellationToken>()).Returns(ClientResponse.Success);

        var change = CreateContext("propose_create_page", null, "{}").Change;

        change.AppliedEntityId = 13;

        var result = await new CreatePageChangeHandler(Mediator).Revert(
            new AiChangeUndoContext { Change = change },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(Arg.Is<ArchivePageCommand>(command => command.Id == 13), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Undo_ShouldLeaveAFolderAlone_WhenPagesWereAddedInsideIt()
    {
        var change = CreateContext("propose_create_page", null, "{}").Change;

        change.AppliedEntityId = 10;

        var result = await new CreatePageChangeHandler(Mediator).Revert(
            new AiChangeUndoContext { Change = change },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(CreatePageChangeHandler.HoldsOtherPagesMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<ArchivePageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBody_ShouldFail_WhenThePageChangedSinceTheProposal()
    {
        Mediator
            .Send(Arg.Any<SavePageCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.Conflict(new PageViewModel { Title = "RFC", Slug = "rfc" }, "changed"));

        var context = CreateContext("propose_update_page_body", RfcId, """{"pageId":11,"title":"RFC","body":"# New","revision":4}""");
        var result = await new UpdatePageBodyChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(UpdatePageBodyChangeHandler.EditedSinceMessage);
    }

    [Fact]
    public async Task UpdateBody_ShouldLinkToTheTasksCreatedBeforeIt_ByTheirSystemIds()
    {
        Mediator.Send(Arg.Any<SavePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));
        Mediator
            .Send(Arg.Is<GetTaskSystemIdsQuery>(query => query.TaskIds.SequenceEqual(new List<int> { 99 })), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, string> { [99] = "NPT-9" });

        var payload = """{"pageId":11,"title":"RFC","body":"- [ ] Write docs [[task:ref:1|Write docs]]","revision":4,"taskRefs":["ref:1"]}""";
        var context = CreateContext("propose_update_page_body", RfcId, payload) with
        {
            ResolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal) { ["ref:1"] = 99 },
        };

        var result = await new UpdatePageBodyChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<SavePageCommand>(command => command.Request.Body == "- [ ] Write docs [[task:NPT-9|Write docs]]"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBody_ShouldFail_RatherThanSaveAHandleThatDidNotResolve()
    {
        Mediator
            .Send(Arg.Any<GetTaskSystemIdsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, string>());

        var payload = """{"pageId":11,"title":"RFC","body":"- [ ] Docs [[task:ref:1|Docs]]","revision":4,"taskRefs":["ref:1"]}""";
        var context = CreateContext("propose_update_page_body", RfcId, payload) with
        {
            ResolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal) { ["ref:1"] = 99 },
        };

        var result = await new UpdatePageBodyChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(UpdatePageBodyChangeHandler.UnresolvedTasksMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<SavePageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBody_Undo_ShouldRestoreTheCapturedBody_WhenNobodyEditedSince()
    {
        GivenCurrentRfc(revision: 5);
        Mediator.Send(Arg.Any<SavePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var change = CreateContext("propose_update_page_body", RfcId, "{}").Change;

        change.UndoPayload = Snapshot(revision: 4);

        var result = await new UpdatePageBodyChangeHandler(Mediator).Revert(
            new AiChangeUndoContext { Change = change },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<SavePageCommand>(command => command.Request.Body == "# Old" && command.Request.Revision == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBody_Undo_ShouldRefuse_WhenSomeoneEditedAfterTheApply()
    {
        GivenCurrentRfc(revision: 7);

        var change = CreateContext("propose_update_page_body", RfcId, "{}").Change;

        change.UndoPayload = Snapshot(revision: 4);

        var result = await new UpdatePageBodyChangeHandler(Mediator).Revert(
            new AiChangeUndoContext { Change = change },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(AiPageUndo.EditedSinceApplyMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<SavePageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Move_ShouldPutThePageLastAmongItsNewSiblings()
    {
        Mediator.Send(Arg.Any<MovePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(13));

        var context = CreateContext("propose_move_page", 13, """{"pageId":13,"parentId":10}""");
        var result = await new MovePageChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<MovePageCommand>(command => command.Id == 13 && command.Request.ParentId == 10 && command.Request.SortOrder == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Move_Undo_ShouldPutThePageBackWhereItWas()
    {
        Mediator.Send(Arg.Any<MovePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var change = CreateContext("propose_move_page", RfcId, """{"pageId":11,"parentId":10}""").Change;

        change.UndoPayload = JsonSerializer.SerializeToDocument(new { pageId = RfcId, title = "RFC", parentId = (int?)null, sortOrder = 3 });

        var result = await new MovePageChangeHandler(Mediator).Revert(
            new AiChangeUndoContext { Change = change },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<MovePageCommand>(command => command.Id == RfcId && command.Request.ParentId == null && command.Request.SortOrder == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Move_Undo_ShouldRefuse_WhenThePageWasMovedAgainSince()
    {
        var change = CreateContext("propose_move_page", RfcId, """{"pageId":11,"parentId":13}""").Change;

        change.UndoPayload = JsonSerializer.SerializeToDocument(new { pageId = RfcId, title = "RFC", parentId = (int?)null, sortOrder = 3 });

        var result = await new MovePageChangeHandler(Mediator).Revert(
            new AiChangeUndoContext { Change = change },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(MovePageChangeHandler.MovedSinceMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<MovePageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_ShouldResolveTagNamesToIds_AndSendTheStatus()
    {
        GivenWorkspaceTags();
        Mediator.Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var context = CreateContext("propose_update_page_details", RfcId, """{"pageId":11,"status":"published","tags":["backend","Frontend"]}""");
        var result = await new UpdatePageDetailsChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<UpdatePageCommand>(command =>
                command.Id == RfcId
                && command.Request.Status == PageStatus.Published
                && command.Request.TagIds!.SequenceEqual(new List<int> { 1, 2 })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_ShouldLeaveTagsAlone_WhenOnlyTheStatusChanges()
    {
        Mediator.Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var context = CreateContext("propose_update_page_details", RfcId, """{"pageId":11,"status":"draft"}""");

        await new UpdatePageDetailsChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        await Mediator.Received(1).Send(
            Arg.Is<UpdatePageCommand>(command => command.Request.Status == PageStatus.Draft && command.Request.TagIds == null),
            Arg.Any<CancellationToken>());
        await Mediator.DidNotReceive().Send(Arg.Any<GetTagsByNameQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_ShouldApplyToAPageCreatedEarlierInTheSet()
    {
        Mediator.Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(50));

        var context = CreateContext("propose_update_page_details", null, """{"pageRef":"ref:1","status":"published"}""") with
        {
            ResolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal) { ["ref:1"] = 50 },
        };

        var result = await new UpdatePageDetailsChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        result.AppliedEntityId.Should().Be(50);
        await Mediator.Received(1).Send(Arg.Is<UpdatePageCommand>(command => command.Id == 50), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_ShouldFail_WhenATagWasDeletedSinceTheProposal()
    {
        GivenWorkspaceTags();

        var context = CreateContext("propose_update_page_details", RfcId, """{"pageId":11,"tags":["Backend","Gone"]}""");
        var result = await new UpdatePageDetailsChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be("These tags do not exist in this workspace: Gone. Was the change that creates them applied?");
        await Mediator.DidNotReceive().Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_Undo_ShouldPutBackTheCapturedStatusAndTags()
    {
        GivenCurrentRfc(revision: 4, status: PageStatus.Draft, tagIds: [1]);
        Mediator.Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var handler = new UpdatePageDetailsChangeHandler(Mediator);
        var context = CreateContext("propose_update_page_details", RfcId, """{"pageId":11,"status":"published","tags":[]}""");
        var change = context.Change;

        change.UndoPayload = await handler.Capture(context, TestContext.Current.CancellationToken);
        GivenCurrentRfc(revision: 4, status: PageStatus.Published);

        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        handler.UndoPermissions.Should().BeEquivalentTo([NetptunePermissions.Pages.Update]);
        await Mediator.Received(1).Send(
            Arg.Is<UpdatePageCommand>(command =>
                command.Id == RfcId
                && command.Request.Status == PageStatus.Draft
                && command.Request.TagIds!.SequenceEqual(new List<int> { 1 })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_Undo_ShouldOnlyRestoreWhatTheChangeSet()
    {
        GivenCurrentRfc(revision: 4, status: PageStatus.Draft, tagIds: [1]);
        Mediator.Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var handler = new UpdatePageDetailsChangeHandler(Mediator);
        var context = CreateContext("propose_update_page_details", RfcId, """{"pageId":11,"tags":[]}""");
        var change = context.Change;

        change.UndoPayload = await handler.Capture(context, TestContext.Current.CancellationToken);
        GivenCurrentRfc(revision: 4, status: PageStatus.Published);

        await handler.Revert(new AiChangeUndoContext { Change = change }, TestContext.Current.CancellationToken);

        await Mediator.Received(1).Send(
            Arg.Is<UpdatePageCommand>(command => command.Request.Status == null && command.Request.TagIds!.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_ShouldResolveATagCreatedEarlierInTheSameSet()
    {
        GivenWorkspaceTags(new TagViewModel { Id = 77, Name = "Ops" });
        Mediator.Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var context = CreateContext("propose_update_page_details", RfcId, """{"pageId":11,"tags":["Ops"]}""");
        var result = await new UpdatePageDetailsChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<UpdatePageCommand>(command => command.Request.TagIds!.SequenceEqual(new List<int> { 77 })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_ShouldResolveATagBeyondTheFirstPageOfWorkspaceTags()
    {
        var manyTags = Enumerable.Range(100, 60).Select(id => new TagViewModel { Id = id, Name = $"Tag {id}" }).ToArray();

        GivenWorkspaceTags(manyTags);
        Mediator.Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var context = CreateContext("propose_update_page_details", RfcId, """{"pageId":11,"tags":["tag 157"]}""");
        var result = await new UpdatePageDetailsChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<GetTagsByNameQuery>(query => query.Names.SequenceEqual(new[] { "tag 157" })),
            Arg.Any<CancellationToken>());
        await Mediator.DidNotReceive().Send(Arg.Any<GetTagsForWorkspaceQuery>(), Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<UpdatePageCommand>(command => command.Request.TagIds!.SequenceEqual(new List<int> { 157 })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_Undo_ShouldRefuse_WhenTheStatusChangedSinceTheApply()
    {
        GivenCurrentRfc(revision: 4, status: PageStatus.Draft);

        var handler = new UpdatePageDetailsChangeHandler(Mediator);
        var context = CreateContext("propose_update_page_details", RfcId, """{"pageId":11,"status":"published"}""");
        var change = context.Change;

        change.UndoPayload = await handler.Capture(context, TestContext.Current.CancellationToken);

        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(UpdatePageDetailsChangeHandler.ChangedSinceMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_Undo_ShouldRefuse_WhenSomeoneTaggedThePageSinceTheApply()
    {
        GivenCurrentRfc(revision: 4, status: PageStatus.Draft);

        var handler = new UpdatePageDetailsChangeHandler(Mediator);
        var context = CreateContext("propose_update_page_details", RfcId, """{"pageId":11,"status":"published","tags":["Tag 1"]}""");
        var change = context.Change;

        change.UndoPayload = await handler.Capture(context, TestContext.Current.CancellationToken);
        GivenCurrentRfc(revision: 4, status: PageStatus.Published, tagIds: [1, 9]);

        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(UpdatePageDetailsChangeHandler.ChangedSinceMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<UpdatePageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Archive_ShouldArchiveThePage()
    {
        Mediator.Send(Arg.Any<ArchivePageCommand>(), Arg.Any<CancellationToken>()).Returns(ClientResponse.Success);

        var context = CreateContext("propose_archive_page", 10, """{"pageId":10}""");
        var result = await new ArchivePageChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        result.AppliedEntityId.Should().Be(10);
        await Mediator.Received(1).Send(Arg.Is<ArchivePageCommand>(command => command.Id == 10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Archive_ShouldExplainARefusal_WhenThePageIsSomeoneElses()
    {
        Mediator.Send(Arg.Any<ArchivePageCommand>(), Arg.Any<CancellationToken>()).Returns(ClientResponse.Forbidden);

        var context = CreateContext("propose_archive_page", 10, """{"pageId":10}""");
        var result = await new ArchivePageChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(ArchivePageChangeHandler.NotYourPageMessage);
    }

    [Fact]
    public async Task Archive_Undo_ShouldRestoreThePage()
    {
        Mediator.Send(Arg.Any<RestorePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(10));

        var handler = new ArchivePageChangeHandler(Mediator);
        var change = CreateContext("propose_archive_page", 10, """{"pageId":10}""").Change;

        change.AppliedEntityId = 10;

        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        handler.UndoPermissions.Should().BeEquivalentTo([NetptunePermissions.Pages.Delete]);
        await Mediator.Received(1).Send(Arg.Is<RestorePageCommand>(command => command.Id == 10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restore_ShouldRestoreThePage()
    {
        Mediator.Send(Arg.Any<RestorePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(20));

        var context = CreateContext("propose_restore_page", 20, """{"pageId":20}""");
        var result = await new RestorePageChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(Arg.Is<RestorePageCommand>(command => command.Id == 20), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restore_Undo_ShouldArchiveThePageAgain()
    {
        Mediator.Send(Arg.Any<ArchivePageCommand>(), Arg.Any<CancellationToken>()).Returns(ClientResponse.Success);

        var handler = new RestorePageChangeHandler(Mediator);
        var change = CreateContext("propose_restore_page", 20, """{"pageId":20}""").Change;

        change.AppliedEntityId = 20;

        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        handler.UndoPermissions.Should().BeEquivalentTo([NetptunePermissions.Pages.Delete]);
        await Mediator.Received(1).Send(Arg.Is<ArchivePageCommand>(command => command.Id == 20), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldDeleteTheArchivedPagePermanently()
    {
        Mediator.Send(Arg.Any<DeleteArchivedPageCommand>(), Arg.Any<CancellationToken>()).Returns(ClientResponse.Success);

        var context = CreateContext("propose_delete_page", 20, """{"pageId":20}""");
        var result = await new DeletePageChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(Arg.Is<DeleteArchivedPageCommand>(command => command.Id == 20), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Delete_ShouldNotBeUndoable()
    {
        var handler = new DeletePageChangeHandler(Mediator);
        var catalog = new AiUndoCatalog([handler, new ArchivePageChangeHandler(Mediator)]);

        handler.Should().NotBeAssignableTo<IAiChangeUndoHandler>();
        catalog.CanUndo("propose_delete_page").Should().BeFalse();
        catalog.CanUndo("propose_archive_page").Should().BeTrue();
    }

    [Fact]
    public async Task RestoreVersion_ShouldRestoreTheVersion_AtTheRevisionItWasProposedAgainst()
    {
        Mediator.Send(Arg.Any<RestorePageVersionCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var context = CreateContext("propose_restore_page_version", RfcId, """{"pageId":11,"versionId":30,"revision":4}""");
        var result = await new RestorePageVersionChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<RestorePageVersionCommand>(command =>
                command.PageId == RfcId && command.VersionId == 30 && command.ExpectedRevision == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreVersion_ShouldFail_WhenThePageChangedSinceTheProposal()
    {
        Mediator
            .Send(Arg.Any<RestorePageVersionCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.Conflict(new PageViewModel { Title = "RFC", Slug = "rfc" }, "changed"));

        var context = CreateContext("propose_restore_page_version", RfcId, """{"pageId":11,"versionId":30,"revision":4}""");
        var result = await new RestorePageVersionChangeHandler(Mediator).Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(UpdatePageBodyChangeHandler.EditedSinceMessage);
    }

    [Fact]
    public async Task RestoreVersion_Undo_ShouldPutBackTheCapturedTitleAndBody()
    {
        GivenCurrentRfc(revision: 5);
        Mediator.Send(Arg.Any<SavePageCommand>(), Arg.Any<CancellationToken>()).Returns(Saved(RfcId));

        var handler = new RestorePageVersionChangeHandler(Mediator);
        var change = CreateContext("propose_restore_page_version", RfcId, "{}").Change;

        change.UndoPayload = Snapshot(revision: 4);

        var result = await handler.Revert(new AiChangeUndoContext { Change = change }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        handler.UndoPermissions.Should().BeEquivalentTo([NetptunePermissions.Pages.Update]);
        await Mediator.Received(1).Send(
            Arg.Is<SavePageCommand>(command => command.Request.Title == "RFC" && command.Request.Body == "# Old" && command.Request.Revision == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreVersion_Undo_ShouldRefuse_WhenSomeoneEditedAfterTheApply()
    {
        GivenCurrentRfc(revision: 7);

        var change = CreateContext("propose_restore_page_version", RfcId, "{}").Change;

        change.UndoPayload = Snapshot(revision: 4);

        var result = await new RestorePageVersionChangeHandler(Mediator).Revert(
            new AiChangeUndoContext { Change = change },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be(AiPageUndo.EditedSinceApplyMessage);
        await Mediator.DidNotReceive().Send(Arg.Any<SavePageCommand>(), Arg.Any<CancellationToken>());
    }

    private void GivenCreated(int id, int revision)
    {
        Mediator
            .Send(Arg.Any<CreatePageCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.Success(new PageViewModel { Id = id, Title = "RFC-015", Slug = "rfc-015", Revision = revision }));
    }

    private void GivenCurrentRfc(int revision, PageStatus status = PageStatus.Draft, List<int>? tagIds = null)
    {
        var tags = (tagIds ?? []).Select(id => new PageTagViewModel { Id = id, Name = $"Tag {id}" }).ToList();
        var page = new PageViewModel { Id = RfcId, Title = "RFC", Slug = "rfc", Revision = revision, Status = status, Tags = tags };

        Mediator
            .Send(Arg.Is<GetPageQuery>(query => query.Slug == "rfc"), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.Success(page));
    }

    private void GivenWorkspaceTags(params TagViewModel[] extra)
    {
        List<TagViewModel> tags =
        [
            new() { Id = 1, Name = "Backend" },
            new() { Id = 2, Name = "Frontend" },
            .. extra,
        ];

        AiTestTags.Given(Mediator, tags);
    }

    private static ClientResponse<PageViewModel> Saved(int id)
    {
        return ClientResponse<PageViewModel>.Success(new PageViewModel { Id = id, Title = "Saved", Slug = "saved" });
    }

    private static JsonDocument Snapshot(int revision)
    {
        return JsonSerializer.SerializeToDocument(new { pageId = RfcId, title = "RFC", body = "# Old", revision });
    }

    private static AiChangeApplyContext CreateContext(string toolName, int? entityId, string payload)
    {
        var change = new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = toolName,
            EntityType = "page",
            EntityId = entityId,
            Summary = "Change a page",
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
