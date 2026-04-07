using System.Reflection;

namespace Netptune.App.Utility;

public class BuildInfo
{
    private BuildInfoViewModel? CachedBuildInfo;

    public BuildInfoViewModel GetBuildInfo()
    {
        if (CachedBuildInfo is not null)
        {
            return CachedBuildInfo;
        }

        CachedBuildInfo = CreateBuildInfoModel();
        return CachedBuildInfo;
    }

    private static  BuildInfoViewModel CreateBuildInfoModel()
    {
        var version = $"1.0.0+LOCAL+REF+{DateTime.UtcNow:yyyyMMdd}.0+LOCAL_BUILD";
        var infoVerAttr = GetInfoVerAttr();

        if (infoVerAttr is not null && infoVerAttr.InformationalVersion.Length > 6)
        {
            // Hash is embedded in the version after a '+' symbol
            // e.g. 1.0.0+a34a913742f8845d3da5309b7b17242222d41a21

            version = infoVerAttr.InformationalVersion;
        }

        var values = version.Split("+");

        var gitHash = values.ElementAtOrDefault(1);
        var gitHubRef = values.ElementAtOrDefault(2);
        var buildNumber = values.ElementAtOrDefault(3);
        var runId = values.ElementAtOrDefault(4);

        return new BuildInfoViewModel
        {
            BuildNumber = buildNumber,
            RunId = runId,
            GitHash = gitHash,
            GitHubRef = gitHubRef,
            GitHashShort = GetShortGitHash(gitHash),
        };
    }

    private static string? GetShortGitHash(string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return null;
        return hash.Length < 6 ? hash : hash.Substring(hash.Length - 6, 6);
    }

    private static AssemblyInformationalVersionAttribute? GetInfoVerAttr()
    {
        var appAssembly = typeof(BuildInfo).Assembly;
        var versionAttr = appAssembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));
        return versionAttr as AssemblyInformationalVersionAttribute;
    }
}

public class BuildInfoViewModel
{
    public string? GitHash { get; init; }

    public string? GitHashShort { get; init; }

    public string? GitHubRef { get; init; }

    public string? BuildNumber { get; init; }

    public string? RunId { get; init; }
}
