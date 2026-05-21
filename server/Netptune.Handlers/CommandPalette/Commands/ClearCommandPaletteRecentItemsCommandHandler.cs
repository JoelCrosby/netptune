using Mediator;

using Netptune.Core.Preferences;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.CommandPalette.Queries;

namespace Netptune.Handlers.CommandPalette.Commands;

public sealed record ClearCommandPaletteRecentItemsCommand : IRequest<ClientResponse<CommandPaletteRecentItemsResponse>>;

public sealed class ClearCommandPaletteRecentItemsCommandHandler
    : IRequestHandler<ClearCommandPaletteRecentItemsCommand, ClientResponse<CommandPaletteRecentItemsResponse>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IPreferenceDefinitionRegistry Registry;
    private readonly IMediator Mediator;

    public ClearCommandPaletteRecentItemsCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IPreferenceDefinitionRegistry registry,
        IMediator mediator)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Registry = registry;
        Mediator = mediator;
    }

    public async ValueTask<ClientResponse<CommandPaletteRecentItemsResponse>> Handle(
        ClearCommandPaletteRecentItemsCommand request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await GetRequiredWorkspaceId();
        if (workspaceId is null)
        {
            return ClientResponse<CommandPaletteRecentItemsResponse>.Failed("A workspace is required.");
        }

        var userId = Identity.GetCurrentUserId();
        var scope = await ResolveRecentItemsScope(userId, workspaceId.Value, cancellationToken);
        var items = await UnitOfWork.CommandPaletteRecentItems.GetClearableItems(
            userId,
            workspaceId.Value,
            scope,
            cancellationToken);

        await UnitOfWork.CommandPaletteRecentItems.DeletePermanent(items, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return await Mediator.Send(new GetCommandPaletteRecentItemsQuery(), cancellationToken);
    }

    private async Task<int?> GetRequiredWorkspaceId()
    {
        return Identity.TryGetWorkspaceKey() is null ? null : await Identity.GetWorkspaceId();
    }

    private async Task<string> ResolveRecentItemsScope(
        string userId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var definition = Registry.Find(PreferenceKeys.CommandPaletteRecentItemsScope)!;
        var values = await UnitOfWork.UserPreferences.GetValues(
            userId,
            definition.Key,
            workspaceId,
            cancellationToken);

        var globalValue = values.FirstOrDefault(value => value.WorkspaceId is null)?.Value.RootElement.Clone();
        var workspaceValue = values.FirstOrDefault(value => value.WorkspaceId == workspaceId)?.Value.RootElement.Clone();
        var effectiveValue = workspaceValue ?? globalValue ?? definition.DefaultValue;

        return effectiveValue.GetString() ?? "workspace";
    }
}
