using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Pages.Queries;
using Netptune.Handlers.Tags.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class PageChangeToolTests
{
    private const int FolderId = 10;
    private const int RfcId = 11;
    private const int HandbookId = 13;
    private const int ArchivedId = 20;
    private const int OlderVersionId = 30;
    private const int CurrentVersionId = 31;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public PageChangeToolTests()
    {
        PageTestTree.Given(Mediator);

        Mediator
            .Send(Arg.Is<GetPageQuery>(query => query.Slug == "rfc"), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.Success(new PageViewModel
            {
                Id = RfcId,
                Title = "RFC",
                Slug = "rfc",
                Kind = PageKind.Document,
                Body = "# Old",
                Revision = 4,
                Status = PageStatus.Draft,
                Tags = [new PageTagViewModel { Id = 1, Name = "Backend" }],
            }));

        var manyTags = Enumerable.Range(100, 60).Select(id => new TagViewModel { Id = id, Name = $"Tag {id}" });
        List<TagViewModel> workspaceTags =
        [
            new() { Id = 1, Name = "Backend" },
            new() { Id = 2, Name = "Frontend" },
            .. manyTags,
        ];

        AiTestTags.Given(Mediator, workspaceTags);

        Mediator
            .Send(Arg.Any<GetArchivedPagesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<List<ArchivedPageViewModel>>.Success(
            [
                new ArchivedPageViewModel { Id = ArchivedId, Title = "Old plan", Slug = "old-plan", Kind = PageKind.Document },
            ]));

        Mediator
            .Send(Arg.Any<GetPageVersionQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageVersionViewModel>.NotFound);

        GivenVersion(OlderVersionId, revision: 2, title: "RFC draft", body: "# Older");
        GivenVersion(CurrentVersionId, revision: 4, title: "RFC", body: "# Old");
    }

    [Fact]
    public async Task Create_ShouldProposeADocumentUnderAPageProposedEarlier()
    {
        await Execute(new CreatePageAction(Mediator, ChangeSet), """{"title":"RFCs","kind":"folder"}""");

        var folderRef = ChangeSet.Changes[0].RefKey;
        var result = await Execute(
            new CreatePageAction(Mediator, ChangeSet),
            $$"""{"title":"RFC-015","body":"# Draft","parentRef":"{{folderRef}}"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes[1];
        var payload = change.Payload.RootElement;

        change.ToolName.Should().Be("propose_create_page");
        payload.GetProperty("kind").GetString().Should().Be("document");
        payload.GetProperty("body").GetString().Should().Be("# Draft");
        payload.GetProperty("parentRef").GetString().Should().Be(folderRef);
        change.Fields.Should().Contain(field => field.Name == "parent" && field.After == "RFCs");
    }

    [Fact]
    public async Task Create_ShouldRefuse_WhenTheParentRefWasNotProposed()
    {
        var result = await Execute(new CreatePageAction(Mediator, ChangeSet), """{"title":"RFC-015","parentRef":"ref_9"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ShouldRefuseABody_ThatLinksToTasksProposedInTheSameSet()
    {
        var result = await Execute(new CreatePageAction(Mediator, ChangeSet), """{"title":"Plan","body":"- [ ] [[task:ref:1|Docs]]"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ShouldRefuseABody_OnAFolder()
    {
        var result = await Execute(new CreatePageAction(Mediator, ChangeSet), """{"title":"RFCs","kind":"folder","body":"x"}""");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBody_ShouldProposeTheWholeBody_AgainstTheRevisionItWasReadAt()
    {
        var result = await Execute(new UpdatePageBodyAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}},"body":"# New","revision":4}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be("propose_update_page_body");
        change.EntityId.Should().Be(RfcId);
        change.Payload.RootElement.GetProperty("revision").GetInt32().Should().Be(4);
        change.Fields.Should().ContainSingle(field => field.Name == "body" && field.Before == "# Old" && field.After == "# New");
    }

    [Fact]
    public async Task UpdateBody_ShouldRefuse_WhenThePageChangedSinceTheModelReadIt()
    {
        var result = await Execute(new UpdatePageBodyAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}},"body":"# New","revision":3}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("changed since you read it");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBody_ShouldRefuse_WithoutTheRevisionThatWasRead()
    {
        var result = await Execute(new UpdatePageBodyAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}},"body":"# New"}""");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBody_ShouldWaitForTheTasksItLinksTo_WhenTheyAreProposedInTheSameSet()
    {
        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = "propose_create_task",
            EntityType = "task",
            RefKey = "ref:7",
            Summary = "Create task “Write docs”",
            Payload = JsonDocument.Parse("{}"),
        });

        var result = await Execute(
            new UpdatePageBodyAction(Mediator, ChangeSet),
            $$"""{"pageId":{{RfcId}},"body":"- [ ] Write docs [[task:ref:7|Write docs]]","revision":4}""");

        result.IsError.Should().BeFalse();

        var refs = ChangeSet.Changes[1].Payload.RootElement.GetProperty("taskRefs").EnumerateArray().Select(item => item.GetString());

        refs.Should().Equal("ref:7");
    }

    [Fact]
    public async Task UpdateBody_ShouldRefuse_WhenItLinksToATaskHandleThatWasNotProposed()
    {
        var result = await Execute(
            new UpdatePageBodyAction(Mediator, ChangeSet),
            $$"""{"pageId":{{RfcId}},"body":"- [ ] Write docs [[task:ref:9|Write docs]]","revision":4}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public void TaskHandles_ShouldBeIgnoredInsideCode()
    {
        var body = "Write `[[task:ref:1|x]]` to link.\n\n```\n[[task:ref:2|y]]\n```\n- [ ] [[task:ref:3|Docs]]";

        var handles = AiPageLookup.ReadTaskHandles(body);
        var replaced = AiPageLookup.ReplaceTaskHandles(body, new Dictionary<string, string> { ["ref:1"] = "NPT-1", ["ref:3"] = "NPT-3" });

        handles.Should().Equal("ref:3");
        replaced.Should().Contain("`[[task:ref:1|x]]`").And.Contain("[[task:NPT-3|Docs]]");
    }

    [Fact]
    public void ReadReferences_ShouldIncludeEveryHandleInAListOfRefs()
    {
        using var payload = JsonDocument.Parse("""{"parentRef":"ref:1","taskRefs":["ref:2","ref:3"],"name":"x"}""");

        var references = AiChangePayload.ReadReferences(payload.RootElement);

        references.Should().Equal("ref:1", "ref:2", "ref:3");
    }

    [Fact]
    public async Task UpdateBody_ShouldRefuse_WhenNothingChanges()
    {
        var result = await Execute(new UpdatePageBodyAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}},"body":"# Old","revision":4}""");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBody_ShouldRefuse_OnAFolder()
    {
        var result = await Execute(new UpdatePageBodyAction(Mediator, ChangeSet), $$"""{"pageId":{{FolderId}},"body":"x","revision":1}""");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Move_ShouldRefuse_WhenTheParentIsInsideThePage()
    {
        var result = await Execute(new MovePageAction(Mediator, ChangeSet), $$"""{"pageId":{{FolderId}},"parentId":{{RfcId}}}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("inside itself");
    }

    [Fact]
    public async Task Move_ShouldProposeTheNewParent_WithTheOldOneBeforeIt()
    {
        var result = await Execute(new MovePageAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}},"parentId":{{HandbookId}}}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Single().Fields.Should().ContainSingle(field =>
            field.Name == "parent" && field.Before == "Engineering" && field.After == "Handbook");
    }

    [Fact]
    public async Task Move_ShouldRefuse_WhenThePageIsAlreadyThere()
    {
        var result = await Execute(new MovePageAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}},"parentId":{{FolderId}}}""");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateDetails_ShouldProposeStatusAndTags_WithWhatTheyWereBefore()
    {
        var result = await Execute(
            new UpdatePageDetailsAction(Mediator, ChangeSet),
            $$"""{"pageId":{{RfcId}},"status":"published","tags":["Backend","Frontend"]}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;
        var payload = change.Payload.RootElement;
        var tags = change.Fields.Single(field => field.Name == "tags");

        change.ToolName.Should().Be("propose_update_page_details");
        change.EntityId.Should().Be(RfcId);
        change.Fields.Should().Contain(field => field.Name == "status" && field.Before == "draft" && field.After == "published");
        tags.BeforeValues!.Select(value => value.Display).Should().Equal("Backend");
        tags.AfterValues!.Select(value => value.Display).Should().Equal("Backend", "Frontend");
        payload.GetProperty("status").GetString().Should().Be("published");
        payload.GetProperty("tags").EnumerateArray().Select(item => item.GetString()).Should().Equal("Backend", "Frontend");
    }

    [Fact]
    public async Task UpdateDetails_ShouldLeaveOutTheStatus_WhenItIsAlreadySet()
    {
        var result = await Execute(
            new UpdatePageDetailsAction(Mediator, ChangeSet),
            $$"""{"pageId":{{RfcId}},"status":"draft","tags":[]}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Single();

        change.Fields.Select(field => field.Name).Should().Equal("tags");
        change.Payload.RootElement.TryGetProperty("status", out _).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDetails_ShouldAcceptATagProposedInTheSameSet()
    {
        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = "propose_create_tag",
            EntityType = "tag",
            Summary = "Create tag “Ops”",
            Payload = JsonDocument.Parse("""{"name":"Ops"}"""),
        });

        var result = await Execute(new UpdatePageDetailsAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}},"tags":["Ops"]}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateDetails_ShouldAcceptATagBeyondTheFirstPageOfWorkspaceTags()
    {
        var result = await Execute(new UpdatePageDetailsAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}},"tags":["Tag 157"]}""");

        result.IsError.Should().BeFalse();
        await Mediator.DidNotReceive().Send(Arg.Any<GetTagsForWorkspaceQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_ShouldOnlyLookUpTagsThatAreNotProposedInTheSameSet()
    {
        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = "propose_create_tag",
            EntityType = "tag",
            Summary = "Create tag “Ops”",
            Payload = JsonDocument.Parse("""{"name":"Ops"}"""),
        });

        await Execute(new UpdatePageDetailsAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}},"tags":["Ops","Frontend"]}""");

        await Mediator.Received(1).Send(
            Arg.Is<GetTagsByNameQuery>(query => query.Names.SequenceEqual(new[] { "Frontend" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_ShouldTargetAPageProposedEarlier_ByItsHandle()
    {
        await Execute(new CreatePageAction(Mediator, ChangeSet), """{"title":"RFC-015"}""");

        var pageRef = ChangeSet.Changes[0].RefKey;
        var result = await Execute(
            new UpdatePageDetailsAction(Mediator, ChangeSet),
            $$"""{"pageRef":"{{pageRef}}","status":"published"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes[1];

        change.EntityId.Should().BeNull();
        change.Payload.RootElement.GetProperty("pageRef").GetString().Should().Be(pageRef);
        change.Summary.Should().Contain("RFC-015");
    }

    [Theory]
    [InlineData("""{"pageId":11,"tags":["Nope"]}""", "do not exist")]
    [InlineData("""{"pageId":11,"status":"archived"}""", "draft or published")]
    [InlineData("""{"pageId":11,"tags":"Backend"}""", "array")]
    [InlineData("""{"pageId":11}""", "status, tags")]
    [InlineData("""{"pageId":11,"status":"draft","tags":["Backend"]}""", "already has")]
    [InlineData("""{"pageId":99,"status":"published"}""", "not in this workspace")]
    [InlineData("""{"pageRef":"ref:9","status":"published"}""", "does not match")]
    public async Task UpdateDetails_ShouldRefuse_WhatItCannotApply(string arguments, string message)
    {
        var result = await Execute(new UpdatePageDetailsAction(Mediator, ChangeSet), arguments);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain(message);
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Archive_ShouldSayHowManyPagesInsideGoWithIt()
    {
        var result = await Execute(new ArchivePageAction(Mediator, ChangeSet), $$"""{"pageId":{{FolderId}}}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be("propose_archive_page");
        change.EntityId.Should().Be(FolderId);
        change.Summary.Should().Be("Archive “Engineering” and the 2 pages inside it");
        change.Fields.Should().ContainSingle(field => field.Name == "name" && field.Before == "Engineering" && field.After == null);
    }

    [Fact]
    public async Task Archive_ShouldNotMentionPagesInside_WhenThereAreNone()
    {
        await Execute(new ArchivePageAction(Mediator, ChangeSet), $$"""{"pageId":{{HandbookId}}}""");

        ChangeSet.Changes.Single().Summary.Should().Be("Archive “Handbook”");
    }

    [Fact]
    public async Task Archive_ShouldRefuse_APageThatIsNotLive()
    {
        var result = await Execute(new ArchivePageAction(Mediator, ChangeSet), $$"""{"pageId":{{ArchivedId}}}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Restore_ShouldProposeBringingBackAnArchivedPage()
    {
        var result = await Execute(new RestorePageAction(Mediator, ChangeSet), $$"""{"pageId":{{ArchivedId}}}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be("propose_restore_page");
        change.EntityId.Should().Be(ArchivedId);
        change.Fields.Should().ContainSingle(field => field.Name == "name" && field.Before == null && field.After == "Old plan");
    }

    [Fact]
    public async Task Restore_ShouldRefuse_APageThatIsNotArchived()
    {
        var result = await Execute(new RestorePageAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}}}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("list_archived_pages");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldSayPlainlyThatItCannotBeUndone()
    {
        var result = await Execute(new DeletePageAction(Mediator, ChangeSet), $$"""{"pageId":{{ArchivedId}}}""");

        result.IsError.Should().BeFalse();
        result.Content.Should().Contain("cannot be undone");

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be("propose_delete_page");
        change.Summary.Should().StartWith("Permanently delete “Old plan”").And.Contain("cannot be undone");
        change.Payload.RootElement.GetProperty("pageId").GetInt32().Should().Be(ArchivedId);
    }

    [Fact]
    public async Task Delete_ShouldRefuse_ALivePage()
    {
        var result = await Execute(new DeletePageAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}}}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("archive a live page instead");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task RestoreVersion_ShouldProposeTheVersionsTitleAndBody_AgainstTheCurrentRevision()
    {
        var result = await Execute(
            new RestorePageVersionAction(Mediator, ChangeSet),
            $$"""{"pageId":{{RfcId}},"versionId":{{OlderVersionId}}}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;
        var payload = change.Payload.RootElement;

        change.ToolName.Should().Be("propose_restore_page_version");
        change.EntityId.Should().Be(RfcId);
        change.Fields.Should().Contain(field => field.Name == "name" && field.Before == "RFC" && field.After == "RFC draft");
        change.Fields.Should().Contain(field => field.Name == "body" && field.Before == "# Old" && field.After == "# Older");
        payload.GetProperty("versionId").GetInt32().Should().Be(OlderVersionId);
        payload.GetProperty("revision").GetInt32().Should().Be(4);
    }

    [Theory]
    [InlineData(CurrentVersionId, "as it is now")]
    [InlineData(99, "list_page_versions")]
    public async Task RestoreVersion_ShouldRefuse_AVersionItCannotRestore(int versionId, string message)
    {
        var result = await Execute(
            new RestorePageVersionAction(Mediator, ChangeSet),
            $$"""{"pageId":{{RfcId}},"versionId":{{versionId}}}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain(message);
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task RestoreVersion_ShouldRefuse_WithoutAVersionId()
    {
        var result = await Execute(new RestorePageVersionAction(Mediator, ChangeSet), $$"""{"pageId":{{RfcId}}}""");

        result.IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData("propose_create_page", NetptunePermissions.Pages.Create)]
    [InlineData("propose_update_page_body", NetptunePermissions.Pages.Update)]
    [InlineData("propose_move_page", NetptunePermissions.Pages.Update)]
    [InlineData("propose_update_page_details", NetptunePermissions.Pages.Update)]
    [InlineData("propose_archive_page", NetptunePermissions.Pages.Delete)]
    [InlineData("propose_restore_page", NetptunePermissions.Pages.Delete)]
    [InlineData("propose_delete_page", NetptunePermissions.Pages.Delete)]
    [InlineData("propose_restore_page_version", NetptunePermissions.Pages.Update)]
    public void GetChangePermissions_ShouldAskForWhatEachChangeNeeds(string changeName, string permission)
    {
        var tool = new PageChangeTool(Mediator, ChangeSet);
        var payload = JsonDocument.Parse("{}").RootElement;

        tool.GetChangePermissions(changeName, payload).Should().BeEquivalentTo([permission]);
    }

    [Theory]
    [InlineData("create", NetptunePermissions.Pages.Create)]
    [InlineData("update_body", NetptunePermissions.Pages.Update)]
    [InlineData("move", NetptunePermissions.Pages.Update)]
    [InlineData("update_details", NetptunePermissions.Pages.Update)]
    [InlineData("archive", NetptunePermissions.Pages.Delete)]
    [InlineData("restore", NetptunePermissions.Pages.Delete)]
    [InlineData("delete", NetptunePermissions.Pages.Delete)]
    [InlineData("restore_version", NetptunePermissions.Pages.Update)]
    public void GetRequiredPermissions_ShouldAskForWhatEachActionNeeds(string action, string permission)
    {
        var tool = new PageChangeTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse($$"""{"action":"{{action}}"}""").RootElement;

        tool.GetRequiredPermissions(arguments).Should().BeEquivalentTo([permission]);
    }

    [Fact]
    public void GetRequiredPermissions_ShouldAskForTagsAssign_WhenUpdateDetailsSetsTags()
    {
        var tool = new PageChangeTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse("""{"action":"update_details","pageId":11,"tags":["Backend"]}""").RootElement;

        tool.GetRequiredPermissions(arguments).Should().BeEquivalentTo([NetptunePermissions.Pages.Update, NetptunePermissions.Tags.Assign]);
    }

    [Fact]
    public void GetChangePermissions_ShouldAskForTagsAssign_WhenTheDetailsChangeSetsTags()
    {
        var tool = new PageChangeTool(Mediator, ChangeSet);
        var payload = JsonDocument.Parse("""{"pageId":11,"tags":[]}""").RootElement;

        tool.GetChangePermissions("propose_update_page_details", payload)
            .Should().BeEquivalentTo([NetptunePermissions.Pages.Update, NetptunePermissions.Tags.Assign]);
    }

    [Fact]
    public void IsAvailable_ShouldStillOfferStatusChanges_WithoutTagsAssign()
    {
        var tool = new PageChangeTool(Mediator, ChangeSet);
        var permissions = new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Update };

        tool.IsAvailable(permissions).Should().BeTrue();
        tool.RequiredPermissions.Should().NotContain(NetptunePermissions.Tags.Assign);
    }

    private void GivenVersion(int id, int revision, string title, string body)
    {
        var version = new PageVersionViewModel { Id = id, PageId = RfcId, Revision = revision, Title = title, Body = body };

        Mediator
            .Send(Arg.Is<GetPageVersionQuery>(query => query.PageId == RfcId && query.VersionId == id), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageVersionViewModel>.Success(version));
    }

    private static async Task<AiToolExecution> Execute(IAiToolAction action, string arguments)
    {
        var element = JsonDocument.Parse(arguments).RootElement;

        return await action.Execute(element, TestContext.Current.CancellationToken);
    }
}

public static class PageTestTree
{
    public static void Given(IMediator mediator)
    {
        List<PageTreeNodeViewModel> tree =
        [
            new() { Id = 10, ParentId = null, Kind = PageKind.Folder, Title = "Engineering", Slug = "engineering", SortOrder = 1 },
            new() { Id = 11, ParentId = 10, Kind = PageKind.Document, Title = "RFC", Slug = "rfc", SortOrder = 1 },
            new() { Id = 12, ParentId = 10, Kind = PageKind.Document, Title = "Other", Slug = "other", SortOrder = 4 },
            new() { Id = 13, ParentId = null, Kind = PageKind.Document, Title = "Handbook", Slug = "handbook", SortOrder = 2 },
        ];

        mediator
            .Send(Arg.Any<GetPageTreeQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<List<PageTreeNodeViewModel>>.Success(tree));
    }
}
