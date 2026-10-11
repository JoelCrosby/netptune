using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class PageChangeToolTests
{
    private const int FolderId = 10;
    private const int RfcId = 11;
    private const int HandbookId = 13;

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
            }));
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

    [Theory]
    [InlineData("propose_create_page", NetptunePermissions.Pages.Create)]
    [InlineData("propose_update_page_body", NetptunePermissions.Pages.Update)]
    [InlineData("propose_move_page", NetptunePermissions.Pages.Update)]
    public void GetChangePermissions_ShouldAskForWhatEachChangeNeeds(string changeName, string permission)
    {
        var tool = new PageChangeTool(Mediator, ChangeSet);
        var payload = JsonDocument.Parse("{}").RootElement;

        tool.GetChangePermissions(changeName, payload).Should().BeEquivalentTo([permission]);
    }

    private static async Task<AiToolExecution> Execute(IAiToolAction action, string arguments)
    {
        var element = JsonDocument.Parse(arguments).RootElement;

        return await action.Execute(element, TestContext.Current.CancellationToken);
    }
}

public static class PageTestTree
{
    // Engineering (10) holds RFC (11, order 1) and Other (12, order 4); Handbook (13) sits at the top level.
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
