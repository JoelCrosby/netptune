using Hangfire;
using Hangfire.Redis.StackExchange;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Core.Constants;
using Netptune.Core.Events;
using Netptune.Core.Extensions;
using Netptune.Core.Jobs;
using Netptune.Entities.Configuration;
using Netptune.JobServer.Auth;
using Netptune.JobServer.Data;
using Netptune.JobServer.Util;
using Netptune.Messaging;
using Netptune.Repositories.Configuration;
using Netptune.Services.Cache.Redis;
using Netptune.Services.Configuration;
using Netptune.Storage;

namespace Netptune.JobServer;

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
        var connectionString = Configuration.GetNetptuneConnectionString("netptune");
        var jobsConnectionString = Configuration.GetNetptuneConnectionString("netptune-jobs");

        var redisConnectionString = Configuration.GetNetptuneRedisConnectionString();

        services.AddHangfire(configuration =>
        {
            configuration.UseRedisStorage(redisConnectionString, new ()
            {
                Prefix = NetptuneJobConstants.RedisPrefix,
            });
        });

        services.AddHangfireServer();

        services.AddTransient<IJobClient, EmptyJobClient>();

        services.AddNetptuneRedis(options =>
        {
            options.Connection = redisConnectionString;
        });

        services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
        services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

        services.AddNetptuneJobServerAuth();
        services.AddNetptuneJobServerEntities(options => options.ConnectionString = jobsConnectionString);

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

        services.AddActivitySink();
    }

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
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
        });

        app.UseHangfireDashboard("", new ()
        {
            DisplayStorageConnectionString = false,
            DashboardTitle = "Netptune Jobs",
            AppPath = "/account/logout",
            Authorization = new[]
            {
                new HangfireAuthorizationFilter(),
            },
        });
    }
}
