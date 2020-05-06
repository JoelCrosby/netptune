using AutoMapper;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;
using Netptune.Models.MappingProfiles;
using Netptune.Repositories.Configuration;
using Netptune.Services.Authentication;
using Netptune.Services.Configuration;

using System;
using System.IO;
using System.Linq;

namespace Netptune.App
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            WebHostEnvironment = webHostEnvironment;
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = GetConnectionString();

            services.AddCors();
            services.AddControllers();

            services.AddAutoMapper(typeof(UserMaps));

            services.AddNeptuneAuthentication(Configuration);

            services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
            services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

            services.AddNetptuneServices();

            services.AddSpaStaticFiles(configuration => configuration.RootPath = "../dist");

            ConfigureDatabase(services);
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
                app.UseExceptionHandler("/error");
                app.UseHsts();
                app.UseHttpsRedirection();

                var spaPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName, "dist");

                app.UseSpaStaticFiles(new StaticFileOptions
                {
                    RequestPath = "/app",
                    FileProvider = new PhysicalFileProvider(spaPath),
                });
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = GetFileExtensionContentTypeProvider()
            });

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

        private string GetConnectionString()
        {
            var appSettingsConString = Configuration.GetConnectionString("ProjectsDatabase");
            var envVar = Configuration["ConnectionStringEnvironmentVariable"];

            if (envVar is null) return appSettingsConString;

            var envConstring = Environment.GetEnvironmentVariable(envVar);

            if (envConstring is null) return appSettingsConString;

            return ParseConnectionString(envConstring);
        }

        private static string ParseConnectionString(string value)
        {
            value.Replace("//", "");

            var delimiterChars = new [] { '/', ':', '@', '?' };
            var conn = value.Split(delimiterChars);

            conn = conn.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            var user = conn[1];
            var pass = conn[2];
            var server = conn[3];
            var database = conn[5];
            var port = conn[4];

            return $"host={server};port={port};database={database};uid={user};pwd={pass};sslmode=Require;Trust Server Certificate=true;Timeout=1000";
        }
    }
}
