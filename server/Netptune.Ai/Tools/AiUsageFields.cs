using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Usage;

namespace Netptune.Ai.Tools;

// What still points at a status or relation type does not stop it being deleted, but it is left pointing at
// nothing afterwards, so the review names it.
internal static class AiUsageFields
{
    private static readonly Dictionary<UsageReferenceKind, string> FieldNames = new()
    {
        [UsageReferenceKind.Project] = "projectsUsingIt",
        [UsageReferenceKind.BoardGroup] = "boardGroupsUsingIt",
        [UsageReferenceKind.AutomationRule] = "automationRulesUsingIt",
    };

    public static void Add(List<AiChangeField> fields, EntityUsageViewModel usage)
    {
        foreach (var group in usage.References)
        {
            var name = FieldNames.GetValueOrDefault(group.Kind, "usedBy");

            fields.Add(AiChangeFields.Text(name, null, Join(group)));
        }
    }

    public static string Describe(EntityUsageViewModel usage)
    {
        if (usage.References.Count == 0)
        {
            return string.Empty;
        }

        var groups = usage.References.Select(group => $"{group.Kind}: {Join(group)}");

        return $" It is still referenced by {string.Join("; ", groups)}, which will be left pointing at nothing.";
    }

    private static string Join(UsageReferenceGroupViewModel group)
    {
        var names = group.Items.Select(item => item.Context is null ? item.Name : $"{item.Name} ({item.Context})");

        return string.Join(", ", names);
    }
}
