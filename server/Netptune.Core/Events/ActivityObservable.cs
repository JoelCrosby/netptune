using System;
using System.Collections.Generic;

namespace Netptune.Core.Events
{
    public class ActivityObservable : IActivityObservable
    {
        private readonly List<IObserver<ActivityEvent>> Observers;

        public ActivityObservable()
        {
            Observers = new List<IObserver<ActivityEvent>>();
        }

        public void Track(ActivityEvent activityEvent)
        {
            NotifySubscribers(activityEvent);
        }

        public IDisposable Subscribe(IObserver<ActivityEvent> observer)
        {
            // Check whether observer is already registered. If not, add it
            if (Observers.Contains(observer))
            {
                return new Unsubscriber<ActivityEvent>(Observers, observer);
            }

            Observers.Add(observer);

            return new Unsubscriber<ActivityEvent>(Observers, observer);
        }

        private void NotifySubscribers(ActivityEvent activityEvent)
        {
            // Provide observers with existing data.

            foreach (var observer in Observers)
            {
                observer.OnNext(activityEvent);
            }
        }
    }
}
