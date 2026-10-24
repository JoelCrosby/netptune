using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

using Netptune.Core.Localization;

namespace Netptune.ServiceDefaults.Middleware;

public static class RequestLocalizationDefaults
{
    public const string LocaleCookieName = "nt_locale";

    // Only the UI culture follows the user. Formatting stays invariant, as it was before localization,
    // so dates and numbers the server writes into payloads and files do not change with the reader.
    public static RequestLocalizationOptions Create()
    {
        var uiCultures = AppLocales.Supported
            .Select(CultureInfo.GetCultureInfo)
            .ToList();
        var defaultUiCulture = CultureInfo.GetCultureInfo(AppLocales.Default);

        return new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture, defaultUiCulture),
            SupportedCultures = [CultureInfo.InvariantCulture],
            SupportedUICultures = uiCultures,
            FallBackToParentUICultures = true,
            RequestCultureProviders =
            [
                new CustomRequestCultureProvider(ReadLocaleCookie),
                new AcceptLanguageHeaderRequestCultureProvider(),
            ],
        };
    }

    private static Task<ProviderCultureResult?> ReadLocaleCookie(HttpContext context)
    {
        var cookie = context.Request.Cookies[LocaleCookieName];
        var locale = AppLocales.Match(cookie);

        if (locale is null)
        {
            return Task.FromResult<ProviderCultureResult?>(null);
        }

        var result = new ProviderCultureResult(CultureInfo.InvariantCulture.Name, locale);

        return Task.FromResult<ProviderCultureResult?>(result);
    }
}
