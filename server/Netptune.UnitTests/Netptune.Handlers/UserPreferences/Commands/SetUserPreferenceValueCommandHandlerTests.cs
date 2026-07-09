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

public class SetUserPreferenceValueCommandHandlerTests
{
    private const string UserId = "user-id";
    private const int WorkspaceId = 42;

    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly PreferenceDefinitionRegistry Registry = new();
    private readonly SetUserPreferenceValueCommandHandler Handler;

    public SetUserPreferenceValueCommandHandlerTests()
    {
        Handler = new(Identity, UnitOfWork, Registry);
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.TryGetWorkspaceKey().Returns("workspace");
        Identity.GetWorkspaceId().Returns(WorkspaceId);
    }

    [Fact]
    public async Task SetUserPreferenceValue_AddsWorkspaceValue_WhenValueIsValid()
    {
        UnitOfWork.UserPreferences
            .GetScopedValue(
                UserId,
                PreferenceKeys.CommandPaletteRecentItemsScope,
                WorkspaceId,
                TestContext.Current.CancellationToken)
            .Returns((UserPreferenceValue?)null);
        UnitOfWork.UserPreferences
            .AddAsync(Arg.Any<UserPreferenceValue>(), TestContext.Current.CancellationToken)
            .Returns(call => call.Arg<UserPreferenceValue>());
        UnitOfWork.UserPreferences
            .GetValues(
                UserId,
                PreferenceKeys.CommandPaletteRecentItemsScope,
                WorkspaceId,
                TestContext.Current.CancellationToken)
            .Returns([
                CreatePreference(PreferenceKeys.CommandPaletteRecentItemsScope, WorkspaceId, "global"),
            ]);

        var result = await Handler.Handle(
            new SetUserPreferenceValueCommand
            {
                Key = PreferenceKeys.CommandPaletteRecentItemsScope,
                Scope = PreferenceScopes.Workspace,
                Value = JsonSerializer.SerializeToElement("global"),
            },
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.EffectiveValue.GetString().Should().Be("global");

        await UnitOfWork.UserPreferences.Received(1).AddAsync(
            Arg.Is<UserPreferenceValue>(preference =>
                preference.UserId == UserId &&
                preference.WorkspaceId == WorkspaceId &&
                preference.Key == PreferenceKeys.CommandPaletteRecentItemsScope &&
                preference.Value.RootElement.GetString() == "global"),
            TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SetUserPreferenceValue_ReturnsFailure_WhenValueIsNotAllowed()
    {
        var result = await Handler.Handle(
            new SetUserPreferenceValueCommand
            {
                Key = PreferenceKeys.CommandPaletteRecentItemsScope,
                Scope = PreferenceScopes.Workspace,
                Value = JsonSerializer.SerializeToElement("invalid"),
            },
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.UserPreferences.DidNotReceive().AddAsync(
            Arg.Any<UserPreferenceValue>(),
            TestContext.Current.CancellationToken);
        await UnitOfWork.DidNotReceive().CompleteAsync(TestContext.Current.CancellationToken);
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
