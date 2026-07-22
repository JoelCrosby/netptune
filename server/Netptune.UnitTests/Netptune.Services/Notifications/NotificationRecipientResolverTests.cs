using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Preferences;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Notifications;

public sealed class NotificationRecipientResolverTests
{
    private const int WorkspaceId = 10;
    private const string ActorUserId = "actor";
    private const string RecipientUserId = "recipient";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    [Fact]
    public async Task Resolve_ReturnsExplicitRecipient_WhenNoPreferenceIsStored()
    {
        var recipients = await Resolve([]);

        recipients.Should().Equal(RecipientUserId);
    }

    [Fact]
    public async Task Resolve_ExcludesRecipient_WhenEventIsDisabledGlobally()
    {
        var preference = Preference(false, null);

        var recipients = await Resolve([preference]);

        recipients.Should().BeEmpty();
    }

    [Fact]
    public async Task Resolve_UsesWorkspacePreference_AheadOfGlobalPreference()
    {
        var recipients = await Resolve([
            Preference(false, null),
            Preference(true, WorkspaceId),
        ]);

        recipients.Should().Equal(RecipientUserId);
    }

    [Fact]
    public async Task Resolve_ReturnsNoRecipients_WhenRecipientListIsMissing()
    {
        var recipients = await NotificationRecipientResolver.Resolve(
            UnitOfWork,
            new NotificationRecipientRequest
            {
                RequestedUserIds = null,
                WorkspaceUserIds = [ActorUserId, RecipientUserId],
                ActorUserId = ActorUserId,
                WorkspaceId = WorkspaceId,
                ActivityType = ActivityType.Mention,
            },
            TestContext.Current.CancellationToken);

        recipients.Should().BeEmpty();
        await UnitOfWork.UserPreferences.DidNotReceiveWithAnyArgs().GetValues(
            default!,
            default!,
            default,
            TestContext.Current.CancellationToken);
    }

    private async Task<List<string>> Resolve(List<UserPreferenceValue> preferences)
    {
        UnitOfWork.UserPreferences
            .GetValues(
                Arg.Any<IEnumerable<string>>(),
                PreferenceKeys.NotificationEvent(ActivityType.Mention),
                WorkspaceId,
                TestContext.Current.CancellationToken)
            .Returns(preferences);

        return await NotificationRecipientResolver.Resolve(
            UnitOfWork,
            new NotificationRecipientRequest
            {
                RequestedUserIds = [RecipientUserId],
                WorkspaceUserIds = [ActorUserId, RecipientUserId],
                ActorUserId = ActorUserId,
                WorkspaceId = WorkspaceId,
                ActivityType = ActivityType.Mention,
            },
            TestContext.Current.CancellationToken);
    }

    private static UserPreferenceValue Preference(bool enabled, int? workspaceId)
    {
        return new UserPreferenceValue
        {
            UserId = RecipientUserId,
            WorkspaceId = workspaceId,
            Key = PreferenceKeys.NotificationEvent(ActivityType.Mention),
            Value = JsonSerializer.SerializeToDocument(enabled),
        };
    }
}
