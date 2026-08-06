namespace Netptune.Transfer.Mapping;

public sealed record ImportMappingValidationResult
{
    public IReadOnlyList<string> Errors { get; init; } = [];

    public bool IsValid => Errors.Count == 0;
}

public static class ImportMappingValidator
{
    public static ImportMappingValidationResult Validate(ImportMappingModel? mapping, int columnCount)
    {
        if (mapping is null)
        {
            return Failed("A mapping is required.");
        }

        var recordType = TransferFieldCatalog.FindRecordType(mapping.RecordType);

        if (recordType is null)
        {
            return Failed($"'{mapping.RecordType}' is not an importable record type.");
        }

        var errors = new List<string>();
        var bound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var binding in mapping.Bindings)
        {
            ValidateBinding(binding, recordType, columnCount, bound, errors);
        }

        ValidateRequiredFields(recordType, bound, errors);
        ValidateDedupe(mapping, recordType, errors);

        return new ImportMappingValidationResult { Errors = errors };
    }

    private static void ValidateBinding(
        ImportFieldBinding binding,
        TransferRecordType recordType,
        int columnCount,
        HashSet<string> bound,
        List<string> errors)
    {
        var field = recordType.Fields.FirstOrDefault(candidate =>
            string.Equals(candidate.Key, binding.FieldKey, StringComparison.OrdinalIgnoreCase));

        if (field is null)
        {
            errors.Add($"'{binding.FieldKey}' is not a field of '{recordType.Key}'.");

            return;
        }

        var isDuplicate = !bound.Add(field.Key);

        if (isDuplicate)
        {
            errors.Add($"'{field.Key}' is bound more than once.");
        }

        var hasColumn = binding.ColumnIndex is not null;
        var hasConstant = binding.Constant is not null;

        if (hasColumn && hasConstant)
        {
            errors.Add($"'{field.Key}' cannot take both a column and a constant.");
        }

        if (!hasColumn && !hasConstant)
        {
            errors.Add($"'{field.Key}' needs either a column or a constant.");
        }

        var isColumnInRange = binding.ColumnIndex is null || (binding.ColumnIndex >= 0 && binding.ColumnIndex < columnCount);

        if (!isColumnInRange)
        {
            errors.Add($"'{field.Key}' is bound to column {binding.ColumnIndex}, which the file does not have.");
        }

        var hasUnknownTransform = binding.Transforms.Any(transform => !Enum.IsDefined(transform.Kind));

        if (hasUnknownTransform)
        {
            errors.Add($"'{field.Key}' uses a transform that is not supported.");
        }
    }

    private static void ValidateRequiredFields(TransferRecordType recordType, HashSet<string> bound, List<string> errors)
    {
        var missing = recordType.Fields
            .Where(field => field.IsRequiredForImport)
            .Where(field => !bound.Contains(field.Key))
            .Select(field => field.Name)
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        errors.Add($"These fields have to be mapped: {string.Join(", ", missing)}.");
    }

    private static void ValidateDedupe(ImportMappingModel mapping, TransferRecordType recordType, List<string> errors)
    {
        if (mapping.Dedupe is null)
        {
            return;
        }

        var keyField = recordType.Fields.FirstOrDefault(field =>
            string.Equals(field.Key, mapping.Dedupe.KeyFieldKey, StringComparison.OrdinalIgnoreCase));

        if (keyField is null)
        {
            errors.Add($"'{mapping.Dedupe.KeyFieldKey}' cannot be used to match existing records.");

            return;
        }

        var isKeyBound = mapping.Bindings.Any(binding =>
            string.Equals(binding.FieldKey, keyField.Key, StringComparison.OrdinalIgnoreCase));

        if (!isKeyBound)
        {
            errors.Add($"'{keyField.Name}' matches existing records, so it has to be mapped.");
        }
    }

    private static ImportMappingValidationResult Failed(string error)
    {
        return new ImportMappingValidationResult { Errors = [error] };
    }
}
