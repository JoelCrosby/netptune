using Netptune.Core.Enums;

namespace Netptune.Core.Onboarding.Templates;

public static class WorkspaceSetupTemplateCatalog
{
    public const string DefaultKey = "basic";
    public const string NewStatusKey = "new";

    private static readonly IReadOnlyList<SetupTemplateRelationDefinition> StandardRelations =
    [
        Relation("Parent of", "Child of", "parent-of", "#8b5cf6", RelationCategory.Hierarchy),
        Relation("Blocks", "Is Blocked By", "blocks", "#b81414", RelationCategory.Dependency),
        Relation("Relates To", "Relates To", "relates-to", "#6b7280", RelationCategory.Related),
        Relation("Duplicates", "Is Duplicated By", "duplicates", "#f59e0b", RelationCategory.Duplicate),
    ];

    public static readonly IReadOnlyList<WorkspaceSetupTemplateDefinition> All =
    [
        new()
        {
            Key = DefaultKey,
            Name = "Basic",
            Description = "A flexible workflow for small teams and general project work.",
            Statuses =
            [
                Status("New", NewStatusKey, "#6b7280", StatusCategory.Todo),
                Status("In Progress", "in-progress", "#2563eb", StatusCategory.Active),
                Status("On Hold", "on-hold", "#f59e0b", StatusCategory.Backlog),
                Status("Un-assigned", "un-assigned", "#8b5cf6", StatusCategory.Backlog),
                Status("Blocked", "blocked", "#b81414", StatusCategory.Inactive),
                Status("Inactive", "inactive", "#64748b", StatusCategory.Inactive),
                Status("Complete", "complete", "#16a34a", StatusCategory.Done),
            ],
            Tags = ["Feature", "Bug", "Improvement", "Documentation"],
            RelationTypes = StandardRelations,
            BoardGroups =
            [
                BoardGroup("Backlog", "on-hold", StatusCategory.Backlog),
                BoardGroup("Todo", NewStatusKey, StatusCategory.Todo),
                BoardGroup("Done", "complete", StatusCategory.Done),
            ],
        },
        new()
        {
            Key = "software",
            Name = "Software delivery",
            Description = "A delivery workflow with review, blockers, and engineering-focused tags.",
            IsRecommended = true,
            Statuses =
            [
                Status("New", NewStatusKey, "#6b7280", StatusCategory.Todo),
                Status("Backlog", "backlog", "#64748b", StatusCategory.Backlog),
                Status("Ready", "ready", "#8b5cf6", StatusCategory.Todo),
                Status("In Progress", "in-progress", "#2563eb", StatusCategory.Active),
                Status("In Review", "in-review", "#0d9488", StatusCategory.Active),
                Status("Blocked", "blocked", "#b81414", StatusCategory.Inactive),
                Status("Done", "done", "#16a34a", StatusCategory.Done),
            ],
            Tags = ["Feature", "Bug", "Tech Debt", "Documentation", "Security"],
            RelationTypes = StandardRelations,
            BoardGroups =
            [
                BoardGroup("Backlog", "backlog", StatusCategory.Backlog),
                BoardGroup("Ready", NewStatusKey, StatusCategory.Todo),
                BoardGroup("In Progress", "in-progress", StatusCategory.Active),
                BoardGroup("Review", "in-review", StatusCategory.Active),
                BoardGroup("Done", "done", StatusCategory.Done),
            ],
        },
        new()
        {
            Key = "content",
            Name = "Content workflow",
            Description = "An editorial workflow from ideas and drafting through review and publication.",
            Statuses =
            [
                Status("New", NewStatusKey, "#6b7280", StatusCategory.Todo),
                Status("Ideas", "ideas", "#8b5cf6", StatusCategory.Backlog),
                Status("Planned", "planned", "#64748b", StatusCategory.Todo),
                Status("Drafting", "drafting", "#2563eb", StatusCategory.Active),
                Status("In Review", "in-review", "#0d9488", StatusCategory.Active),
                Status("Published", "published", "#16a34a", StatusCategory.Done),
                Status("Archived", "archived", "#6b7280", StatusCategory.Inactive),
            ],
            Tags = ["Blog", "Documentation", "Social", "Campaign"],
            RelationTypes = StandardRelations,
            BoardGroups =
            [
                BoardGroup("Ideas", "ideas", StatusCategory.Backlog),
                BoardGroup("Planned", NewStatusKey, StatusCategory.Todo),
                BoardGroup("Drafting", "drafting", StatusCategory.Active),
                BoardGroup("Review", "in-review", StatusCategory.Active),
                BoardGroup("Published", "published", StatusCategory.Done),
            ],
        },
        new()
        {
            Key = "minimal",
            Name = "Minimal",
            Description = "Only the essentials: New, In Progress, and Done.",
            Statuses =
            [
                Status("New", NewStatusKey, "#6b7280", StatusCategory.Todo),
                Status("In Progress", "in-progress", "#2563eb", StatusCategory.Active),
                Status("Done", "done", "#16a34a", StatusCategory.Done),
            ],
            RelationTypes =
            [
                Relation("Relates To", "Relates To", "relates-to", "#6b7280", RelationCategory.Related),
            ],
            BoardGroups =
            [
                BoardGroup("Todo", NewStatusKey, StatusCategory.Todo),
                BoardGroup("In Progress", "in-progress", StatusCategory.Active),
                BoardGroup("Done", "done", StatusCategory.Done),
            ],
        },
    ];

    public static WorkspaceSetupTemplateDefinition? Find(string? key)
    {
        var resolvedKey = string.IsNullOrWhiteSpace(key) ? DefaultKey : key.Trim();

        return All.FirstOrDefault(template =>
            string.Equals(template.Key, resolvedKey, StringComparison.OrdinalIgnoreCase));
    }

    public static WorkspaceSetupTemplateViewModel ToViewModel(WorkspaceSetupTemplateDefinition template)
    {
        return new()
        {
            Key = template.Key,
            Name = template.Name,
            Description = template.Description,
            IsRecommended = template.IsRecommended,
            Statuses = template.Statuses
                .Select(status => new SetupTemplateStatusViewModel
                {
                    Name = status.Name,
                    Color = status.Color,
                    Category = status.Category,
                })
                .ToList(),
            Tags = template.Tags.ToList(),
            RelationTypes = template.RelationTypes
                .Select(relation => new SetupTemplateRelationViewModel
                {
                    Name = relation.Name,
                    InverseName = relation.InverseName,
                    Color = relation.Color,
                    Category = relation.Category,
                })
                .ToList(),
            BoardGroups = template.BoardGroups.Select(group => group.Name).ToList(),
        };
    }

    private static SetupTemplateStatusDefinition Status(
        string name,
        string key,
        string color,
        StatusCategory category)
    {
        return new()
        {
            Name = name,
            Key = key,
            Color = color,
            Category = category,
        };
    }

    private static SetupTemplateRelationDefinition Relation(
        string name,
        string inverseName,
        string key,
        string color,
        RelationCategory category)
    {
        return new()
        {
            Name = name,
            InverseName = inverseName,
            Key = key,
            Color = color,
            Category = category,
        };
    }

    private static SetupTemplateBoardGroupDefinition BoardGroup(
        string name,
        string statusKey,
        StatusCategory fallbackStatusCategory)
    {
        return new()
        {
            Name = name,
            StatusKey = statusKey,
            FallbackStatusCategory = fallbackStatusCategory,
        };
    }
}
