using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Api.Services;
using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Configuration;
using Netptune.Services.Authentication;
using Netptune.Services.Authentication.Interfaces;
using Netptune.Services.Configuration;

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
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var connectionString = isWindows
                ? Configuration.GetConnectionString("ProjectsDatabase")
                : Configuration.GetConnectionString("ProjectsDatabasePostgres");

            services.AddAuth(Configuration);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(
                    options => options.SerializerSettings.ReferenceLoopHandling =
                    Newtonsoft.Json.ReferenceLoopHandling.Ignore
                );

            services.AddTransient<INetptuneAuthService, NetptuneAuthService>();

            services.AddNetptuneRepository(connectionString);
            services.AddNetptuneEntities(options =>
            {
                options.ConnectionString = connectionString;
                options.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            });

            services.AddNetptuneServices();

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