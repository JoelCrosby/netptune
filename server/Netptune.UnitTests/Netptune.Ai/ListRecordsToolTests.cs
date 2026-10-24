using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Boards.Queries;
using Netptune.Handlers.Projects.Queries;
using Netptune.Handlers.RelationTypes.Queries;
using Netptune.Handlers.ServiceAccounts.Queries;
using Netptune.Handlers.Sprints.Queries;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Users.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class ListRecordsToolTests
{
    private static readonly string[] AllKinds =
    [
        "statuses",
        "tags",
        "projects",
        "relation_types",
        "board_groups",
        "boards",
        "sprints",
        "members",
        "member_roles",
    ];

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Theory]
    [InlineData("statuses", NetptunePermissions.Statuses.Read)]
    [InlineData("tags", NetptunePermissions.Tags.Read)]
    [InlineData("projects", NetptunePermissions.Projects.Read)]
    [InlineData("relation_types", NetptunePermissions.RelationTypes.Read)]
    [InlineData("board_groups", NetptunePermissions.BoardGroups.Read)]
    [InlineData("boards", NetptunePermissions.Boards.Read)]
    [InlineData("sprints", NetptunePermissions.Sprints.Read)]
    [InlineData("members", NetptunePermissions.Members.Read)]
    [InlineData("member_roles", NetptunePermissions.Members.Read)]
    [InlineData("automation_service_accounts", NetptunePermissions.ServiceAccounts.Read)]
    public void GetRequiredPermissions_ShouldAskOnlyForTheKindBeingListed(string kind, string expected)
    {
        var tool = new ListRecordsTool(Mediator);
        var required = tool.GetRequiredPermissions(Arguments($$"""{"kind":"{{kind}}"}"""));

        required.Should().BeEquivalentTo([expected]);
    }

    [Fact]
    public void IsAvailable_ShouldOfferTheTool_WhenAnyKindCanBeRead()
    {
        var tool = new ListRecordsTool(Mediator);
        var permissions = new HashSet<string> { NetptunePermissions.Tags.Read };

        tool.IsAvailable(permissions).Should().BeTrue("a member who can read only tags can still list them");
        tool.IsAvailable(new HashSet<string>()).Should().BeFalse();
    }

    [Theory]
    [InlineData("""{"kind":"tags"}""", "list_records · tags")]
    [InlineData("""{"kind":"labels"}""", "list_records")]
    [InlineData("{}", "list_records")]
    public void DescribeCall_ShouldNameTheKind_OnlyWhenItIsOneTheToolKnows(string json, string expected)
    {
        var tool = new ListRecordsTool(Mediator);

        tool.DescribeCall(Arguments(json)).Should().Be(expected);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheKindIsUnknown()
    {
        var tool = new ListRecordsTool(Mediator);
        var result = await tool.Execute(Arguments("""{"kind":"labels"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("statuses, tags, projects");
    }

    [Fact]
    public async Task Execute_ShouldListAPageOfTags_AndSayWhetherThereAreMore()
    {
        var page = new PagedResponse<TagViewModel>([new TagViewModel { Id = 1, Name = "bug" }], 2, 1, 3);

        Mediator
            .Send(Arg.Any<GetTagsPageQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<TagViewModel>>.Success(page));

        var tool = new ListRecordsTool(Mediator);
        var arguments = Arguments("""{"kind":"tags","page":2,"pageSize":1,"search":" bu "}""");
        var result = await tool.Execute(arguments, TestContext.Current.CancellationToken);

        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;

        root.GetProperty("totalCount").GetInt32().Should().Be(3);
        root.GetProperty("page").GetInt32().Should().Be(2);
        root.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        root.GetProperty("records").EnumerateArray().Single().GetProperty("name").GetString().Should().Be("bug");
        await Mediator.Received(1).Send(
            Arg.Is<GetTagsPageQuery>(query =>
                query.Filter.Page == 2 && query.Filter.PageSize == 1 && query.Filter.Search == "bu"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldPassThePageAndSearch_ToEveryKindsQuery()
    {
        var tool = new ListRecordsTool(Mediator);

        foreach (var kind in AllKinds)
        {
            var arguments = Arguments($$"""{"kind":"{{kind}}","page":3,"pageSize":7,"search":"ops"}""");

            await tool.Execute(arguments, TestContext.Current.CancellationToken);
        }

        await Mediator.Received(1).Send(
            Arg.Is<GetStatusesPageQuery>(query => IsPage(query.Filter, query.Filter.Search)),
            Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<GetTagsPageQuery>(query => IsPage(query.Filter, query.Filter.Search)),
            Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<GetProjectsPageQuery>(query => IsPage(query.Filter, query.Filter.Search)),
            Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<GetRelationTypesPageQuery>(query => IsPage(query.Filter, query.Filter.Search)),
            Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<GetBoardGroupOptionsPageQuery>(query => IsPage(query.Filter, query.Filter.Search)),
            Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<GetBoardsInWorkspaceQuery>(query => IsPage(query.Filter, query.Filter.Search)),
            Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<GetSprintsPageQuery>(query => IsPage(query.Filter, query.Filter.Search)),
            Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<GetAssigneesQuery>(query => IsPage(query.Filter, query.Filter.Search)),
            Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<GetWorkspaceUsersQuery>(query => IsPage(query.Filter!, query.Filter!.Search)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldListProjectsAndBoardsByName_SoPagesStayStable()
    {
        var tool = new ListRecordsTool(Mediator);

        await tool.Execute(Arguments("""{"kind":"projects"}"""), TestContext.Current.CancellationToken);
        await tool.Execute(Arguments("""{"kind":"boards"}"""), TestContext.Current.CancellationToken);

        await Mediator.Received(1).Send(
            Arg.Is<GetProjectsPageQuery>(query => query.Filter.SortBy == "name"),
            Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(
            Arg.Is<GetBoardsInWorkspaceQuery>(query => query.Filter.SortBy == "name"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldCapThePageSize()
    {
        GivenBoards();

        var tool = new ListRecordsTool(Mediator);

        await tool.Execute(Arguments("""{"kind":"boards","pageSize":500}"""), TestContext.Current.CancellationToken);

        await Mediator.Received(1).Send(
            Arg.Is<GetBoardsInWorkspaceQuery>(query => query.Filter.Page == 1 && query.Filter.PageSize == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldPassTheProjectThrough_WhenListingSprints()
    {
        var page = new PagedResponse<SprintViewModel>([], 1, 50, 0);

        Mediator
            .Send(Arg.Any<GetSprintsPageQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<SprintViewModel>>.Success(page));

        var tool = new ListRecordsTool(Mediator);

        await tool.Execute(Arguments("""{"kind":"sprints","projectId":4}"""), TestContext.Current.CancellationToken);

        await Mediator.Received(1).Send(
            Arg.Is<GetSprintsPageQuery>(query => query.Filter.ProjectId == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldPassTheBoardThrough_WhenListingBoardGroups()
    {
        var page = new PagedResponse<BoardGroupOptionViewModel>([], 1, 50, 0);

        Mediator
            .Send(Arg.Any<GetBoardGroupOptionsPageQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<BoardGroupOptionViewModel>>.Success(page));

        var tool = new ListRecordsTool(Mediator);

        await tool.Execute(Arguments("""{"kind":"board_groups","boardId":7}"""), TestContext.Current.CancellationToken);

        await Mediator.Received(1).Send(
            Arg.Is<GetBoardGroupOptionsPageQuery>(query => query.Filter.BoardId == 7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenThePageCannotBeRead()
    {
        Mediator
            .Send(Arg.Any<GetProjectsPageQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<ProjectViewModel>>.NotFound);

        var tool = new ListRecordsTool(Mediator);
        var result = await tool.Execute(Arguments("""{"kind":"projects"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
    }

    private static bool IsPage(PageRequest filter, string? search)
    {
        return filter.Page == 3 && filter.PageSize == 7 && search == "ops";
    }

    private void GivenBoards()
    {
        var page = new PagedResponse<BoardViewModel>([], 1, 100, 0);

        Mediator
            .Send(Arg.Any<GetBoardsInWorkspaceQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<BoardViewModel>>.Success(page));
    }

    [Fact]
    public async Task Execute_ShouldPageServiceAccountsByName_AndSayWhetherEachCanRunRules()
    {
        Mediator
            .Send(Arg.Any<GetServiceAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                ServiceAccount("ops-2", "Ops bot", disabled: true),
                ServiceAccount("release", "Release bot"),
                ServiceAccount("ops-1", "Ops agent"),
            ]);

        var tool = new ListRecordsTool(Mediator);
        var arguments = Arguments("""{"kind":"automation_service_accounts","search":"ops","pageSize":1}""");
        var result = await tool.Execute(arguments, TestContext.Current.CancellationToken);

        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;
        var record = root.GetProperty("records").EnumerateArray().Single();

        root.GetProperty("totalCount").GetInt32().Should().Be(2);
        root.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        record.GetProperty("userId").GetString().Should().Be("ops-1");
        record.GetProperty("isEnabled").GetBoolean().Should().BeTrue();
    }

    private static ServiceAccountViewModel ServiceAccount(string userId, string name, bool disabled = false)
    {
        return new ServiceAccountViewModel
        {
            UserId = userId,
            Name = name,
            DisabledAt = disabled ? DateTime.UtcNow : null,
            OwnerUserIds = [],
            Permissions = [NetptunePermissions.Tasks.Update],
        };
    }

    private static JsonElement Arguments(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }
}
