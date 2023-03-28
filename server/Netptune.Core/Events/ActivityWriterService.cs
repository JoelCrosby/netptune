using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;

namespace Netptune.Core.Events;

public class ActivityWriterService : IActivityWriterService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IAncestorService AncestorService;

    public ActivityWriterService(INetptuneUnitOfWork unitOfWork, IAncestorService ancestorService)
    {
        UnitOfWork = unitOfWork;
        AncestorService = ancestorService;
    }

    public async Task WriteActivity(IEnumerable<IActivityEvent> events)
    {
        foreach (var activityEvent in events)
        {
            if (activityEvent.EntityId is null)
            {
                throw new Exception("IActivityEvent EntityId cannot be null.");
            }

            var ancestors = await AncestorService.GetTaskAncestors(activityEvent.EntityId.Value);

            await UnitOfWork.ActivityLogs.AddAsync(new ActivityLog
            {
                OwnerId = activityEvent.UserId,
                Type = activityEvent.Type,
                EntityType = activityEvent.EntityType,
                EntityId = activityEvent.EntityId,
                UserId = activityEvent.UserId,
                CreatedByUserId = activityEvent.UserId,
                WorkspaceId = activityEvent.WorkspaceId,
                TaskId = activityEvent.EntityId,
                ProjectId = ancestors.ProjectId,
                BoardId = ancestors.ProjectId,
                BoardGroupId = ancestors.BoardGroupId,
                Time = activityEvent.Time,
                Meta = activityEvent.Meta is {} ? JsonDocument.Parse(activityEvent.Meta) : null,
            });
        }

        await UnitOfWork.CompleteAsync();
    }
}
