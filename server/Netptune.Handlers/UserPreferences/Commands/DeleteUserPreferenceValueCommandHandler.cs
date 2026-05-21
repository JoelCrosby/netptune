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

    public DeleteUserPreferenceValueCommandHandler(
        IIdentityService identity,
        INetptuneUnitOfWork unitOfWork,
        IPreferenceDefinitionRegistry registry)
    {
        Identity = identity;
        UnitOfWork = unitOfWork;
        Registry = registry;
    }

    public async ValueTask<ClientResponse<ResolvedPreferenceValue>> Handle(
        DeleteUserPreferenceValueCommand request,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await TryGetWorkspaceId();

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

        var result = await Resolve(userId, workspaceId, definition, cancellationToken);

        return ClientResponse<ResolvedPreferenceValue>.Success(result);
    }

    private async Task<int?> TryGetWorkspaceId()
    {
        return Identity.TryGetWorkspaceKey() is null ? null : await Identity.GetWorkspaceId();
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
}
