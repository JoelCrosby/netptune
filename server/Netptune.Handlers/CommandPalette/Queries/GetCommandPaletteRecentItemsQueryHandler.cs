using Mediator;

using Netptune.Core.Preferences;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.CommandPalette.Queries;

public sealed record GetCommandPaletteRecentItemsQuery : IRequest<ClientResponse<CommandPaletteRecentItemsResponse>>;

public sealed class GetCommandPaletteRecentItemsQueryHandler
    : IRequestHandler<GetCommandPaletteRecentItemsQuery, ClientResponse<CommandPaletteRecentItemsResponse>>
{
    private const int MaxRecentItems = 10;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IPreferenceDefinitionRegistry Registry;

    public GetCommandPaletteRecentItemsQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IPreferenceDefinitionRegistry registry)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Registry = registry;
    }

    public async ValueTask<ClientResponse<CommandPaletteRecentItemsResponse>> Handle(
        GetCommandPaletteRecentItemsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await GetRequiredWorkspaceId();
        if (workspaceId is null)
        {
            return ClientResponse<CommandPaletteRecentItemsResponse>.Failed("A workspace is required.");
        }

        var userId = Identity.GetCurrentUserId();
        var scope = await ResolveRecentItemsScope(userId, workspaceId.Value, cancellationToken);
        var items = await UnitOfWork.CommandPaletteRecentItems.GetRecentItems(
            userId,
            workspaceId.Value,
            scope,
            MaxRecentItems,
            cancellationToken);

        return ClientResponse<CommandPaletteRecentItemsResponse>.Success(
            new CommandPaletteRecentItemsResponse
            {
                Scope = scope,
                Items = items,
            });
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
