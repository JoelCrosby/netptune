namespace Netptune.Core.Services;

public interface ITurnstileService
{
    Task<bool> ValidateAsync(string? token, string? remoteIp = null);
}
