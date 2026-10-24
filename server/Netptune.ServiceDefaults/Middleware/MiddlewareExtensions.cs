using Microsoft.AspNetCore.Builder;

namespace Netptune.ServiceDefaults.Middleware;

public static class MiddlewareExtensions
{
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseNetptuneRequestDefaults(
            RequestLocaleSource localeSource = RequestLocaleSource.Reader)
        {
            var localization = RequestLocalizationDefaults.Create(localeSource);

            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<ServerErrorLoggingMiddleware>();

            return app.UseRequestLocalization(localization);
        }
    }
}
