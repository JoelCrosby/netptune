using System;
using System.Linq.Expressions;

using Hangfire;

using Netptune.Core.Jobs;

namespace Netptune.JobClient;

public class JobClientService : IJobClient
{
    public string Enqueue(Expression<Action> methodCall)
    {
        return BackgroundJob.Enqueue(methodCall);
    }

    public string Enqueue<TService>(Expression<Action<TService>> methodCall)
    {
        return BackgroundJob.Enqueue(methodCall);
    }
}