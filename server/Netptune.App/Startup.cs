using System;
using System.IO;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using Netptune.App.Hubs;
using Netptune.App.Utility;
using Netptune.Core.Events;
using Netptune.Core.Utilities;
using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;
using Netptune.JobClient;
using Netptune.Messaging;
using Netptune.Repositories.Configuration;
using Netptune.Services.Authentication;
using Netptune.Services.Authorization;
using Netptune.Services.Cache.Redis;
using Netptune.Services.Configuration;
using Netptune.Storage;

using Polly;

using Serilog;

namespace Netptune.App;

public class Startup
{
    private IConfiguration Configuration { get; }
    private IWebHostEnvironment WebHostEnvironment { get; }

    private string[] CorsOrigins => GetCorsOrigins();

    public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        WebHostEnvironment = webHostEnvironment;
        Configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        var connectionString = GetConnectionString();
        var redisConnectionString = GetRedisConnectionString();

        services.AddCors();
        services.AddControllers();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor| ForwardedHeaders.XForwardedProto;
        });

        services.AddSingleton<BuildInfo>();

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = WebHostEnvironment.IsDevelopment();
        });

        services.AddNeptuneAuthorization();
        services.AddNeptuneAuthentication(options =>
        {
            options.Issuer = Configuration["Tokens:Issuer"];
            options.Audience = Configuration["Tokens:Audience"];
            options.SecurityKey = Environment.GetEnvironmentVariable("NETPTUNE_SIGNING_KEY")!;
            options.GitHubClientId = Environment.GetEnvironmentVariable("NETPTUNE_GITHUB_CLIENT_ID")!;
            options.GitHubSecret = Environment.GetEnvironmentVariable("NETPTUNE_GITHUB_SECRET")!;
        });

        services.AddNetptuneRedis(options =>
        {
            options.Connection = redisConnectionString;
        });

        services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
        services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

        services.AddNetptuneServices(options =>
        {
            options.ClientOrigin = Configuration["Origin"];
            options.ContentRootPath = WebHostEnvironment.ContentRootPath;
        });

        services.AddSendGridEmailService(options =>
        {
            options.SendGridApiKey = Environment.GetEnvironmentVariable("SEND_GRID_API_KEY")!;
            options.DefaultFromAddress = Configuration["Email:DefaultFromAddress"];
            options.DefaultFromDisplayName = Configuration["Email:DefaultFromDisplayName"];
        });

        services.AddS3StorageService(options =>
        {
            options.BucketName = Environment.GetEnvironmentVariable("NETPTUNE_S3_BUCKET_NAME")!;
            options.Region = Environment.GetEnvironmentVariable("NETPTUNE_S3_REGION")!;
            options.AccessKeyID = Environment.GetEnvironmentVariable("NETPTUNE_S3_ACCESS_KEY_ID")!;
            options.SecretAccessKey = Environment.GetEnvironmentVariable("NETPTUNE_S3_SECRET_ACCESS_KEY")!;
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

        ConfigureDatabase(services);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        });

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // TODO: remove the below middleware when possible.

            // This is currently being used to ensure that the github oauth middleware
            // generates the callback url with a https:// scheme. Without this middleware
            // it generates a plain http url.

            // This is due to the app not receiving XForwardedFor headers correctly from
            // the nginx reverse proxy

            // See links below for more detail

            // https://github.com/dotnet/AspNetCore.Docs/issues/2384
            // https://github.com/aspnet/Security/issues/1070

            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";
                return next();
            });

            app.UseExceptionHandler("/error");
            app.UseHsts();
            app.UseHttpsRedirection();

            var spaPath = Path.Join(WebHostEnvironment.WebRootPath, "dist");

            app.UseSpaStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(spaPath),
            });
        }

        app.UseDefaultFiles();

        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = GetFileExtensionContentTypeProvider(),
        });

        app.UseSerilogRequestLogging();

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
        var provider = new FileExtensionContentTypeProvider
        {
            Mappings =
            {
                [".webmanifest"] = "application/manifest+json",
            },
        };

        return provider;
    }

    private static void ConfigureDatabase(IServiceCollection services)
    {
        using var serviceScope = services.BuildServiceProvider().CreateScope();

        var context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

        Policy
            .Handle<Exception>()
            .WaitAndRetry(4, _ => TimeSpan.FromSeconds(4))
            .Execute(() => context.Database.EnsureCreated());
    }

    private string GetConnectionString()
    {
        var appSettingsConString = Configuration.GetConnectionString("netptune");
        var envVar = Configuration["ConnectionStringEnvironmentVariable"];

        if (envVar is null) return appSettingsConString;

        var envConString = Environment.GetEnvironmentVariable(envVar);

        if (envConString is null) return appSettingsConString;

        return ConnectionStringParser.ParseConnectionString(envConString);
    }

    private string GetRedisConnectionString()
    {
        var appSettingsConString = Configuration.GetConnectionString("redis");
        var envVar = Environment.GetEnvironmentVariable("REDIS_URL");

        var connectionString = envVar ?? appSettingsConString;

        if (connectionString is null)
        {
            throw new Exception("An environment variable with the key of {REDIS_URL} not found.");
        }

        return ConnectionStringParser.ParseRedis(connectionString);
    }
}
