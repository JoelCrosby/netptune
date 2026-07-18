using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Authentication;
using Netptune.Core.Entities;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ServiceAccounts;

namespace Netptune.Handlers.ServiceAccounts.Commands;

public sealed record CreateApiCredentialCommand(int ServiceAccountId, CreateApiCredentialRequest Request)
    : IRequest<ClientResponse<ApiCredentialCreatedViewModel>>;

public sealed class CreateApiCredentialCommandHandler
    : IRequestHandler<CreateApiCredentialCommand, ClientResponse<ApiCredentialCreatedViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public CreateApiCredentialCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<ApiCredentialCreatedViewModel>> Handle(
        CreateApiCredentialCommand command,
        CancellationToken cancellationToken)
    {
        var currentUser = await Identity.GetCurrentUser();

        if (currentUser.UserType != AppUserType.User)
        {
            return ClientResponse<ApiCredentialCreatedViewModel>.Forbidden;
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var serviceAccount = await UnitOfWork.ServiceAccounts.GetForManagement(
            command.ServiceAccountId,
            workspaceId,
            cancellationToken);

        if (serviceAccount is null)
        {
            return ClientResponse<ApiCredentialCreatedViewModel>.NotFound;
        }

        if (serviceAccount.DisabledAt.HasValue)
        {
            return ClientResponse<ApiCredentialCreatedViewModel>.Failed("Deleted service accounts cannot create credentials.");
        }

        var membership = await UnitOfWork.WorkspaceUsers.GetUserPermissions(
            serviceAccount.UserId,
            Identity.GetWorkspaceKey(),
            cancellationToken: cancellationToken);

        if (membership is null)
        {
            return ClientResponse<ApiCredentialCreatedViewModel>.Failed("Service account does not have a workspace membership.");
        }

        var scopes = command.Request.Scopes.Count == 0
            ? membership.Permissions.OrderBy(scope => scope).ToList()
            : command.Request.Scopes
                .Select(scope => scope.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(scope => scope)
                .ToList();

        if (scopes.Any(scope => !NetptunePermissions.All.Contains(scope) || !membership.Permissions.Contains(scope)))
        {
            return ClientResponse<ApiCredentialCreatedViewModel>.Failed("Credential scopes must be a subset of the service account permissions.");
        }

        var name = command.Request.Name.Trim();

        if (name.Length is < 2 or > 128)
        {
            return ClientResponse<ApiCredentialCreatedViewModel>.Failed("Credential name must be between 2 and 128 characters.");
        }

        var now = DateTime.UtcNow;
        var expiresAt = command.Request.ExpiresAt ?? now.AddDays(90);

        if (expiresAt <= now || expiresAt > now.AddDays(366))
        {
            return ClientResponse<ApiCredentialCreatedViewModel>.Failed("Credential expiry must be within the next 366 days.");
        }

        var credentialId = Guid.NewGuid();
        var token = ApiCredentialToken.Create(credentialId);
        var credential = new ApiCredential
        {
            Id = credentialId,
            ServiceAccountId = serviceAccount.Id,
            Name = name,
            TokenPrefix = token.Prefix,
            SecretHash = token.SecretHash,
            Scopes = scopes,
            CreatedAt = now,
            CreatedByUserId = currentUser.Id,
            ExpiresAt = expiresAt,
        };

        await UnitOfWork.ServiceAccounts.AddCredential(credential, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse<ApiCredentialCreatedViewModel>.Success(new ApiCredentialCreatedViewModel
        {
            Id = credential.Id,
            Name = credential.Name,
            Token = token.Token,
            ExpiresAt = credential.ExpiresAt,
            Scopes = credential.Scopes,
        });
    }
}
