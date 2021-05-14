using System;

using Netptune.Core.Models.Activity;

namespace Netptune.Core.Events
{
    public interface IActivityLogger
    {
        void Log(Action<ActivityOptions> options);

        void Log(Action<ActivityMultipleOptions> options);
    }
}
