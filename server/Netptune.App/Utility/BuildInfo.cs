using System;
using System.Linq;
using System.Reflection;

namespace Netptune.App.Utility;

public class BuildInfo
{
    private string GitHash;
    private string GitHashShort;
    private string GitHubRef;
    private string BuildNumber;
    private string RunId;

    private BuildInfoViewModel BuildInfoViewModel;

    public BuildInfoViewModel GetBuildInfo()
    {
        if (BuildInfoViewModel is {})
        {
            return BuildInfoViewModel;
        }

        GetGitHash();
        GetShortGitHash();

        var info = new BuildInfoViewModel
        {
            GitHash = GitHash,
            GitHashShort = GitHashShort,
            GitHubRef = GitHubRef,
            BuildNumber = BuildNumber,
            RunId = RunId,
        };

        BuildInfoViewModel = info;

        return BuildInfoViewModel;
    }

    public string GetGitHash()
    {
        if (!string.IsNullOrEmpty(GitHash))
        {
            return GitHash;
        }

        var version = $"1.0.0+LOCAL+REF+{DateTime.UtcNow:yyyyMMdd}.0+LOCAL_BUILD";
        var infoVerAttr = GetInfoVerAttr();

        if (infoVerAttr is { } && infoVerAttr.InformationalVersion.Length > 6)
        {
            // Hash is embedded in the version after a '+' symbol
            // e.g. 1.0.0+a34a913742f8845d3da5309b7b17242222d41a21

            version = infoVerAttr.InformationalVersion;
        }

        var values = version.Split("+");

        GitHash = values.ElementAtOrDefault(1);
        GitHubRef = values.ElementAtOrDefault(2);
        BuildNumber = values.ElementAtOrDefault(3);
        RunId = values.ElementAtOrDefault(4);

        return GitHash;
    }

    public string GetShortGitHash()
    {
        if (string.IsNullOrEmpty(GitHashShort))
        {
            GitHashShort = GitHash.Length < 6
                ? GitHash
                : GitHash.Substring(GitHash.Length - 6, 6);
        }

        return GitHashShort;
    }

    private static AssemblyInformationalVersionAttribute GetInfoVerAttr()
    {
        var appAssembly = typeof(BuildInfo).Assembly;

        return (AssemblyInformationalVersionAttribute)appAssembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute))
            .FirstOrDefault();
    }
}

public class BuildInfoViewModel
{
    public string GitHash { get; set; }

    public string GitHashShort { get; set; }

    public string GitHubRef { get; set; }

    public string BuildNumber { get; set; }

    public string RunId { get; set; }
}