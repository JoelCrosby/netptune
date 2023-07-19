using System;

using Netptune.Core.Models.Activity;

namespace Netptune.Core.Services.Activity;

public interface IActivityLogger
{
    void Log(Action<ActivityOptions> options);

    void LogMany(Action<ActivityMultipleOptions> options);

    void LogWith<TMeta>(Action<ActivityOptions<TMeta>> options) where TMeta : class;

    void LogWithMany<TMeta>(Action<ActivityMultipleOptions<TMeta>> options) where TMeta : class;
}
