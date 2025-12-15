using System;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Messaging;

using RazorLight;

namespace Netptune.Messaging;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddSendGridEmailService(Action<SendGridEmailOptions> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var options = new SendGridEmailOptions();
            action(options);

            services.Configure(action);
            services.AddTransient<IEmailService, SendGridEmailService>();

            services.AddRazorLightRenderer();
        }

        private void AddRazorLightRenderer()
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
