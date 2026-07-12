namespace Netptune.Core.Http;

public class SafeHttpClientOptions
{
    public long MaxResponseBytes { get; set; } = 2 * 1024 * 1024;

    public int MaxRedirects { get; set; } = 3;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public string UserAgent { get; set; } = "Netptune/1.0";
}
