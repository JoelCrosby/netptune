using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Api.Services;
using Netptune.Entities.Contexts;
using Netptune.Repository;
using Netptune.Repository.Interfaces;
using Netptune.Services.Authentication;
using Netptune.Services.Authentication.Interfaces;

namespace Netptune.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<DbContext, DataContext>();
            services.AddDbContext<DataContext>(options =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    options
                        .UseLazyLoadingProxies()
                        .UseSqlServer(Configuration.GetConnectionString("ProjectsDatabase"));
                }
                else
                {
                    options.UseNpgsql(Configuration.GetConnectionString("ProjectsDatabasePostgres"));
                }
            });

            services.AddAuth(Configuration);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(
                    options => options.SerializerSettings.ReferenceLoopHandling =
                    Newtonsoft.Json.ReferenceLoopHandling.Ignore
                );

            services.AddTransient<INetptuneAuthService, NetptuneAuthService>();

            // Register Repository services.
            services.AddTransient<ITaskRepository, TaskRepository>();
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IProjectRepository, ProjectRepository>();
            services.AddTransient<IWorkspaceRepository, WorkspaceRepository>();

            // Register the Swagger.
            services.AddSwagger();

            if (Environment.IsDevelopment())
            {
                ConfigureDatabase(services).GetAwaiter().GetResult();
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();

            app.UseSwagger();

            app.UseSwaggerUI(config =>
            {
                config.SwaggerEndpoint("/swagger/v1/swagger.json", "Netptune API V1");
                config.DocumentTitle = "Netptune Api";
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });
        }

        private async Task ConfigureDatabase(IServiceCollection services)
        {
            using (var serviceScope = services.BuildServiceProvider().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

                await context.Database.EnsureCreatedAsync();
            }
        }
    }
}