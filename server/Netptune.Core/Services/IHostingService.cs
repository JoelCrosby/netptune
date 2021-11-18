namespace Netptune.Core.Services;

public interface IHostingService
{
    string ContentRootPath { get; set; }

    string ClientOrigin { get; set; }
}