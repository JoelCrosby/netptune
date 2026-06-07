using Microsoft.Extensions.DependencyInjection;

using Netptune.Automation.Execution;
using Netptune.Entities.Contexts;

namespace Netptune.Automation.Tests;

public sealed class AutomationTestScope : IAsyncDisposable
{
    private readonly AsyncServiceScope Scope;

    public AutomationTestScope(AsyncServiceScope scope)
    {
        Scope = scope;
    }

    public DataContext Db => Scope.ServiceProvider.GetRequiredService<DataContext>();

    public IExecutionService AutomationExecution => Scope.ServiceProvider.GetRequiredService<IExecutionService>();

    public async ValueTask DisposeAsync()
    {
        await Scope.DisposeAsync();
    }
}
