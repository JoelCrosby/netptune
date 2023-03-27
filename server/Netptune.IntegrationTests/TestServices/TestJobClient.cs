using System.Linq.Expressions;

using Netptune.Core.Jobs;

namespace Netptune.IntegrationTests.TestServices;

public class TestJobClient : IJobClient
{
    public string Enqueue(Expression<Action> methodCall)
    {
        return String.Empty;
    }

    public string Enqueue<TService>(Expression<Action<TService>> methodCall)
    {
        return String.Empty;
    }
}
