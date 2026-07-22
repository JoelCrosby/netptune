using Mediator;

using Netptune.Core.Preferences;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.UserPreferences.Queries;

public sealed record GetUserPreferenceValuesQuery : IRequest<PreferenceValuesResponse>;

public sealed class GetUserPreferenceValuesQueryHandler
    : IRequestHandler<GetUserPreferenceValuesQuery, PreferenceValuesResponse>
{
    private readonly IIdentityService Identity;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IPreferenceDefinitionRegistry Registry;

    public GetUserPreferenceValuesQueryHandler(
        IIdentityService identity,
        INetptuneUnitOfWork unitOfWork,
        IPreferenceDefinitionRegistry registry)
    {
        Identity = identity;
        UnitOfWork = unitOfWork;
        Registry = registry;
    }

    public async ValueTask<PreferenceValuesResponse> Handle(
        GetUserPreferenceValuesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await TryGetWorkspaceId();
        var resolved = await ResolveAll(userId, workspaceId, cancellationToken);

        var groups = Registry.GetGroups()
            .Select(group => new PreferenceValueGroup
            {
                Key = group.Key,
                Label = group.Label,
                Order = group.Order,
                Preferences = group.Preferences
                    .Select(definition => resolved[definition.Key])
                    .ToList(),
            })
            .ToList();

        return new PreferenceValuesResponse
        {
            Groups = groups,
        };
    }

    private async Task<int?> TryGetWorkspaceId()
    {
        return Identity.TryGetWorkspaceKey() is null ? null : await Identity.GetWorkspaceId();
    }

    private async Task<Dictionary<string, ResolvedPreferenceValue>> ResolveAll(
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

    private async Task<ResolvedPreferenceValue> Resolve(
        string userId,
        int? workspaceId,
        PreferenceDefinition definition,
        CancellationToken cancellationToken)
    {
        var values = await UnitOfWork.UserPreferences.GetValues(
            userId,
            definition.Key,
            workspaceId,
            cancellationToken) ?? [];

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
}
