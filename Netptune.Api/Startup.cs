using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Configuration;
using Netptune.Services.Authentication;
using Netptune.Services.Configuration;

namespace Netptune.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
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

            services.AddCors();

            services.AddControllers();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddNeptuneAuthentication(Configuration);
            services.AddNetptuneRepository(connectionString);
            services.AddNetptuneEntities(options =>
            {
                options.ConnectionString = connectionString;
                options.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            });

            services.AddNetptuneServices();

            if (Environment.IsDevelopment())
            {
                ConfigureDatabase(services).GetAwaiter().GetResult();
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseCors(builder => builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()
            );

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
                endpoints.MapControllers()
            );
        }

        private async Task ConfigureDatabase(IServiceCollection services)
        {
            using var serviceScope = services.BuildServiceProvider().CreateScope();

            var context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

            await context.Database.EnsureCreatedAsync();
        }
    }
}