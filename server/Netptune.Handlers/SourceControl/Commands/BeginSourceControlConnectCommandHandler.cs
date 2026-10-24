using Mediator;

using Netptune.Core.Localization;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record BeginSourceControlConnectCommand(BeginSourceControlConnectRequest Request)
    : IRequest<ClientResponse<SourceControlConnectStartViewModel>>;

public sealed class BeginSourceControlConnectCommandHandler
    : IRequestHandler<BeginSourceControlConnectCommand, ClientResponse<SourceControlConnectStartViewModel>>
{
    private readonly IIdentityService Identity;
    private readonly ISourceControlProviderRegistry Registry;

    public BeginSourceControlConnectCommandHandler(IIdentityService identity, ISourceControlProviderRegistry registry)
    {
        Identity = identity;
        Registry = registry;
    }

    public async ValueTask<ClientResponse<SourceControlConnectStartViewModel>> Handle(
        BeginSourceControlConnectCommand command,
        CancellationToken cancellationToken)
    {
        var providerKind = command.Request.Provider;
        var isEnabled = Registry.IsEnabled(providerKind);

        if (!isEnabled)
        {
            var message = Localized.Format(AppText.ErrorNotEnabledNetptuneInstance, providerKind);

            return ClientResponse<SourceControlConnectStartViewModel>.Failed(message);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var provider = Registry.Resolve(providerKind);
        var start = provider.Connector.BeginConnect(new ScmConnectRequest(workspaceId, userId));
        var viewModel = new SourceControlConnectStartViewModel(start.RedirectUrl);

        return ClientResponse<SourceControlConnectStartViewModel>.Success(viewModel);
    }
}
