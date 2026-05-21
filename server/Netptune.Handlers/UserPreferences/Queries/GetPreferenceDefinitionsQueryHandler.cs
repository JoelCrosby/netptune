using Mediator;

using Netptune.Core.Preferences;

namespace Netptune.Handlers.UserPreferences.Queries;

public sealed record GetPreferenceDefinitionsQuery : IRequest<PreferenceDefinitionsResponse>;

public sealed class GetPreferenceDefinitionsQueryHandler
    : IRequestHandler<GetPreferenceDefinitionsQuery, PreferenceDefinitionsResponse>
{
    private readonly IPreferenceDefinitionRegistry Registry;

    public GetPreferenceDefinitionsQueryHandler(IPreferenceDefinitionRegistry registry)
    {
        Registry = registry;
    }

    public ValueTask<PreferenceDefinitionsResponse> Handle(
        GetPreferenceDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new PreferenceDefinitionsResponse
        {
            Groups = Registry.GetGroups(),
        });
    }
}
