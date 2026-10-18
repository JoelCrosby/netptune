using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;

namespace Netptune.Core.Services.Realtime;

// HTTP routes broadcast their own changes, so only changes made in the background need a push from the ledger.
public static class BackgroundChangeScopes
{
    private static readonly Dictionary<string, string> ScopesByEntityType = new(StringComparer.Ordinal)
    {
        [EventEntityTypes.From(EntityType.Task)] = WorkspaceEventScopes.Task,
        [EventEntityTypes.From(EntityType.Sprint)] = WorkspaceEventScopes.Sprint,
        [EventEntityTypes.From(EntityType.Project)] = WorkspaceEventScopes.Project,
        [EventEntityTypes.From(EntityType.Board)] = WorkspaceEventScopes.Board,
        [EventEntityTypes.From(EntityType.Comment)] = WorkspaceEventScopes.Comment,
        [EventEntityTypes.From(EntityType.Tag)] = WorkspaceEventScopes.Tag,
        [EventEntityTypes.From(EntityType.Status)] = WorkspaceEventScopes.Status,
    };

    private static readonly HashSet<string> ChangedEntityRoles =
    [
        EventReferenceRoles.Member,
        EventReferenceRoles.Source,
        EventReferenceRoles.Target,
    ];

    public static IReadOnlyList<string> For(CanonicalEventEnvelope envelope)
    {
        var isBackgroundChange = envelope.OriginType is EventOriginType.Automation or EventOriginType.System;
        var changesEntities = !EventKeys.IsSourceControlActivity(envelope.EventKey);
        var isRelevant = isBackgroundChange && changesEntities && envelope.WorkspaceId.HasValue;

        if (!isRelevant)
        {
            return [];
        }

        var changedEntityTypes = envelope.References
            .Where(reference => ChangedEntityRoles.Contains(reference.Role))
            .Select(reference => reference.EntityType)
            .Prepend(envelope.SubjectType);

        return changedEntityTypes
            .Select(entityType => entityType is null ? null : ScopesByEntityType.GetValueOrDefault(entityType))
            .OfType<string>()
            .Distinct()
            .ToList();
    }
}
