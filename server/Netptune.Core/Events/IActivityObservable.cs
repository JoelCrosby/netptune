using System;
using System.Collections.Generic;

namespace Netptune.Core.Events;

public interface IActivityObservable : IObservable<IEnumerable<ActivityEvent>>
{
    void Track(IEnumerable<ActivityEvent> activityEvents);
}