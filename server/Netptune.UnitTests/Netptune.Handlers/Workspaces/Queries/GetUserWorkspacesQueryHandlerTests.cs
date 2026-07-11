using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
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
}
