using System.Text.Json;

using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.UserPreferences.Commands;

public sealed record SetUserPreferenceValueCommand : IRequest<ClientResponse<ResolvedPreferenceValue>>
{
    public required string Key { get; init; }

    public required string Scope { get; init; }

    public required JsonElement Value { get; init; }
}

public sealed class SetUserPreferenceValueCommandHandler
    : IRequestHandler<SetUserPreferenceValueCommand, ClientResponse<ResolvedPreferenceValue>>
{
    private readonly IIdentityService Identity;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IPreferenceDefinitionRegistry Registry;

    public SetUserPreferenceValueCommandHandler(
        IIdentityService identity,
        INetptuneUnitOfWork unitOfWork,
        IPreferenceDefinitionRegistry registry)
    {
        Identity = identity;
        UnitOfWork = unitOfWork;
        Registry = registry;
    }

    public async ValueTask<ClientResponse<ResolvedPreferenceValue>> Handle(
        SetUserPreferenceValueCommand request,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await TryGetWorkspaceId();

        var definition = Registry.Find(request.Key);
        if (definition is null || !Validate(definition, request.Scope, request.Value, workspaceId))
        {
            return ClientResponse<ResolvedPreferenceValue>.Failed("Invalid preference key, scope, or value.");
        }

        var scopedWorkspaceId = request.Scope == PreferenceScopes.Workspace ? workspaceId : null;
        var entity = await UnitOfWork.UserPreferences.GetScopedValue(
            userId,
            request.Key,
            scopedWorkspaceId,
            cancellationToken);

        var now = DateTime.UtcNow;

        if (entity is null)
        {
            await UnitOfWork.UserPreferences.AddAsync(new UserPreferenceValue
            {
                UserId = userId,
                WorkspaceId = scopedWorkspaceId,
                Key = request.Key,
                Value = JsonSerializer.SerializeToDocument(request.Value),
                CreatedAt = now,
                UpdatedAt = now,
            }, cancellationToken);
        }
        else
        {
            entity.Value = JsonSerializer.SerializeToDocument(request.Value);
            entity.UpdatedAt = now;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

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

    private static bool Validate(
        PreferenceDefinition definition,
        string scope,
        JsonElement value,
        int? workspaceId)
    {
        if (!definition.AllowedScopes.Contains(scope)) return false;
        if (scope == PreferenceScopes.Workspace && workspaceId is null) return false;

        if (definition.ValueType == "number-array")
        {
            return IsNumberArray(value);
        }

        if (definition.ValueType == "number-array-map")
        {
            return value.ValueKind == JsonValueKind.Object
                   && value.EnumerateObject().All(property => IsNumberArray(property.Value));
        }

        if (definition.ValueType == "string" && value.ValueKind != JsonValueKind.String) return false;

        if (definition.Options.Count == 0) return true;

        var stringValue = value.GetString();

        return definition.Options.Any(option =>
            string.Equals(option.Value, stringValue, StringComparison.Ordinal));
    }

    private static bool IsNumberArray(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Array
               && value.EnumerateArray().All(element => element.ValueKind == JsonValueKind.Number);
    }
}
