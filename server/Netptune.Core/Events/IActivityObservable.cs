using System;

namespace Netptune.Core.Events
{
    public interface IActivityObservable : IObservable<ActivityEvent>
    {
        void Track(ActivityEvent activityEvent);
    }
}
