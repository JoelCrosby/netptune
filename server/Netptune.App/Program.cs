using System;
using System.IO;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;

namespace Netptune.App
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateLogsFolder();

            var currentDir = Directory.GetCurrentDirectory();
            var logPath = Path.Join(currentDir, "App_Data", "log", "netptune-app-.txt");

            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("App Name", "Netptune.App")
#if DEBUG
                .MinimumLevel.Information()
                .MinimumLevel.Override("Otis", LogEventLevel.Debug)
#else
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)

#endif
                .WriteTo.Console()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSerilog();
                    webBuilder.UseStartup<Startup>();
                });
        }

        private static void CreateLogsFolder()
        {
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                var appDataDir = Path.Join(currentDir, "App_Data", "log");

                Directory.CreateDirectory(appDataDir);
            }
            catch (Exception)
            {
                Log.Error("Unable to create App_Data directory. See inner-exception for details");
            }
        }
    }
}
