using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.CommandPalette.Commands;
using Netptune.Handlers.CommandPalette.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.CommandPalette.Commands;

public class ClearCommandPaletteRecentItemsCommandHandlerTests
{
    private const string UserId = "user-id";
    private const int WorkspaceId = 42;

    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly PreferenceDefinitionRegistry Registry = new();
    private readonly ClearCommandPaletteRecentItemsCommandHandler Handler;

    public ClearCommandPaletteRecentItemsCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Registry, Mediator);
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.TryGetWorkspaceKey().Returns("workspace");
        Identity.GetWorkspaceId().Returns(WorkspaceId);
    }

    [Fact]
    public async Task ClearCommandPaletteRecentItems_UsesResolvedPreferenceScope()
    {
        var entities = new List<CommandPaletteRecentItem>
        {
            new()
            {
                UserId = UserId,
                WorkspaceId = WorkspaceId,
                Type = "task",
                Title = "Task",
                Url = "/tasks/1",
            },
        };
        var response = ClientResponse<CommandPaletteRecentItemsResponse>.Success(
            new CommandPaletteRecentItemsResponse
            {
                Scope = PreferenceScopes.Global,
            });
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
            .GetClearableItems(UserId, WorkspaceId, PreferenceScopes.Global, TestContext.Current.CancellationToken)
            .Returns(entities);
        Mediator.Send(Arg.Any<GetCommandPaletteRecentItemsQuery>(), TestContext.Current.CancellationToken)
            .Returns(response);

        var result = await Handler.Handle(new ClearCommandPaletteRecentItemsCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Scope.Should().Be(PreferenceScopes.Global);

        await UnitOfWork.CommandPaletteRecentItems.Received(1)
            .GetClearableItems(UserId, WorkspaceId, PreferenceScopes.Global, TestContext.Current.CancellationToken);
        await UnitOfWork.CommandPaletteRecentItems.Received(1)
            .DeletePermanent(entities, TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
        await Mediator.Received(1).Send(
            Arg.Any<GetCommandPaletteRecentItemsQuery>(),
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
