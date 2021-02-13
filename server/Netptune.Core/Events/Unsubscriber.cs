using System;
using System.Collections.Generic;

namespace Netptune.Core.Events
{
    public sealed class Unsubscriber<TObserver> : IDisposable
    {
        private readonly List<IObserver<TObserver>> Observers;
        private readonly IObserver<TObserver> Observer;

        internal Unsubscriber(List<IObserver<TObserver>> observers, IObserver<TObserver> observer)
        {
            Observers = observers;
            Observer = observer;
        }

        public void Dispose()
        {
            if (Observers.Contains(Observer))
                Observers.Remove(Observer);
        }
    }
}
