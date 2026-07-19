using Microsoft.AspNetCore.Http;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Services.Activity;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Activity;

public class ActivityLoggerTests
{
    private const int WorkspaceId = 42;

    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    [Fact]
    public async Task Log_ShouldUseExplicitWorkspaceId_WithoutResolvingRequestWorkspace()
    {
        Identity.GetCurrentUserId().Returns("user-1");

        var logger = new ActivityLogger(EventPublisher, Identity, new HttpContextAccessor());

        logger.Log(options =>
        {
            options.EntityId = WorkspaceId;
            options.WorkspaceId = WorkspaceId;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.Create;
        });

        await Identity.DidNotReceive().GetWorkspaceId();
        await EventPublisher.Received(1).Dispatch(Arg.Is<ActivityMessage>(message =>
            message.Events.Count == 1
            && message.Events[0].WorkspaceId == WorkspaceId
            && message.Events[0].EntityId == WorkspaceId));
    }

    [Fact]
    public async Task Log_ShouldResolveRequestWorkspace_WhenWorkspaceIdIsNotExplicit()
    {
        Identity.GetCurrentUserId().Returns("user-1");
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        var logger = new ActivityLogger(EventPublisher, Identity, new HttpContextAccessor());

        logger.Log(options =>
        {
            options.EntityId = 7;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Create;
        });

        await Identity.Received(1).GetWorkspaceId();
        await EventPublisher.Received(1).Dispatch(Arg.Is<ActivityMessage>(message =>
            message.Events.Single().WorkspaceId == WorkspaceId));
    }

    [Fact]
    public async Task Log_ShouldNotPublishACompetingActivityMessage_WhenCanonicalEventWasCaptured()
    {
        Identity.GetCurrentUserId().Returns("user-1");
        var capture = new CanonicalEventCapture();
        capture.Record(WorkspaceId, EventEntityTypes.From(EntityType.Task), "7");
        var logger = new ActivityLogger(
            EventPublisher,
            Identity,
            new HttpContextAccessor(),
            capture);

        logger.Log(options =>
        {
            options.EntityId = 7;
            options.WorkspaceId = WorkspaceId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.ModifyStatus;
        });

        await EventPublisher.DidNotReceive().Dispatch(Arg.Any<ActivityMessage>());
    }
}
