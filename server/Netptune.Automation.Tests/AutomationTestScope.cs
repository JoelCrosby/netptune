using Microsoft.Extensions.DependencyInjection;

using Netptune.Automation.Execution;
using Netptune.Core.UnitOfWork;
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

    public INetptuneUnitOfWork UnitOfWork => Scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();

    internal RecordingEventRecordWriter EventRecords => Scope.ServiceProvider.GetRequiredService<RecordingEventRecordWriter>();

    internal RecordingEventPublisher EventPublisher => Scope.ServiceProvider.GetRequiredService<RecordingEventPublisher>();

    public async ValueTask DisposeAsync()
    {
        await Scope.DisposeAsync();
    }
}
