using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Automation.Configuration;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Execution;

namespace Netptune.Automation.Scheduling;

internal sealed class ScheduleService : BackgroundService
{
    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<ScheduleService> Logger;
    private readonly ScheduleOptions Options;

    public ScheduleService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduleService> logger,
        IOptions<ScheduleOptions> options)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
        Options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation(
            "Automation schedule service starting with startup delay {StartupDelay} and run interval {RunInterval}",
            Options.StartupDelay,
            Options.RunInterval);

        await Task.Delay(Options.StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = Telemetry.StartActivity("automation.schedule.rules");
            var startedAt = Stopwatch.GetTimestamp();

            try
            {
                Logger.LogInformation("Automation schedule cycle started");

                using var scope = ScopeFactory.CreateScope();
                var automationExecution = scope.ServiceProvider.GetRequiredService<IExecutionService>();

                await ExecuteScheduledRules(
                    "unassigned-task",
                    automationExecution.ExecuteUnassignedRules,
                    activity,
                    stoppingToken);
                await ExecuteScheduledRules(
                    "due-date",
                    automationExecution.ExecuteDueDateRules,
                    activity,
                    stoppingToken);

                Logger.LogInformation(
                    "Automation schedule cycle completed in {ElapsedMilliseconds}ms",
                    Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Telemetry.MarkFailed(activity, ex);
                Logger.LogError(ex, "AutomationScheduleService failed");
            }

            await Task.Delay(Options.RunInterval, stoppingToken);
        }
    }

    private async Task ExecuteScheduledRules(
        string ruleSet,
        Func<CancellationToken, Task> execute,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        try
        {
            await execute(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Telemetry.MarkFailed(activity, ex);
            Logger.LogError(ex, "Scheduled {RuleSet} automation evaluation failed", ruleSet);
        }
    }
}
