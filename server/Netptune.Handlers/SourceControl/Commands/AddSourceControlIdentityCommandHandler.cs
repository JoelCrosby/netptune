using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record AddSourceControlIdentityCommand(AddSourceControlIdentityRequest Request)
    : IRequest<ClientResponse<SourceControlIdentityViewModel>>;

public sealed class AddSourceControlIdentityCommandHandler
    : IRequestHandler<AddSourceControlIdentityCommand, ClientResponse<SourceControlIdentityViewModel>>
{
    private const string ProviderNotEnabled = "That source control provider isn't enabled.";
    private const string UsernameRequired = "Enter a username.";
    private const string UsernameTaken = "That username is already mapped to another member.";

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly TimeProvider Clock;

    public AddSourceControlIdentityCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ISourceControlProviderRegistry registry,
        TimeProvider clock)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Registry = registry;
        Clock = clock;
    }

    public async ValueTask<ClientResponse<SourceControlIdentityViewModel>> Handle(
        AddSourceControlIdentityCommand command,
        CancellationToken cancellationToken)
    {
        var provider = command.Request.Provider;
        var username = command.Request.Username.Trim();
        var isEnabled = Registry.IsEnabled(provider);

        if (!isEnabled)
        {
            return ClientResponse<SourceControlIdentityViewModel>.Failed(ProviderNotEnabled);
        }

        if (username.Length == 0)
        {
            return ClientResponse<SourceControlIdentityViewModel>.Failed(UsernameRequired);
        }

        var userId = Identity.GetCurrentUserId();
        var existing = await UnitOfWork.UserSourceControlIdentities.GetByUsername(
            provider,
            username,
            cancellationToken);

        var isTakenByOtherMember = existing is not null && existing.UserId != userId;

        if (isTakenByOtherMember)
        {
            return ClientResponse<SourceControlIdentityViewModel>.Failed(UsernameTaken);
        }

        if (existing is not null)
        {
            var existingViewModel = SourceControlMapping.ToViewModel(existing);

            return ClientResponse<SourceControlIdentityViewModel>.Success(existingViewModel);
        }

        var now = Clock.GetUtcNow().UtcDateTime;

        var identity = new UserSourceControlIdentity
        {
            UserId = userId,
            Provider = provider,
            IsVerified = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        identity.SetUsername(username);

        await UnitOfWork.UserSourceControlIdentities.AddAsync(identity, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var viewModel = SourceControlMapping.ToViewModel(identity);

        return ClientResponse<SourceControlIdentityViewModel>.Success(viewModel);
    }
}
