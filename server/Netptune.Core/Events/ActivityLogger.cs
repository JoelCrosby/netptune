using System;

using Netptune.Core.Models.Activity;

namespace Netptune.Core.Events
{
    public class ActivityLogger : IActivityLogger
    {
        private readonly IActivityObservable Observable;

        public ActivityLogger(IActivityObservable observable)
        {
            Observable = observable;
        }

        public void Log(Action<ActivityOptions> options)
        {
            var activityOptions = new ActivityOptions();

            options.Invoke(activityOptions);

            if (activityOptions.EntityId is null)
            {
                throw new Exception($"Cannot call log with null {nameof(activityOptions.EntityId)}.");
            }

            if (activityOptions.WorkspaceId is null)
            {
                throw new Exception($"Cannot call log with null {nameof(activityOptions.WorkspaceId)}.");
            }

            var activity = new ActivityEvent
            {
                Type = activityOptions.Type,
                EntityType = activityOptions.EntityType,
                UserId = activityOptions.UserId,
                EntityId = activityOptions.EntityId.Value,
                WorkspaceId = activityOptions.WorkspaceId.Value,
            };

            Observable.Track(activity);
        }
    }
}
