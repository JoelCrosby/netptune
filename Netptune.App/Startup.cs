using AutoMapper;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;
using Netptune.Models.MappingProfiles;
using Netptune.Repositories.Configuration;
using Netptune.Services.Authentication;
using Netptune.Services.Configuration;

namespace Netptune.App
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSpaStaticFiles(configuration => configuration.RootPath = "ClientApp/dist/netptune");

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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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


            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = GetFileExtensionContentTypeProvider()
            });

            app.UseSpaStaticFiles();

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

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:6400");
                }
            });
        }

        private static FileExtensionContentTypeProvider GetFileExtensionContentTypeProvider()
        {
            var provider = new FileExtensionContentTypeProvider();

            provider.Mappings[".webmanifest"] = "application/manifest+json";

            return provider;
        }

        private static void ConfigureDatabase(IServiceCollection services)
        {
            using var serviceScope = services.BuildServiceProvider().CreateScope();

            var context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

            context.Database.EnsureCreated();
        }
    }
}
