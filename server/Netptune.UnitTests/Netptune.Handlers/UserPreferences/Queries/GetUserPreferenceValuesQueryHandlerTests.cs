using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.UserPreferences.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.UserPreferences.Queries;

public class GetUserPreferenceValuesQueryHandlerTests
{
    private const string UserId = "user-id";
    private const int WorkspaceId = 42;

    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly PreferenceDefinitionRegistry Registry = new();
    private readonly GetUserPreferenceValuesQueryHandler Handler;

    public GetUserPreferenceValuesQueryHandlerTests()
    {
        Handler = new(Identity, UnitOfWork, Registry);
    }

    [Fact]
    public async Task GetUserPreferenceValues_ReturnsResolvedValues()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.TryGetWorkspaceKey().Returns("workspace");
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        UnitOfWork.UserPreferences
            .GetValues(UserId, PreferenceKeys.AppearanceTheme, WorkspaceId, TestContext.Current.CancellationToken)
            .Returns([
                CreatePreference(PreferenceKeys.AppearanceTheme, null, "dark"),
            ]);
        UnitOfWork.UserPreferences
            .GetValues(UserId, PreferenceKeys.CommandPaletteRecentItemsScope, WorkspaceId, TestContext.Current.CancellationToken)
            .Returns([
                CreatePreference(PreferenceKeys.CommandPaletteRecentItemsScope, null, "global"),
                CreatePreference(PreferenceKeys.CommandPaletteRecentItemsScope, WorkspaceId, "workspace"),
            ]);
        UnitOfWork.UserPreferences
            .GetValues(UserId, PreferenceKeys.BoardHiddenGroupIds, WorkspaceId, TestContext.Current.CancellationToken)
            .Returns([]);

        var result = await Handler.Handle(new GetUserPreferenceValuesQuery(), TestContext.Current.CancellationToken);

        var appearance = result.Groups
            .Single(group => group.Key == "appearance")
            .Preferences
            .Single();
        appearance.Source.Should().Be(PreferenceScopes.Global);
        appearance.EffectiveValue.GetString().Should().Be("dark");

        var recentItemsScope = result.Groups
            .Single(group => group.Key == "commandPalette")
            .Preferences
            .Single();
        recentItemsScope.Source.Should().Be(PreferenceScopes.Workspace);
        recentItemsScope.GlobalValue!.Value.GetString().Should().Be("global");
        recentItemsScope.WorkspaceValue!.Value.GetString().Should().Be("workspace");
        recentItemsScope.EffectiveValue.GetString().Should().Be("workspace");
    }

    private static UserPreferenceValue CreatePreference(string key, int? workspaceId, string value)
    {
        return new()
        {
            UserId = UserId,
            WorkspaceId = workspaceId,
            Key = key,
            Value = JsonSerializer.SerializeToDocument(value),
        };
    }
}
