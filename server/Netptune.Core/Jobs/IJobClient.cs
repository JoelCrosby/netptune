using System;
using System.Linq.Expressions;

namespace Netptune.Core.Jobs
{
    public interface IJobClient
    {
        string Enqueue(Expression<Action> methodCall);

        string Enqueue<TService>(Expression<Action<TService>> methodCall);
    }
}
