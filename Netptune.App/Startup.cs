using System;
using System.IO;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using Netptune.App.Hubs;
using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;
using Netptune.Messaging;
using Netptune.Repositories.Configuration;
using Netptune.Services.Authentication;
using Netptune.Services.Configuration;
using Netptune.Storage;

namespace Netptune.App
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public string[] CorsOrigins => GetCorsOrigins();

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

            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = WebHostEnvironment.IsDevelopment();
            });

            services.AddNeptuneAuthentication(Configuration);

            services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
            services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

            services.AddNetptuneServices(options =>
            {
                options.ClientOrigin = Configuration["Origin"];
                options.ContentRootPath = WebHostEnvironment.ContentRootPath;
            });

            services.AddSendGridEmailService(options =>
            {
                options.DefaultFromAddress = Configuration["Email:DefaultFromAddress"];
                options.DefaultFromDisplayName = Configuration["Email:DefaultFromDisplayName"];
            });

            services.AddS3StorageService(options =>
            {
                options.BucketName = Environment.GetEnvironmentVariable("NETPTUNE_S3_BUCKET_NAME");
                options.AccessKeyID = Environment.GetEnvironmentVariable("NETPTUNE_S3_ACCESS_KEY_ID");
                options.SecretAccessKey = Environment.GetEnvironmentVariable("NETPTUNE_S3_SECRET_ACCESS_KEY");
            });

            services.AddSpaStaticFiles(configuration => configuration.RootPath = Path.Join("..", "dist"));

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

                var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;

                if (string.IsNullOrEmpty(parentDir)) throw new Exception("Unable to get parent Directory");

                var spaPath = Path.Combine(parentDir, "dist");

                app.UseSpaStaticFiles(new StaticFileOptions
                {
                    RequestPath = "/app",
                    FileProvider = new PhysicalFileProvider(spaPath),
                });
            }

            app.UseDefaultFiles();

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = GetFileExtensionContentTypeProvider()
            });

            app.UseRouting();

            app.UseCors(builder => builder
                .WithOrigins(CorsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("Content-Disposition")
            );

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<BoardHub>(BoardHub.Path);
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:6400");
                }
            });
        }

        private string[] GetCorsOrigins()
        {
            return Configuration.GetSection("CorsOrigins")
                .AsEnumerable()
                .Where(pair => pair.Value is { })
                .Select(pair => pair.Value)
                .ToArray();
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

            var envConString = Environment.GetEnvironmentVariable(envVar);

            if (envConString is null) return appSettingsConString;

            return ParseConnectionString(envConString);
        }

        private static string ParseConnectionString(string value)
        {
            var conn = value
                .Replace("//", "")
                .Split('/', ':', '@', '?')
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var user = conn[1];
            var pass = conn[2];
            var server = conn[3];
            var database = conn[5];
            var port = conn[4];

            return $"host={server};port={port};database={database};uid={user};pwd={pass};sslmode=Require;Trust Server Certificate=true;Timeout=1000";
        }
    }
}
