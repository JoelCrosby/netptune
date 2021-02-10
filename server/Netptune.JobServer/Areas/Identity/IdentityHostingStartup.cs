using Microsoft.AspNetCore.Hosting;

using Netptune.JobServer.Areas.Identity;

[assembly: HostingStartup(typeof(IdentityHostingStartup))]

namespace Netptune.JobServer.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
        }
    }
}
