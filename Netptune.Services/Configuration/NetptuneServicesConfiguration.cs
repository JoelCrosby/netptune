using System;

using AutoMapper;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.MappingProfiles;
using Netptune.Core.Models.Hosting;
using Netptune.Core.Services;

namespace Netptune.Services.Configuration
{
    public static class NetptuneServicesConfiguration
    {
        public static void AddNetptuneServices(this IServiceCollection services, Action<NetptuneServiceOptions> action)
        {
            ConfigureServices(services, action);

            services.AddAutoMapper(typeof(UserMaps));

            services.AddTransient<IHostingService, HostingService>();

            services.AddTransient<IProjectService, ProjectService>();
            services.AddTransient<ITaskService, TaskService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IWorkspaceService, WorkspaceService>();
            services.AddTransient<IBoardService, BoardService>();
            services.AddTransient<IBoardGroupService, BoardGroupService>();
            services.AddTransient<ICommentService, CommentService>();

            services.AddHttpContextAccessor();
            services.AddScoped<IIdentityService, IdentityService>();
        }

        private static void ConfigureServices(IServiceCollection services, Action<NetptuneServiceOptions> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            var options = new NetptuneServiceOptions
            {
                HostingOptions = new HostingOptions(),
            };

            action(options);

            services.Configure(action);
        }
    }
}
