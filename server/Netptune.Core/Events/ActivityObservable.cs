using System;
using System.Collections.Generic;
using System.Linq;

namespace Netptune.Core.Events
{
    public class ActivityObservable : IActivityObservable
    {
        private readonly List<IObserver<IEnumerable<ActivityEvent>>> Observers;

        public ActivityObservable()
        {
            Observers = new List<IObserver<IEnumerable<ActivityEvent>>>();
        }

        public void Track(IEnumerable<ActivityEvent> events)
        {
            NotifySubscribers(events);
        }

        public IDisposable Subscribe(IObserver<IEnumerable<ActivityEvent>> observer)
        {
            // Check whether observer is already registered. If not, add it
            if (Observers.Contains(observer))
            {
                return new Unsubscriber<IEnumerable<ActivityEvent>>(Observers, observer);
            }

            Observers.Add(observer);

            return new Unsubscriber<IEnumerable<ActivityEvent>>(Observers, observer);
        }

        private void NotifySubscribers(IEnumerable<ActivityEvent> events)
        {
            // Provide observers with existing data.
            var enumerated = events.ToList();

            foreach (var observer in Observers)
            {
                observer.OnNext(enumerated);
            }
        }
    }
}
