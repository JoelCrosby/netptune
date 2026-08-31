using Mediator;

using Netptune.Core.Preferences;
using Netptune.Core.Services;

namespace Netptune.Handlers.UserPreferences.Queries;

public sealed record GetUserPreferenceValuesQuery : IRequest<PreferenceValuesResponse>;

public sealed class GetUserPreferenceValuesQueryHandler
    : IRequestHandler<GetUserPreferenceValuesQuery, PreferenceValuesResponse>
{
    private readonly IIdentityService Identity;
    private readonly IPreferenceDefinitionRegistry Registry;
    private readonly PreferenceValueResolver Resolver;

    public GetUserPreferenceValuesQueryHandler(
        IIdentityService identity,
        IPreferenceDefinitionRegistry registry,
        PreferenceValueResolver resolver)
    {
        Identity = identity;
        Registry = registry;
        Resolver = resolver;
    }

    public async ValueTask<PreferenceValuesResponse> Handle(
        GetUserPreferenceValuesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Resolver.TryGetWorkspaceId();
        var resolved = await Resolver.ResolveAll(userId, workspaceId, cancellationToken);

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
}
