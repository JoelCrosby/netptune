using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Providers.AzureDevOps.Dtos;

namespace Netptune.SourceControl.Providers.AzureDevOps;

internal static class AzureDevOpsMapper
{
    private const string BranchRefPrefix = "refs/heads/";

    public static ScmRepository ToRepository(AzureDevOpsRepositoryDto repository)
    {
        var project = repository.Project.Name;

        return new ScmRepository
        {
            Ref = new ScmRepositoryRef(repository.Id, project, repository.Name),
            FullName = $"{project}/{repository.Name}",
            DefaultBranch = ToBranchName(repository.DefaultBranch),
            WebUrl = repository.WebUrl,
            IsPrivate = true,
        };
    }

    private static string ToBranchName(string? branchRef)
    {
        if (branchRef is null)
        {
            return string.Empty;
        }

        var isBranchRef = branchRef.StartsWith(BranchRefPrefix, StringComparison.Ordinal);

        return isBranchRef ? branchRef[BranchRefPrefix.Length..] : branchRef;
    }
}
