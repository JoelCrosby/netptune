using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Services;

namespace Netptune.Services.Configuration
{
    public static class NetptuneServicesConfiguration
    {
        public static void AddNetptuneServices(this IServiceCollection services)
        {
            services.AddTransient<IProjectService, ProjectService>();
            services.AddTransient<ITaskService, TaskService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IWorkspaceService, WorkspaceService>();
            services.AddTransient<IBoardService, BoardService>();
            services.AddTransient<IBoardGroupService, BoardGroupService>();

            services.AddHttpContextAccessor();
            services.AddScoped<IIdentityService, IdentityService>();
        }
    }
}
