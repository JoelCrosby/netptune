using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Export;

public sealed record ExportDefinitionValidationResult
{
    public IReadOnlyList<string> Errors { get; init; } = [];

    public bool IsValid => Errors.Count == 0;
}

public static class ExportDefinitionValidator
{
    public static ExportDefinitionValidationResult Validate(ExportDefinitionModel? definition)
    {
        if (definition is null)
        {
            return Failed("An export definition is required.");
        }

        var errors = new List<string>();
        var isWorkspaceArchive = IsWorkspaceRecordType(definition.RecordType);
        var recordType = TransferFieldCatalog.FindRecordType(definition.RecordType);

        var isStandaloneExportable = recordType?.IsStandaloneExportable == true;

        if (!isWorkspaceArchive && !isStandaloneExportable)
        {
            return Failed($"'{definition.RecordType}' cannot be exported on its own. Export the whole workspace instead.");
        }

        ValidateFormat(definition, isWorkspaceArchive, errors);
        ValidateFields(definition, isWorkspaceArchive ? null : recordType, errors);
        ValidateOptions(definition, errors);

        return new ExportDefinitionValidationResult { Errors = errors };
    }

    public static IReadOnlyList<TransferField> ResolveFields(ExportDefinitionModel definition)
    {
        var recordType = TransferFieldCatalog.FindRecordType(definition.RecordType);

        if (recordType is null)
        {
            return [];
        }

        if (definition.Fields.Count == 0)
        {
            return recordType.Fields.Where(field => field.IsExportedByDefault).ToList();
        }

        return definition.Fields
            .Select(TransferFieldCatalog.FindField)
            .Where(field => field is not null)
            .Select(field => field!)
            .ToList();
    }

    public static bool IsWorkspaceRecordType(string recordType)
    {
        return string.Equals(recordType, ExportDefinitionModel.WorkspaceRecordType, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateFormat(ExportDefinitionModel definition, bool isWorkspaceArchive, List<string> errors)
    {
        var isArchiveFormat = definition.Format == ExportFormat.Archive;

        if (isWorkspaceArchive && !isArchiveFormat)
        {
            errors.Add("A whole-workspace export must use the archive format.");
        }

        if (!isWorkspaceArchive && isArchiveFormat)
        {
            errors.Add("The archive format only applies to a whole-workspace export.");
        }
    }

    private static void ValidateFields(ExportDefinitionModel definition, TransferRecordType? recordType, List<string> errors)
    {
        if (recordType is null)
        {
            if (definition.Fields.Count > 0)
            {
                errors.Add("A whole-workspace export cannot select individual fields.");
            }

            return;
        }

        var known = recordType.Fields.Select(field => field.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fieldKey in definition.Fields)
        {
            if (!known.Contains(fieldKey))
            {
                errors.Add($"'{fieldKey}' is not a field of '{recordType.Key}'.");
                continue;
            }

            var isDuplicate = !seen.Add(fieldKey);

            if (isDuplicate)
            {
                errors.Add($"'{fieldKey}' is selected more than once.");
            }
        }
    }

    private static void ValidateOptions(ExportDefinitionModel definition, List<string> errors)
    {
        var options = definition.Options;
        var isDelimited = definition.Format is ExportFormat.Csv or ExportFormat.Tsv;
        var isUnusableDelimiter = options.Delimiter is '\0' or '\r' or '\n' or '"';

        if (isDelimited && isUnusableDelimiter)
        {
            errors.Add("The delimiter cannot be a quote or a line break.");
        }

        if (string.IsNullOrWhiteSpace(options.DateFormat))
        {
            errors.Add("A date format is required.");
        }

        if (string.IsNullOrEmpty(options.CollectionSeparator))
        {
            errors.Add("A separator for multi-value fields is required.");
        }

        var timeZoneResolved = TryResolveTimeZone(options.TimeZoneId);

        if (!timeZoneResolved)
        {
            errors.Add($"'{options.TimeZoneId}' is not a known time zone.");
        }

        var isArchiveFormat = definition.Format == ExportFormat.Archive;
        var usesArchiveOptions = options.IncludeHistory || options.IncludeFiles || options.IncludeMembers;

        if (!isArchiveFormat && usesArchiveOptions)
        {
            errors.Add("History, file and member options only apply to an archive export.");
        }
    }

    private static ExportDefinitionValidationResult Failed(string error)
    {
        return new ExportDefinitionValidationResult { Errors = [error] };
    }

    private static bool TryResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
    }
}
