using AutoMapper;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;
using Netptune.Models.MappingProfiles;
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
            var connectionString = Configuration.GetConnectionString("ProjectsDatabase");

            services.AddCors();

            services.AddControllers();

            services.AddAutoMapper(typeof(UserMaps));

            services.AddNeptuneAuthentication(Configuration);

            services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
            services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

            services.AddNetptuneServices();

            if (Environment.IsDevelopment())
            {
                ConfigureDatabase(services);
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

        private static void ConfigureDatabase(IServiceCollection services)
        {
            using var serviceScope = services.BuildServiceProvider().CreateScope();

            var context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

            context.Database.EnsureCreated();
        }
    }
}