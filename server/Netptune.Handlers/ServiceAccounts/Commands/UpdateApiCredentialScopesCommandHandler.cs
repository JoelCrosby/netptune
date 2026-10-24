using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Localization;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ServiceAccounts;

namespace Netptune.Handlers.ServiceAccounts.Commands;

public sealed record UpdateApiCredentialScopesCommand(
    int ServiceAccountId,
    Guid CredentialId,
    UpdateApiCredentialScopesRequest Request) : IRequest<ClientResponse<ApiCredentialViewModel>>;

public sealed class UpdateApiCredentialScopesCommandHandler
    : IRequestHandler<UpdateApiCredentialScopesCommand, ClientResponse<ApiCredentialViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public UpdateApiCredentialScopesCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<ApiCredentialViewModel>> Handle(
        UpdateApiCredentialScopesCommand command,
        CancellationToken cancellationToken)
    {
        var currentUser = await Identity.GetCurrentUser();

        if (currentUser.UserType != AppUserType.User)
        {
            return ClientResponse<ApiCredentialViewModel>.Forbidden;
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var credential = await UnitOfWork.ServiceAccounts.GetCredentialForManagement(
            command.CredentialId,
            command.ServiceAccountId,
            workspaceId,
            cancellationToken);

        if (credential is null)
        {
            return ClientResponse<ApiCredentialViewModel>.NotFound;
        }

        if (credential.ServiceAccount.DisabledAt.HasValue)
        {
            return ClientResponse<ApiCredentialViewModel>.Failed(
                AppText.ErrorDeletedServiceAccountsCannotChangeCredentials);
        }

        if (credential.RevokedAt.HasValue)
        {
            return ClientResponse<ApiCredentialViewModel>.Failed(AppText.ErrorRevokedCredentialsCannotChangeScopes);
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var membership = await UnitOfWork.WorkspaceUsers.GetUserPermissions(
            credential.ServiceAccount.UserId,
            workspaceKey,
            cancellationToken: cancellationToken);

        if (membership is null)
        {
            return ClientResponse<ApiCredentialViewModel>.Failed(AppText.ErrorServiceAccountDoesNotWorkspaceMembership);
        }

        var scopes = command.Request.Scopes
            .Select(scope => scope.Trim())
            .Where(scope => scope.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(scope => scope)
            .ToList();

        if (scopes.Count == 0)
        {
            return ClientResponse<ApiCredentialViewModel>.Failed(AppText.ErrorCredentialNeedsAtLeastOneScope);
        }

        var exceedsAccount = scopes.Any(scope => !NetptunePermissions.All.Contains(scope)
                                                 || !membership.Permissions.Contains(scope));

        if (exceedsAccount)
        {
            return ClientResponse<ApiCredentialViewModel>.Failed(
                AppText.ErrorCredentialScopesMustSubsetServiceAccountPermissions);
        }

        credential.Scopes.Clear();
        credential.Scopes.AddRange(scopes);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse<ApiCredentialViewModel>.Success(new ApiCredentialViewModel
        {
            Id = credential.Id,
            Name = credential.Name,
            TokenPrefix = credential.TokenPrefix,
            CreatedAt = credential.CreatedAt,
            ExpiresAt = credential.ExpiresAt,
            RevokedAt = credential.RevokedAt,
            LastUsedAt = credential.LastUsedAt,
            Scopes = scopes,
        });
    }
}
