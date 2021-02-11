using System;
using System.Linq.Expressions;

using Netptune.Core.Jobs;

namespace Netptune.JobServer.Util
{
    public class EmptyJobClient : IJobClient
    {
        public string Enqueue(Expression<Action> methodCall)
        {
            throw new Exception("calling a IJobClient method called from background job is not supported");
        }

        public string Enqueue<TService>(Expression<Action<TService>> methodCall)
        {
            throw new Exception("calling a IJobClient method called from background job is not supported");
        }
    }
}
