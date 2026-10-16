using Netptune.Core.Entities;
using Netptune.Core.SourceControl;
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
        };
    }
}
