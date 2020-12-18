using System;

using Microsoft.Extensions.DependencyInjection;
using Netptune.Core.Cache;
using Netptune.Core.Models.Hosting;
using Netptune.Core.Services;
using Netptune.Core.Services.Export;
using Netptune.Core.Services.Import;
using Netptune.Services.Cache;
using Netptune.Services.Export;
using Netptune.Services.Import;

namespace Netptune.Services.Configuration
{
    public static class NetptuneServicesConfiguration
    {
        public static void AddNetptuneServices(this IServiceCollection services, Action<HostingOptions> action)
        {
            ConfigureServices(services, action);

            services.AddMemoryCache();
            services.AddHttpContextAccessor();

            services.AddTransient<IHostingService, HostingService>();

            services.AddTransient<IProjectService, ProjectService>();
            services.AddTransient<ITaskService, TaskService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IWorkspaceService, WorkspaceService>();
            services.AddTransient<IBoardService, BoardService>();
            services.AddTransient<IBoardGroupService, BoardGroupService>();
            services.AddTransient<ICommentService, CommentService>();
            services.AddTransient<ITagService, TagService>();

            services.AddTransient<ITaskImportService, TaskImportService>();
            services.AddTransient<ITaskExportService, TaskExportService>();
            services.AddTransient<IWebService, WebService>();


            services.AddTransient<IUserConnectionService, UserConnectionService>();

            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IUserCache, UserCache>();
            services.AddScoped<IWorkspaceUserCache, WorkspaceUserCache>();
            services.AddScoped<IInviteCache, InviteCache>();
        }

        private static void ConfigureServices(IServiceCollection services, Action<HostingOptions> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            var options = new HostingOptions();

            action(options);

            services.Configure(action);
        }
    }
}
