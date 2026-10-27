using System.Text.Json;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public static class AiPendingReference
{
    public static string? Read(JsonElement arguments, string name)
    {
        var value = AiToolSchema.GetString(arguments, name)?.Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static AiChangeDraft? Find(IAiChangeSetBuilder changeSet, string refKey, string entityType)
    {
        return changeSet.Changes.FirstOrDefault(change =>
            string.Equals(change.RefKey, refKey, StringComparison.Ordinal) &&
            string.Equals(change.EntityType, entityType, StringComparison.Ordinal));
    }

    public static string ProposedName(AiChangeDraft draft)
    {
        var name = draft.Fields.FirstOrDefault(field => string.Equals(field.Name, "name", StringComparison.Ordinal));

        return name?.After ?? draft.Summary;
    }

    public static string? Conflict(JsonElement arguments, string idName, string refName)
    {
        var hasId = AiToolSchema.GetInt(arguments, idName).HasValue;
        var hasRef = Read(arguments, refName) is not null;

        return hasId && hasRef ? $"Pass {idName} or {refName}, not both." : null;
    }

    public static string Missing(string refKey, string entityType)
    {
        return $"“{refKey}” does not match a {entityType} proposed in this change set.";
    }
}
