using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.CommandPalette.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.CommandPalette.Queries;

public class GetCommandPaletteRecentItemsQueryHandlerTests
{
    private const string UserId = "user-id";
    private const int WorkspaceId = 42;

    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly PreferenceDefinitionRegistry Registry = new();
    private readonly GetCommandPaletteRecentItemsQueryHandler Handler;

    public GetCommandPaletteRecentItemsQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Registry);
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.TryGetWorkspaceKey().Returns("workspace");
        Identity.GetWorkspaceId().Returns(WorkspaceId);
    }

    [Fact]
    public async Task GetCommandPaletteRecentItems_UsesResolvedPreferenceScope()
    {
        var items = new List<CommandPaletteRecentItemResponse>
        {
            new()
            {
                Type = "task",
                Title = "Task",
                Url = "/tasks/1",
                LastAccessedAt = DateTime.UtcNow,
            },
        };
        UnitOfWork.UserPreferences
            .GetValues(
                UserId,
                PreferenceKeys.CommandPaletteRecentItemsScope,
                WorkspaceId,
                TestContext.Current.CancellationToken)
            .Returns([
                CreatePreference(PreferenceKeys.CommandPaletteRecentItemsScope, null, "global"),
            ]);
        UnitOfWork.CommandPaletteRecentItems
            .GetRecentItems(UserId, WorkspaceId, PreferenceScopes.Global, 10, TestContext.Current.CancellationToken)
            .Returns(items);

        var result = await Handler.Handle(new GetCommandPaletteRecentItemsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Scope.Should().Be(PreferenceScopes.Global);
        result.Payload!.Items.Should().BeEquivalentTo(items);
        await UnitOfWork.CommandPaletteRecentItems.Received(1)
            .GetRecentItems(UserId, WorkspaceId, PreferenceScopes.Global, 10, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetCommandPaletteRecentItems_ReturnsFailure_WhenWorkspaceIsMissing()
    {
        Identity.TryGetWorkspaceKey().Returns((string?)null);

        var result = await Handler.Handle(new GetCommandPaletteRecentItemsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.CommandPaletteRecentItems.DidNotReceive()
            .GetRecentItems(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                TestContext.Current.CancellationToken);
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
