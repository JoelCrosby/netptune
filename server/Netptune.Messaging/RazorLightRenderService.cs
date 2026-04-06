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
        return await Engine.CompileRenderAsync("Resources.EmailTemplate", model);
    }
}
