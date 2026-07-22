using Netptune.Core.Colors;
using Netptune.Core.Enums;

namespace Netptune.Core.Onboarding.Templates;

public static class WorkspaceSetupTemplateCatalog
{
    public const string DefaultKey = "basic";
    public const string NewStatusKey = "new";

    private static readonly IReadOnlyList<SetupTemplateRelationDefinition> StandardRelations =
    [
        Relation("Parent of", "Child of", "parent-of", NamedColors.Violet, RelationCategory.Hierarchy),
        Relation("Blocks", "Is Blocked By", "blocks", NamedColors.Red, RelationCategory.Dependency),
        Relation("Relates To", "Relates To", "relates-to", NamedColors.Slate, RelationCategory.Related),
        Relation("Duplicates", "Is Duplicated By", "duplicates", NamedColors.Amber, RelationCategory.Duplicate),
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
                Status("New", NewStatusKey, NamedColors.Slate, StatusCategory.Todo),
                Status("In Progress", "in-progress", NamedColors.Blue, StatusCategory.Active),
                Status("On Hold", "on-hold", NamedColors.Amber, StatusCategory.Backlog),
                Status("Un-assigned", "un-assigned", NamedColors.Violet, StatusCategory.Backlog),
                Status("Blocked", "blocked", NamedColors.Red, StatusCategory.Inactive),
                Status("Inactive", "inactive", NamedColors.Slate, StatusCategory.Inactive),
                Status("Complete", "complete", NamedColors.Green, StatusCategory.Done),
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
                Status("New", NewStatusKey, NamedColors.Slate, StatusCategory.Todo),
                Status("Backlog", "backlog", NamedColors.Slate, StatusCategory.Backlog),
                Status("Ready", "ready", NamedColors.Violet, StatusCategory.Todo),
                Status("In Progress", "in-progress", NamedColors.Blue, StatusCategory.Active),
                Status("In Review", "in-review", NamedColors.Teal, StatusCategory.Active),
                Status("Blocked", "blocked", NamedColors.Red, StatusCategory.Inactive),
                Status("Done", "done", NamedColors.Green, StatusCategory.Done),
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
                Status("New", NewStatusKey, NamedColors.Slate, StatusCategory.Todo),
                Status("Ideas", "ideas", NamedColors.Violet, StatusCategory.Backlog),
                Status("Planned", "planned", NamedColors.Slate, StatusCategory.Todo),
                Status("Drafting", "drafting", NamedColors.Blue, StatusCategory.Active),
                Status("In Review", "in-review", NamedColors.Teal, StatusCategory.Active),
                Status("Published", "published", NamedColors.Green, StatusCategory.Done),
                Status("Archived", "archived", NamedColors.Slate, StatusCategory.Inactive),
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
                Status("New", NewStatusKey, NamedColors.Slate, StatusCategory.Todo),
                Status("In Progress", "in-progress", NamedColors.Blue, StatusCategory.Active),
                Status("Done", "done", NamedColors.Green, StatusCategory.Done),
            ],
            RelationTypes =
            [
                Relation("Relates To", "Relates To", "relates-to", NamedColors.Slate, RelationCategory.Related),
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
