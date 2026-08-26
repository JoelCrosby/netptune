using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Models.Workspaces;
using Netptune.Core.Preferences;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;
using Netptune.Handlers.Workspaces.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Workspaces.Queries;

public class GetUserWorkspacesQueryHandlerTests
{
    private readonly GetUserWorkspacesQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetUserWorkspacesQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);

        UnitOfWork.Workspaces
            .GetMemberSummaries(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task GetUserWorkspaces_ShouldReturnCorrectly_WhenInputValid()
    {
        var workspaces = new List<Workspace> { AutoFixtures.Workspace };
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Workspaces.GetUserWorkspaces("userId", TestContext.Current.CancellationToken).Returns(workspaces);

        var result = await Handler.Handle(new GetUserWorkspacesQuery(), TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(workspaces, options => options.ExcludingMissingMembers());
        result.Should().OnlyContain(workspace => !workspace.IsLastVisited);
    }

    [Fact]
    public async Task GetUserWorkspaces_ShouldFlagLastVisited_WhenPreferenceMatchesSlug()
    {
        var workspace = AutoFixtures.Workspace;
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Workspaces.GetUserWorkspaces("userId", TestContext.Current.CancellationToken)
            .Returns([workspace]);
        UnitOfWork.UserPreferences
            .GetScopedValue("userId", PreferenceKeys.WorkspaceLastVisited, null, TestContext.Current.CancellationToken)
            .Returns(new UserPreferenceValue
            {
                UserId = "userId",
                Key = PreferenceKeys.WorkspaceLastVisited,
                Value = JsonSerializer.SerializeToDocument(workspace.Slug),
            });

        var result = await Handler.Handle(new GetUserWorkspacesQuery(), TestContext.Current.CancellationToken);

        result.Single().IsLastVisited.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserWorkspaces_ShouldIncludeMemberSummary_WhenMembersExist()
    {
        var workspace = AutoFixtures.Workspace;
        var member = new AssigneeViewModel { Id = "memberId", DisplayName = "Sarah Whitfield" };

        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Workspaces.GetUserWorkspaces("userId", TestContext.Current.CancellationToken)
            .Returns([workspace]);
        UnitOfWork.Workspaces
            .GetMemberSummaries(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, WorkspaceMemberSummary>
            {
                [workspace.Id] = new() { MemberCount = 6, Members = [member] },
            });

        var result = await Handler.Handle(new GetUserWorkspacesQuery(), TestContext.Current.CancellationToken);

        result.Single().MemberCount.Should().Be(6);
        result.Single().Members.Should().ContainSingle().Which.DisplayName.Should().Be("Sarah Whitfield");
    }

    [Fact]
    public async Task GetUserWorkspaces_ShouldReturnEmptyMembers_WhenSummaryMissing()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Workspaces.GetUserWorkspaces("userId", TestContext.Current.CancellationToken)
            .Returns([AutoFixtures.Workspace]);

        var result = await Handler.Handle(new GetUserWorkspacesQuery(), TestContext.Current.CancellationToken);

        result.Single().Members.Should().BeEmpty();
        result.Single().MemberCount.Should().Be(0);
    }
}
