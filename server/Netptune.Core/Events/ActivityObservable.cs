using System;
using System.Collections.Generic;

namespace Netptune.Core.Events
{
    public class ActivityObservable : IActivityObservable
    {
        private List<IActivity> ActivityTrail;
        private List<IObserver<IActivity>> Observers;

        public ActivityObservable()
        {
            ActivityTrail = new List<IActivity>();
            Observers = new List<IObserver<IActivity>>();
        }

        public IDisposable Subscribe(IObserver<IActivity> observer)
        {
            // Check whether observer is already registered. If not, add it
            if (Observers.Contains(observer))
            {
                return new Unsubscriber<IActivity>(Observers, observer);
            }

            Observers.Add(observer);

            // Provide observer with existing data.
            foreach (var activity in ActivityTrail)
            {
                observer.OnNext(activity);
            }

            return new Unsubscriber<IActivity>(Observers, observer);
        }
    }
}
