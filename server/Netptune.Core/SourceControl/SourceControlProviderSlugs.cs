namespace Netptune.Core.SourceControl;

public static class SourceControlProviderSlugs
{
    private static readonly Dictionary<string, SourceControlProvider> ProvidersBySlug =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["github"] = SourceControlProvider.GitHub,
        ["azure-devops"] = SourceControlProvider.AzureDevOps,
        ["gitlab"] = SourceControlProvider.GitLab,
    };

    public static bool TryParse(string slug, out SourceControlProvider provider)
    {
        return ProvidersBySlug.TryGetValue(slug, out provider);
    }
}
