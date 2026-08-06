using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Import;

public sealed record ImportTransform
{
    public required ImportTransformKind Kind { get; init; }

    public string? Argument { get; init; }
}

public sealed record ImportFieldBinding
{
    public required string FieldKey { get; init; }

    public int? ColumnIndex { get; init; }

    // Further columns folded into this binding. Jira repeats a header such as "Sprint" or "Labels"
    // once per value, so one logical collection field arrives as several columns.
    public List<int> AdditionalColumnIndexes { get; init; } = [];

    public string? Constant { get; init; }

    public List<ImportTransform> Transforms { get; init; } = [];

    public Dictionary<string, string> ValueMap { get; init; } = [];

    public double Confidence { get; init; }

    public ImportBindingOrigin Origin { get; init; }
}

public sealed record ImportDedupeModel
{
    public required string KeyFieldKey { get; init; }

    public ImportDedupeAction Action { get; init; }
}

public sealed record ImportUnknownPolicyModel
{
    public ImportUnknownPolicy Statuses { get; init; } = ImportUnknownPolicy.UseDefault;

    public ImportUnknownPolicy Tags { get; init; } = ImportUnknownPolicy.Create;

    public ImportUnknownPolicy Users { get; init; } = ImportUnknownPolicy.Skip;

    public ImportUnknownPolicy BoardGroups { get; init; } = ImportUnknownPolicy.Create;

    public ImportUnknownPolicy Sprints { get; init; } = ImportUnknownPolicy.Skip;

    public ImportUnknownPolicy Projects { get; init; } = ImportUnknownPolicy.Fail;
}

public sealed record ImportDefaultsModel
{
    public string? StatusKey { get; init; }

    public string? BoardGroupName { get; init; }
}

public sealed record ImportMappingModel
{
    public required string RecordType { get; init; }

    public List<ImportFieldBinding> Bindings { get; init; } = [];

    public ImportDedupeModel? Dedupe { get; init; }

    public ImportUnknownPolicyModel UnknownPolicy { get; init; } = new();

    public ImportDefaultsModel Defaults { get; init; } = new();
}
