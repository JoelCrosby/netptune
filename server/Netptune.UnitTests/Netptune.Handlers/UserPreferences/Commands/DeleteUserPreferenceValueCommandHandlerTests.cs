using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.UserPreferences.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.UserPreferences.Commands;

public class DeleteUserPreferenceValueCommandHandlerTests
{
    private const string UserId = "user-id";
    private const int WorkspaceId = 42;

    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly PreferenceDefinitionRegistry Registry = new();
    private readonly DeleteUserPreferenceValueCommandHandler Handler;

    public DeleteUserPreferenceValueCommandHandlerTests()
    {
        Handler = new(Identity, UnitOfWork, Registry);
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.TryGetWorkspaceKey().Returns("workspace");
        Identity.GetWorkspaceId().Returns(WorkspaceId);
    }

    [Fact]
    public async Task DeleteUserPreferenceValue_DeletesScopedValue_AndReturnsResolvedDefault()
    {
        var preference = CreatePreference(PreferenceKeys.CommandPaletteRecentItemsScope, WorkspaceId, "global") with
        {
            Id = 10,
        };
        UnitOfWork.UserPreferences
            .GetScopedValue(
                UserId,
                PreferenceKeys.CommandPaletteRecentItemsScope,
                WorkspaceId,
                TestContext.Current.CancellationToken)
            .Returns(preference);
        UnitOfWork.UserPreferences
            .GetValues(
                UserId,
                PreferenceKeys.CommandPaletteRecentItemsScope,
                WorkspaceId,
                TestContext.Current.CancellationToken)
            .Returns([]);

        var result = await Handler.Handle(
            new DeleteUserPreferenceValueCommand(
                PreferenceKeys.CommandPaletteRecentItemsScope,
                PreferenceScopes.Workspace),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Source.Should().Be("default");
        result.Payload!.EffectiveValue.GetString().Should().Be("workspace");

        await UnitOfWork.UserPreferences.Received(1).DeletePermanent(10, TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteUserPreferenceValue_ReturnsFailure_WhenWorkspaceScopeHasNoWorkspace()
    {
        Identity.TryGetWorkspaceKey().Returns((string?)null);

        var result = await Handler.Handle(
            new DeleteUserPreferenceValueCommand(
                PreferenceKeys.CommandPaletteRecentItemsScope,
                PreferenceScopes.Workspace),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.UserPreferences.DidNotReceive().DeletePermanent(
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
