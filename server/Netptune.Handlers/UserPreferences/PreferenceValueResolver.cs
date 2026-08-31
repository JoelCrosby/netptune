using Netptune.Core.Preferences;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.UserPreferences;

public sealed class PreferenceValueResolver
{
    private readonly IIdentityService Identity;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IPreferenceDefinitionRegistry Registry;

    public PreferenceValueResolver(
        IIdentityService identity,
        INetptuneUnitOfWork unitOfWork,
        IPreferenceDefinitionRegistry registry)
    {
        Identity = identity;
        UnitOfWork = unitOfWork;
        Registry = registry;
    }

    // Preferences are readable outside a workspace, so a request that names none resolves the global
    // scope rather than failing.
    public async Task<int?> TryGetWorkspaceId()
    {
        return Identity.TryGetWorkspaceKey() is null ? null : await Identity.GetWorkspaceId();
    }

    public async Task<ResolvedPreferenceValue> Resolve(
        string userId,
        int? workspaceId,
        PreferenceDefinition definition,
        CancellationToken cancellationToken)
    {
        var values = await UnitOfWork.UserPreferences.GetValues(
            userId,
            definition.Key,
            workspaceId,
            cancellationToken);

        var globalValue = values.FirstOrDefault(value => value.WorkspaceId is null)?.Value.RootElement.Clone();
        var workspaceValue = values.FirstOrDefault(value => value.WorkspaceId == workspaceId)?.Value.RootElement.Clone();
        var source = workspaceValue is not null ? PreferenceScopes.Workspace :
            globalValue is not null ? PreferenceScopes.Global : "default";
        var effectiveValue = workspaceValue ?? globalValue ?? definition.DefaultValue;

        return new ResolvedPreferenceValue
        {
            Definition = definition,
            GlobalValue = globalValue,
            WorkspaceValue = workspaceValue,
            EffectiveValue = effectiveValue.Clone(),
            Source = source,
        };
    }

    public async Task<Dictionary<string, ResolvedPreferenceValue>> ResolveAll(
        string userId,
        int? workspaceId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, ResolvedPreferenceValue>(StringComparer.Ordinal);

        foreach (var group in Registry.GetGroups())
        {
            foreach (var definition in group.Preferences)
            {
                result[definition.Key] = await Resolve(userId, workspaceId, definition, cancellationToken);
            }
        }

        return result;
    }
}
