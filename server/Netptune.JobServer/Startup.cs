using System;

using Hangfire;
using Hangfire.Redis;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Core.Constants;
using Netptune.Core.Jobs;
using Netptune.Core.Utilities;
using Netptune.Entities.Configuration;
using Netptune.Messaging;
using Netptune.Repositories.Configuration;
using Netptune.Services.Cache.Redis;
using Netptune.Services.Configuration;
using Netptune.Storage;

using StackExchange.Redis;

namespace Netptune.JobServer
{
    public class Startup
    {
        private static ConnectionMultiplexer Redis;
        private static string RedisConnectionString;

        private IConfiguration Configuration { get; }
        private IWebHostEnvironment WebHostEnvironment { get; }


        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            WebHostEnvironment = webHostEnvironment;
            Configuration = configuration;

            ConnectRedis();
        }

        private static void ConnectRedis()
        {
            RedisConnectionString = ConnectionStringParser.ParseRedis(Environment.GetEnvironmentVariable("REDIS_URL"));

            Redis = ConnectionMultiplexer.Connect(RedisConnectionString);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(configuration =>
            {
                configuration.UseRedisStorage(Redis, new RedisStorageOptions
                {
                    Prefix = NetptuneJobConstants.RedisPrefix
                });
            });

            services.AddHangfireServer();

            services.AddTransient<IJobClient, EmptyJobClient>();

            services.AddNetptuneRedis(options =>
            {
                options.Connection = RedisConnectionString;
            });

            var connectionString = GetConnectionString();

            services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
            services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

            services.AddNetptuneServices(options =>
            {
                options.ClientOrigin = Configuration["Origin"];
                options.ContentRootPath = WebHostEnvironment.ContentRootPath;
            });

            services.AddSendGridEmailService(options =>
            {
                options.SendGridApiKey = Environment.GetEnvironmentVariable("SEND_GRID_API_KEY");
                options.DefaultFromAddress = Configuration["Email:DefaultFromAddress"];
                options.DefaultFromDisplayName = Configuration["Email:DefaultFromDisplayName"];
            });

            services.AddS3StorageService(options =>
            {
                options.BucketName = Environment.GetEnvironmentVariable("NETPTUNE_S3_BUCKET_NAME");
                options.Region = Environment.GetEnvironmentVariable("NETPTUNE_S3_REGION");
                options.AccessKeyID = Environment.GetEnvironmentVariable("NETPTUNE_S3_ACCESS_KEY_ID");
                options.SecretAccessKey = Environment.GetEnvironmentVariable("NETPTUNE_S3_SECRET_ACCESS_KEY");
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireServer();

            app.UseHangfireDashboard("",
                new DashboardOptions
                {
                    DashboardTitle = "Netptune Jobs",
                    DisplayStorageConnectionString = true,
                    Authorization = new[]
                    {
                        new HangfireAuthorizationFilter()
                    }
                });
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
    }
}
