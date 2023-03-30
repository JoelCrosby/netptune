using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.JobServer.Handlers;

public sealed class ActivityHandler : IRequestHandler<ActivityMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IAncestorService AncestorService;

    public ActivityHandler(INetptuneUnitOfWork unitOfWork, IAncestorService ancestorService)
    {
        UnitOfWork = unitOfWork;
        AncestorService = ancestorService;
    }

    public async Task Handle(ActivityMessage request, CancellationToken cancellationToken)
    {
        foreach (var activity in request.Events)
        {
            if (activity.EntityId is null)
            {
                throw new Exception("IActivityEvent EntityId cannot be null.");
            }

            var ancestors = await AncestorService.GetTaskAncestors(activity.EntityId.Value);

            await UnitOfWork.ActivityLogs.AddAsync(new ActivityLog
            {
                OwnerId = activity.UserId,
                Type = activity.Type,
                EntityType = activity.EntityType,
                EntityId = activity.EntityId,
                UserId = activity.UserId,
                CreatedByUserId = activity.UserId,
                WorkspaceId = activity.WorkspaceId,
                TaskId = activity.EntityId,
                ProjectId = ancestors.ProjectId,
                BoardId = ancestors.ProjectId,
                BoardGroupId = ancestors.BoardGroupId,
                Time = activity.Time,
                Meta = activity.Meta is {} ? JsonDocument.Parse(activity.Meta) : null,
            });
        }

        await UnitOfWork.CompleteAsync();
    }
}
