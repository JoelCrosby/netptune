using Netptune.Core.Enums;

namespace Netptune.Core.Onboarding.Templates;

public sealed record WorkspaceSetupTemplateDefinition
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public bool IsRecommended { get; init; }

    public IReadOnlyList<SetupTemplateStatusDefinition> Statuses { get; init; } = [];

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<SetupTemplateRelationDefinition> RelationTypes { get; init; } = [];

    public IReadOnlyList<SetupTemplateBoardGroupDefinition> BoardGroups { get; init; } = [];
}

public sealed record SetupTemplateStatusDefinition
{
    public required string Name { get; init; }

    public required string Key { get; init; }

    public required string Color { get; init; }

    public StatusCategory Category { get; init; }
}

public sealed record SetupTemplateRelationDefinition
{
    public required string Name { get; init; }

    public required string InverseName { get; init; }

    public required string Key { get; init; }

    public required string Color { get; init; }

    public RelationCategory Category { get; init; }
}

public sealed record SetupTemplateBoardGroupDefinition
{
    public required string Name { get; init; }

    public string? StatusKey { get; init; }

    public StatusCategory? FallbackStatusCategory { get; init; }
}

public sealed record WorkspaceSetupTemplateViewModel
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public bool IsRecommended { get; init; }

    public IReadOnlyList<SetupTemplateStatusViewModel> Statuses { get; init; } = [];

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<SetupTemplateRelationViewModel> RelationTypes { get; init; } = [];

    public IReadOnlyList<string> BoardGroups { get; init; } = [];
}

public sealed record SetupTemplateStatusViewModel
{
    public required string Name { get; init; }

    public required string Color { get; init; }

    public StatusCategory Category { get; init; }
}

public sealed record SetupTemplateRelationViewModel
{
    public required string Name { get; init; }

    public required string InverseName { get; init; }

    public required string Color { get; init; }

    public RelationCategory Category { get; init; }
}
