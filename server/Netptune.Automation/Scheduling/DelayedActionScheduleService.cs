using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Automation.Configuration;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Execution;

namespace Netptune.Automation.Scheduling;

internal sealed class DelayedActionScheduleService : BackgroundService
{
    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<DelayedActionScheduleService> Logger;
    private readonly ScheduleOptions Options;

    public DelayedActionScheduleService(
        IServiceScopeFactory scopeFactory,
        ILogger<DelayedActionScheduleService> logger,
        IOptions<ScheduleOptions> options)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
        Options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Delayed automation action scheduler starting with run interval {RunInterval}", Options.DelayedActionRunInterval);

        await Task.Delay(Options.StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = Telemetry.StartActivity("automation.schedule.delayed_actions");
            var startedAt = Stopwatch.GetTimestamp();

            try
            {
                using var scope = ScopeFactory.CreateScope();
                var automationExecution = scope.ServiceProvider.GetRequiredService<IExecutionService>();

                await automationExecution.ExecuteScheduledActions(stoppingToken);

                Logger.LogInformation(
                    "Delayed automation action schedule completed in {ElapsedMilliseconds}ms",
                    Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Telemetry.MarkFailed(activity, ex);
                Logger.LogError(ex, "Delayed automation action schedule failed");
            }

            await Task.Delay(Options.DelayedActionRunInterval, stoppingToken);
        }
    }
}
