using System;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Models.Hosting;
using Netptune.Core.Services;
using Netptune.Core.Services.Export;
using Netptune.Core.Services.Import;
using Netptune.Services.Export;
using Netptune.Services.Import;

namespace Netptune.Services.Configuration
{
    public static class NetptuneServicesConfiguration
    {
        public static void AddNetptuneServices(this IServiceCollection services, Action<HostingOptions> action)
        {
            ConfigureServices(services, action);

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

            services.AddMemoryCache();
            services.AddTransient<IUserConnectionService, UserConnectionService>();

            services.AddHttpContextAccessor();
            services.AddScoped<IIdentityService, IdentityService>();
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
