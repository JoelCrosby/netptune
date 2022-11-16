using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.App.Hubs;
using Netptune.App.Utility;
using Netptune.Core.Events;
using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.JobClient;
using Netptune.Messaging;
using Netptune.Repositories.Configuration;
using Netptune.Services.Authentication;
using Netptune.Services.Authorization;
using Netptune.Services.Cache.Redis;
using Netptune.Services.Configuration;
using Netptune.Storage;

using Serilog;

namespace Netptune.App;

public class Startup
{
    private IConfiguration Configuration { get; }
    private IWebHostEnvironment WebHostEnvironment { get; }

    public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        WebHostEnvironment = webHostEnvironment;
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var connectionString = Configuration.GetNetptuneConnectionString();
        var redisConnectionString = Configuration.GetNetptuneRedisConnectionString();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder => builder
                .WithOrigins(Configuration.GetCorsOrigins())
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("Content-Disposition")
            );
        });

        services.AddControllers();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor| ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddSingleton<BuildInfo>();

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = WebHostEnvironment.IsDevelopment();
        });

        services.AddNetptuneIdentity().AddNetptuneIdentityEntities();
        services.AddNeptuneAuthorization();
        services.AddNeptuneAuthentication(options =>
        {
            options.Issuer = Configuration.GetRequiredValue("Tokens:Issuer");
            options.Audience = Configuration.GetRequiredValue("Tokens:Audience");
            options.SecurityKey = Configuration.GetEnvironmentVariable("NETPTUNE_SIGNING_KEY");
            options.GitHubClientId = Configuration.GetEnvironmentVariable("NETPTUNE_GITHUB_CLIENT_ID");
            options.GitHubSecret = Configuration.GetEnvironmentVariable("NETPTUNE_GITHUB_SECRET");
        });

        services.AddNetptuneRedis(options =>
        {
            options.Connection = redisConnectionString;
        });

        services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
        services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

        services.AddNetptuneServices(options =>
        {
            options.ClientOrigin = Configuration.GetRequiredValue("Origin");
            options.ContentRootPath = WebHostEnvironment.ContentRootPath;
        });

        services.AddSendGridEmailService(options =>
        {
            options.SendGridApiKey = Configuration.GetEnvironmentVariable("SEND_GRID_API_KEY");
            options.DefaultFromAddress = Configuration.GetRequiredValue("Email:DefaultFromAddress");
            options.DefaultFromDisplayName = Configuration.GetRequiredValue("Email:DefaultFromDisplayName");
        });

        services.AddS3StorageService(options =>
        {
            options.BucketName = Configuration.GetEnvironmentVariable("NETPTUNE_S3_BUCKET_NAME");
            options.Region = Configuration.GetEnvironmentVariable("NETPTUNE_S3_REGION");
            options.AccessKeyID = Configuration.GetEnvironmentVariable("NETPTUNE_S3_ACCESS_KEY_ID");
            options.SecretAccessKey = Configuration.GetEnvironmentVariable("NETPTUNE_S3_SECRET_ACCESS_KEY");
        });

        services.AddActivityLogger();

        services.AddNetptuneJobClient(options =>
        {
            options.ConnectionString = redisConnectionString;
        });

        services.AddSpaStaticFiles(configuration =>
        {
            configuration.RootPath = Path.Join(WebHostEnvironment.WebRootPath, "dist");
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseSpaStaticFiles();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseCors();

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
}
