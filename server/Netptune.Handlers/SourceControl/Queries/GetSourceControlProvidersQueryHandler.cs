using Mediator;

using Netptune.Core.SourceControl;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Queries;

public sealed record GetSourceControlProvidersQuery : IRequest<List<SourceControlProviderViewModel>>;

public sealed class GetSourceControlProvidersQueryHandler
    : IRequestHandler<GetSourceControlProvidersQuery, List<SourceControlProviderViewModel>>
{
    private readonly ISourceControlProviderRegistry Registry;

    public GetSourceControlProvidersQueryHandler(ISourceControlProviderRegistry registry)
    {
        Registry = registry;
    }

    public ValueTask<List<SourceControlProviderViewModel>> Handle(
        GetSourceControlProvidersQuery query,
        CancellationToken cancellationToken)
    {
        var providers = Registry.All
            .Select(provider => new SourceControlProviderViewModel
            {
                Provider = provider.Provider,
                Terminology = provider.Terminology,
                Capabilities = SourceControlCapabilityNames.From(provider.Capabilities),
            })
            .ToList();

        return ValueTask.FromResult(providers);
    }
}
