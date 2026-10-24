using System.Globalization;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Netptune.ServiceDefaults.Middleware;

using Xunit;

namespace Netptune.UnitTests.Netptune.ServiceDefaults;

public class RequestLocalizationDefaultsTests
{
    [Fact]
    public async Task Request_ShouldUseTheLocaleCookie_OverTheBrowserLanguage()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie = "nt_locale=de";
        context.Request.Headers.AcceptLanguage = "fr-FR,fr;q=0.9";

        var culture = await ResolveCulture(context);

        culture.UiCulture.Should().Be("de");
    }

    [Fact]
    public async Task Request_ShouldUseTheBrowserLanguage_WhenThereIsNoCookie()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.AcceptLanguage = "es-ES,es;q=0.9";

        var culture = await ResolveCulture(context);

        culture.UiCulture.Should().Be("es");
    }

    [Fact]
    public async Task Request_ShouldFallBackToEnglish_ForAnUnsupportedLanguage()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie = "nt_locale=ja";
        context.Request.Headers.AcceptLanguage = "ja-JP";

        var culture = await ResolveCulture(context);

        culture.UiCulture.Should().Be("en-GB");
    }

    [Fact]
    public async Task Request_ShouldKeepFormattingInvariant_WhateverTheLanguage()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie = "nt_locale=fr";

        var culture = await ResolveCulture(context);

        culture.Culture.Should().Be(CultureInfo.InvariantCulture.Name);
    }

    private static async Task<ResolvedCulture> ResolveCulture(HttpContext context)
    {
        var resolved = new ResolvedCulture(string.Empty, string.Empty);
        var options = Options.Create(RequestLocalizationDefaults.Create());
        var middleware = new RequestLocalizationMiddleware(
            _ =>
            {
                resolved = new ResolvedCulture(CultureInfo.CurrentCulture.Name, CultureInfo.CurrentUICulture.Name);

                return Task.CompletedTask;
            },
            options,
            NullLoggerFactory.Instance);

        await middleware.Invoke(context);

        return resolved;
    }

    private sealed record ResolvedCulture(string Culture, string UiCulture);
}
