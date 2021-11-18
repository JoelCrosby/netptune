using System.Threading.Tasks;

using Netptune.Core.Models.Messaging;

namespace Netptune.Core.Messaging;

public interface IEmailRenderService
{
    Task<string> Render(SendEmailModel model);
}