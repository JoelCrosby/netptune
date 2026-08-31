using Mediator;

using Netptune.Core.Preferences;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.UserPreferences.Commands;

public sealed record DeleteUserPreferenceValueCommand(
    string Key,
    string Scope) : IRequest<ClientResponse<ResolvedPreferenceValue>>;

public sealed class DeleteUserPreferenceValueCommandHandler
    : IRequestHandler<DeleteUserPreferenceValueCommand, ClientResponse<ResolvedPreferenceValue>>
{
    private readonly IIdentityService Identity;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IPreferenceDefinitionRegistry Registry;
    private readonly PreferenceValueResolver Resolver;

    public DeleteUserPreferenceValueCommandHandler(
        IIdentityService identity,
        INetptuneUnitOfWork unitOfWork,
        IPreferenceDefinitionRegistry registry,
        PreferenceValueResolver resolver)
    {
        Identity = identity;
        UnitOfWork = unitOfWork;
        Registry = registry;
        Resolver = resolver;
    }

    public async ValueTask<ClientResponse<ResolvedPreferenceValue>> Handle(
        DeleteUserPreferenceValueCommand request,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Resolver.TryGetWorkspaceId();

        var definition = Registry.Find(request.Key);
        if (definition is null || !definition.AllowedScopes.Contains(request.Scope))
        {
            return ClientResponse<ResolvedPreferenceValue>.Failed("Invalid preference key or scope.");
        }

        if (request.Scope == PreferenceScopes.Workspace && workspaceId is null)
        {
            return ClientResponse<ResolvedPreferenceValue>.Failed("Invalid preference key or scope.");
        }

        var scopedWorkspaceId = request.Scope == PreferenceScopes.Workspace ? workspaceId : null;
        var entity = await UnitOfWork.UserPreferences.GetScopedValue(
            userId,
            request.Key,
            scopedWorkspaceId,
            cancellationToken);

        if (entity is not null)
        {
            await UnitOfWork.UserPreferences.DeletePermanent(entity.Id, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        }

        var result = await Resolver.Resolve(userId, workspaceId, definition, cancellationToken);

        return ClientResponse<ResolvedPreferenceValue>.Success(result);
    }
}
