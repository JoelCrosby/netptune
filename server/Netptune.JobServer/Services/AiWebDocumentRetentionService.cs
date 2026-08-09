using Netptune.Core.UnitOfWork;

namespace Netptune.JobServer.Services;

public sealed class AiWebDocumentRetentionService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<AiWebDocumentRetentionService> Logger;

    public AiWebDocumentRetentionService(IServiceScopeFactory scopeFactory, ILogger<AiWebDocumentRetentionService> logger)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        using var timer = new PeriodicTimer(SweepInterval);

        do
        {
            try
            {
                await Sweep(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "[Assistant] web document sweep failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task Sweep(CancellationToken cancellationToken)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var deleted = await unitOfWork.AiWebDocuments.DeleteExpired(DateTime.UtcNow, cancellationToken);

        if (deleted > 0)
        {
            Logger.LogInformation("[Assistant] removed {Count} expired web documents", deleted);
        }
    }
}
