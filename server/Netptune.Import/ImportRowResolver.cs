using Netptune.Core.Constants;
using Netptune.Core.Enums;
using Netptune.Core.Utilities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;

namespace Netptune.Import;

public sealed record ResolvedTaskRow
{
    public required int RowNumber { get; init; }

    // Whatever the source file calls its own identifier. Stored on the task as ExternalId, and matched
    // against both that and Netptune's own "{project key}-{number}" when looking for an existing task.
    public string? SourceId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? StatusValue { get; init; }

    public TaskPriority? Priority { get; init; }

    public decimal? EstimateValue { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? DueDate { get; init; }

    public string? BoardGroupValue { get; init; }

    public string? SprintValue { get; init; }

    public IReadOnlyList<string> AssigneeValues { get; init; } = [];

    public IReadOnlyList<string> TagValues { get; init; } = [];

    public List<ImportRowDiagnostic> Diagnostics { get; init; } = [];

    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == ImportDiagnosticSeverity.Error);
}

public sealed class ImportRowResolver
{
    private readonly Dictionary<string, ImportFieldBinding> BindingsByField;
    private readonly IReadOnlyList<string> ColumnNames;

    public ImportRowResolver(ImportMappingModel mapping, IReadOnlyList<string> columnNames)
    {
        // Indexed once rather than scanned per lookup: every field is resolved two or three times per
        // row, and a wide mapping over a large file turns that into millions of comparisons.
        BindingsByField = mapping.Bindings
            .GroupBy(binding => binding.FieldKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        ColumnNames = columnNames;
    }

    public ResolvedTaskRow Resolve(ImportRow row)
    {
        var diagnostics = new List<ImportRowDiagnostic>();
        var name = Text(row, TaskFieldKeys.Name);

        if (string.IsNullOrWhiteSpace(name))
        {
            diagnostics.Add(Diagnostic(row, TaskFieldKeys.Name, ImportDiagnosticSeverity.Error,
                ImportDiagnosticCodes.MissingRequiredField, "A task name is required.", name));
        }

        var startDate = Date(row, TaskFieldKeys.StartDate, diagnostics);
        var dueDate = Date(row, TaskFieldKeys.DueDate, diagnostics);
        var scheduleIsValid = startDate is null || dueDate is null || startDate <= dueDate;

        if (!scheduleIsValid)
        {
            diagnostics.Add(Diagnostic(row, TaskFieldKeys.DueDate, ImportDiagnosticSeverity.Error,
                ImportDiagnosticCodes.InvalidSchedule, "The due date falls before the start date.", dueDate?.ToString()));
        }

        return new ResolvedTaskRow
        {
            RowNumber = row.RowNumber,
            SourceId = Text(row, TaskFieldKeys.SystemId),
            Name = name ?? string.Empty,
            Description = Text(row, TaskFieldKeys.Description),
            StatusValue = Text(row, TaskFieldKeys.Status),
            Priority = Priority(row, diagnostics),
            EstimateValue = Number(row, TaskFieldKeys.EstimateValue, diagnostics),
            StartDate = startDate,
            DueDate = dueDate,
            BoardGroupValue = Text(row, TaskFieldKeys.BoardGroup),
            SprintValue = Text(row, TaskFieldKeys.Sprint),
            AssigneeValues = Collection(row, TaskFieldKeys.Assignees),
            TagValues = Collection(row, TaskFieldKeys.Tags),
            Diagnostics = diagnostics,
        };
    }

    private string? Text(ImportRow row, string fieldKey)
    {
        var binding = FindBinding(fieldKey);

        if (binding is null)
        {
            return null;
        }

        var raw = RawValue(row, binding);
        var transformed = ImportTransforms.Apply(raw, binding.Transforms);

        return MapValue(binding, transformed);
    }

    private IReadOnlyList<string> Collection(ImportRow row, string fieldKey)
    {
        var binding = FindBinding(fieldKey);

        if (binding is null)
        {
            return [];
        }

        var columns = ColumnIndexes(binding);
        var values = new List<string>();

        foreach (var raw in columns.Select(index => ValueAt(row, index, binding)))
        {
            var transformed = ImportTransforms.Apply(raw, binding.Transforms);

            values.AddRange(ImportTransforms.Split(transformed, binding.Transforms));
        }

        return values
            .Select(part => MapValue(binding, part) ?? part)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<int?> ColumnIndexes(ImportFieldBinding binding)
    {
        var indexes = new List<int?> { binding.ColumnIndex };

        indexes.AddRange(binding.AdditionalColumnIndexes.Select(index => (int?)index));

        return indexes;
    }

    private static string? ValueAt(ImportRow row, int? columnIndex, ImportFieldBinding binding)
    {
        if (columnIndex is null)
        {
            return binding.Constant;
        }

        if (columnIndex.Value >= row.Values.Count)
        {
            return null;
        }

        return row.Values[columnIndex.Value];
    }

    private DateOnly? Date(ImportRow row, string fieldKey, List<ImportRowDiagnostic> diagnostics)
    {
        var binding = FindBinding(fieldKey);

        if (binding is null)
        {
            return null;
        }

        var value = Text(row, fieldKey);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parsed = FieldValueParser.TryParseDate(value, out var date);

        if (!parsed)
        {
            diagnostics.Add(Diagnostic(row, fieldKey, ImportDiagnosticSeverity.Error,
                ImportDiagnosticCodes.InvalidDate, $"'{value}' is not a date.", value));

            return null;
        }

        return date;
    }

    private decimal? Number(ImportRow row, string fieldKey, List<ImportRowDiagnostic> diagnostics)
    {
        var value = Text(row, fieldKey);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parsed = FieldValueParser.TryParseDecimal(value, out var number);

        if (!parsed)
        {
            diagnostics.Add(Diagnostic(row, fieldKey, ImportDiagnosticSeverity.Warning,
                ImportDiagnosticCodes.InvalidNumber, $"'{value}' is not a number and was left empty.", value));

            return null;
        }

        return number;
    }

    private TaskPriority? Priority(ImportRow row, List<ImportRowDiagnostic> diagnostics)
    {
        var value = Text(row, TaskFieldKeys.Priority);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parsed = FieldValueParser.TryParseEnum<TaskPriority>(value, out var priority);

        if (!parsed)
        {
            diagnostics.Add(Diagnostic(row, TaskFieldKeys.Priority, ImportDiagnosticSeverity.Warning,
                ImportDiagnosticCodes.InvalidPriority, $"'{value}' is not a priority and was left empty.", value));

            return null;
        }

        return priority;
    }

    private ImportFieldBinding? FindBinding(string fieldKey)
    {
        return BindingsByField.GetValueOrDefault(fieldKey);
    }

    private static string? RawValue(ImportRow row, ImportFieldBinding binding)
    {
        if (binding.ColumnIndex is null)
        {
            return binding.Constant;
        }

        if (binding.ColumnIndex.Value >= row.Values.Count)
        {
            return null;
        }

        return row.Values[binding.ColumnIndex.Value];
    }

    private static string? MapValue(ImportFieldBinding binding, string? value)
    {
        if (value is null || binding.ValueMap.Count == 0)
        {
            return value;
        }

        var mapped = binding.ValueMap.FirstOrDefault(entry =>
            string.Equals(entry.Key, value, StringComparison.OrdinalIgnoreCase));

        return mapped.Value ?? value;
    }

    private ImportRowDiagnostic Diagnostic(
        ImportRow row,
        string fieldKey,
        ImportDiagnosticSeverity severity,
        string code,
        string message,
        string? value)
    {
        var binding = FindBinding(fieldKey);
        var columnName = binding?.ColumnIndex is null ? null : ColumnNames.ElementAtOrDefault(binding.ColumnIndex.Value);

        return new ImportRowDiagnostic
        {
            RowNumber = row.RowNumber,
            ColumnName = columnName,
            Severity = severity,
            Code = code,
            Message = message,
            Value = value,
        };
    }
}
