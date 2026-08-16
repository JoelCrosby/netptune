using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Http;
using Netptune.Core.Models.Hosting;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Ai;
using Netptune.Core.Services.Integration;
using Netptune.Core.Services.Notifications;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.Services.Relations;
using Netptune.Core.Services.Reporting;
using Netptune.Services.Activity;
using Netptune.Services.Ai;
using Netptune.Services.Integration;
using Netptune.Services.Notifications;
using Netptune.Services.ProjectTasks;
using Netptune.Services.Relations;
using Netptune.Services.Reporting;


namespace Netptune.Services.Configuration;

public static class NetptuneServicesConfiguration
{
    public static IServiceCollection AddNetptuneServices(this IServiceCollection services, Action<HostingOptions> action)
    {
        ConfigureServices(services, action);

        services.AddHttpClient();
        services.AddMemoryCache();
        services.AddNetptuneEventRecording();
        services.AddNetptuneNotifications();

        services.AddTransient<IHostingService, HostingService>();
        services.AddTransient<IPublicWorkspaceService, PublicWorkspaceService>();
        services.AddTransient<IWebService, WebService>();
        services.AddTransient<IReportingScopeResolver, ReportingScopeResolver>();

        services.AddSafeHttpClient<IHtmlDocumentService, HtmlDocumentService>(options =>
        {
            options.UserAgent = "Netptune/1.0 (+link-preview)";
        });

        services.AddTransient<IActivityLogger, ActivityLogger>();
        services.AddScoped<ITaskMutationPipeline, TaskMutationPipeline>();
        services.AddScoped<ITaskRelationLinker, TaskRelationLinker>();
        services.AddScoped<ITaskReferenceResolver, TaskReferenceResolver>();
        services.AddScoped<ITaskStatusResolver, TaskStatusResolver>();
        services.AddTransient<ITurnstileService, TurnstileService>();

        services.AddScoped<IAncestorService, AncestorService>();
        services.AddSingleton<IAiCredentialProtector, AiCredentialProtector>();
        services.AddScoped<IAiExecutionContext, AiExecutionContext>();

        return services;
    }

    public static IServiceCollection AddNetptuneEventRecording(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICanonicalEventCapture, CanonicalEventCapture>();
        services.AddTransient<IEventRecordWriter, EventRecordWriter>();

        return services;
    }

    public static IServiceCollection AddNetptuneNotifications(this IServiceCollection services)
    {
        services.AddTransient<INotificationDispatcher, NotificationDispatcher>();

        return services;
    }

    public static IServiceCollection AddNetptuneBackgroundIdentity(this IServiceCollection services)
    {
        services.AddScoped<IActorContext, ActorContext>();
        services.AddScoped<IIdentityService, BackgroundIdentityService>();
        services.AddTransient<IActivityLogger, ActivityLogger>();

        return services;
    }

    private static void ConfigureServices(IServiceCollection services, Action<HostingOptions> action)
    {

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var options = new HostingOptions();

        action(options);

        services.Configure(action);
    }
}
