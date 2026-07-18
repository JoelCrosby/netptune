using Netptune.Handlers.Projects.Queries;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Tasks.Commands;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.PublicApi.Configuration;

public static class PublicApiHandlers
{
    public static IServiceCollection AddPublicApiHandlers(this IServiceCollection services)
    {
        services.AddTransient<GetProjectsQueryHandler>();
        services.AddTransient<GetStatusesQueryHandler>();
        services.AddTransient<GetTasksQueryHandler>();
        services.AddTransient<GetTaskQueryHandler>();
        services.AddTransient<CreateTaskCommandHandler>();
        services.AddTransient<UpdateTaskCommandHandler>();

        return services;
    }
}
