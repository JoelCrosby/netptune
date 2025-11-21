using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;

using RazorLight;

namespace Netptune.Messaging;

public class RazorLightRenderService : IEmailRenderService
{
    private readonly RazorLightEngine Engine;

    public RazorLightRenderService(RazorLightEngine engine)
    {
        Engine = engine;
    }

    public Task<string> Render(SendEmailModel model)
    {
        return GetHtmlContent(model);
    }

    private async Task<string> GetHtmlContent(SendEmailModel model)
    {
        var template = await GetTemplateFromEmbeddedResource("Netptune.Messaging.Resources.EmailTemplate.cshtml");

        return await Engine.CompileRenderStringAsync("EmailTemplate", template, model);
    }

    private static async Task<string> GetTemplateFromEmbeddedResource(string resourceName)
    {
        var assembly = typeof(RazorLightRenderService).GetTypeInfo().Assembly;
        var names = assembly.GetManifestResourceNames();

        if (!names.Contains(resourceName))
        {
            throw new Exception($"Unable to find embedded resource {resourceName} in assembly {assembly.FullName}.");
        }

        await using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new Exception($"Resource stream for resource {resourceName} returned null.");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);

        return await reader.ReadToEndAsync();
    }
}
