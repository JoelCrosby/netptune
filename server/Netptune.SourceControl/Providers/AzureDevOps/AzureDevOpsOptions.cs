namespace Netptune.SourceControl.Providers.AzureDevOps;

internal sealed class AzureDevOpsOptions
{
    public const string SectionName = "SourceControl:Providers:AzureDevOps";

    public bool Enabled { get; set; }

    public string ApiBaseUrl { get; set; } = "https://dev.azure.com";

    public bool IsComplete()
    {
        var hasApiBaseUrl = Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _);

        return hasApiBaseUrl;
    }
}
