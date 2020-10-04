using Microsoft.Extensions.Options;

using Netptune.Core.Models.Hosting;
using Netptune.Core.Services;

namespace Netptune.Services
{
    public class HostingService : IHostingService
    {
        public string ContentRootPath { get; set; }
        public string ClientOrigin { get; set; }

        public HostingOptions Options { get; set; }

        public HostingService(IOptionsMonitor<HostingOptions> options)
        {
            Options = options.CurrentValue;
            ContentRootPath = Options.ContentRootPath;
            ClientOrigin = Options.ClientOrigin;
        }
    }
}
