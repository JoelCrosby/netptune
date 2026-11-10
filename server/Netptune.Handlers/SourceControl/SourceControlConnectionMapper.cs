using Netptune.Core.Entities;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Checks;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl;

internal static class SourceControlConnectionMapper
{
    public static SourceControlConnectionViewModel ToViewModel(
        SourceControlConnection connection,
        ISourceControlProviderRegistry registry,
        int linkedRepoCount)
    {
        var scmConnection = connection.ToScmConnection();
        var capabilities = registry.GetEffectiveCapabilities(scmConnection);
        var checker = FindChecker(registry, connection.Provider);
        var storedChecks = ScmConnectionChecks.FromJson(connection.LastCheck);

        return new SourceControlConnectionViewModel
        {
            Id = connection.Id,
            Provider = connection.Provider,
            BaseUrl = connection.BaseUrl,
            AccountLogin = connection.AccountLogin,
            AccountType = connection.AccountType,
            AccountAvatarUrl = connection.AccountAvatarUrl,
            Status = connection.Status,
            Capabilities = SourceControlCapabilityNames.From(capabilities),
            LinkedRepoCount = linkedRepoCount,
            CreatedAt = connection.CreatedAt,
            CredentialHint = connection.CredentialHint,
            CredentialExpiresAt = connection.CredentialExpiresAt,
            CanCheck = checker is not null,
            LastCheckedAt = connection.LastCheckedAt,
            Checks = ToCheckViewModels(checker, storedChecks),
        };
    }

    public static IScmConnectionChecker? FindChecker(
        ISourceControlProviderRegistry registry,
        SourceControlProvider provider)
    {
        var isEnabled = registry.IsEnabled(provider);

        return isEnabled ? registry.Resolve(provider).ConnectionChecker : null;
    }

    public static List<ScmConnectionCheckViewModel> ToCheckViewModels(
        IScmConnectionChecker? checker,
        IReadOnlyList<ScmConnectionCheck> checks)
    {
        if (checker is null)
        {
            return [];
        }

        return checks.Select(check => ToCheckViewModel(checker, check)).ToList();
    }

    private static ScmConnectionCheckViewModel ToCheckViewModel(IScmConnectionChecker checker, ScmConnectionCheck check)
    {
        var description = checker.Describe(check);

        return new ScmConnectionCheckViewModel
        {
            Key = check.Key,
            Status = check.Status,
            IsRequired = description.IsRequired,
            Title = description.Title,
            Detail = description.Detail,
        };
    }
}
