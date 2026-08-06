using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Mapping;

public sealed record ImportRowDiagnostic
{
    public int RowNumber { get; init; }

    public string? ColumnName { get; init; }

    public required ImportDiagnosticSeverity Severity { get; init; }

    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Value { get; init; }
}

public sealed record ImportNewEntity
{
    public required string EntityType { get; init; }

    public required string Name { get; init; }
}

public sealed record ImportRowPreview
{
    public int RowNumber { get; init; }

    public required ImportRowAction Action { get; init; }

    public string? MatchedRef { get; init; }

    public Dictionary<string, string?> Resolved { get; init; } = [];
}

public sealed record ImportPreviewResult
{
    public const int MaxDiagnostics = 200;

    public const int MaxSampleRows = 20;

    public int TotalRows { get; init; }

    public int WillCreate { get; init; }

    public int WillUpdate { get; init; }

    public int WillSkip { get; init; }

    public int WillError { get; init; }

    public bool IsExtrapolated { get; init; }

    public List<ImportRowDiagnostic> Diagnostics { get; init; } = [];

    public List<ImportNewEntity> NewEntities { get; init; } = [];

    public List<string> UsersToInvite { get; init; } = [];

    public List<ImportRowPreview> SampleRows { get; init; } = [];
}

public static class ImportDiagnosticCodes
{
    public const string UnresolvedUser = "unresolved_user";
    public const string UnresolvedStatus = "unresolved_status";
    public const string UnresolvedProject = "unresolved_project";
    public const string UnresolvedSprint = "unresolved_sprint";
    public const string InvalidDate = "invalid_date";
    public const string InvalidNumber = "invalid_number";
    public const string InvalidPriority = "invalid_priority";
    public const string InvalidSchedule = "invalid_schedule";
    public const string MissingRequiredField = "missing_required_field";
    public const string RowSkipped = "row_skipped";
}
