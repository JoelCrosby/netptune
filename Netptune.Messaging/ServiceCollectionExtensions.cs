using System;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;

using RazorLight;

namespace Netptune.Messaging
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSendGridEmailService(this IServiceCollection services, Action<EmailOptions> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var options = new EmailOptions();
            action(options);

            services.Configure(action);
            services.AddTransient<IEmailService, SendGridEmailService>();

            services.AddRazorLightRenderer();
        }

        public static void AddRazorLightRenderer(this IServiceCollection services)
        {
            var razorEngine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(ServiceCollectionExtensions))
                .UseMemoryCachingProvider()
                .Build();

            services.AddSingleton(razorEngine);
            services.AddTransient<IEmailRenderService, RazorLightRenderService>();
        }
    }
}
