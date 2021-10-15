using System;
using System.Collections.Generic;

namespace Netptune.Core.Events
{
    public interface IActivityWriterService : IObserver<IEnumerable<IActivityEvent>>
    {
    }
}
