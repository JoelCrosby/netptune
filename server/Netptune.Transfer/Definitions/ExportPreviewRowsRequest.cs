using Netptune.Transfer.Enums;
using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.Transfer.Definitions;

// The export definition flattened into query parameters, so preview rows can be fetched with a paged
// GET. Everything that shapes a row is here; the writer-only options (delimiter, header row) are not,
// since they do not change what a cell contains.
public sealed class ExportPreviewRowsRequest : PageRequest
{
    public string? RecordType { get; init; }

    public string[] Fields { get; init; } = [];

    public string[] ProjectKeys { get; init; } = [];

    public string[] BoardIdentifiers { get; init; } = [];

    public string[] StatusKeys { get; init; } = [];

    public int[] StatusCategories { get; init; } = [];

    public string[] Tags { get; init; } = [];

    public string[] AssigneeEmails { get; init; } = [];

    public TaskPriority[] Priorities { get; init; } = [];

    public string? SprintRef { get; init; }

    public string? Term { get; init; }

    public bool? IncludeDeleted { get; init; }

    public DateTime? CreatedFrom { get; init; }

    public DateTime? CreatedTo { get; init; }

    public DateTime? UpdatedSince { get; init; }

    public string? DateFormat { get; init; }

    public string? TimeZoneId { get; init; }

    public string? CollectionSeparator { get; init; }

    public bool? ExpandCollectionsToRows { get; init; }

    public ExportDefinitionModel ToDefinition()
    {
        var options = new ExportOptionsModel();

        return new ExportDefinitionModel
        {
            RecordType = RecordType ?? EntityRefTypes.Task,
            // Rows are format independent, and CSV is the one format that is always legal for a record
            // type, so the definition passes validation whatever the wizard has selected.
            Format = ExportFormat.Csv,
            Fields = [.. Fields],
            Filter = new ExportFilterModel
            {
                ProjectKeys = [.. ProjectKeys],
                BoardIdentifiers = [.. BoardIdentifiers],
                StatusKeys = [.. StatusKeys],
                StatusCategories = [.. StatusCategories],
                Tags = [.. Tags],
                AssigneeEmails = [.. AssigneeEmails],
                Priorities = [.. Priorities],
                SprintRef = SprintRef,
                Term = Term,
                IncludeDeleted = IncludeDeleted ?? false,
                CreatedFrom = CreatedFrom,
                CreatedTo = CreatedTo,
                UpdatedSince = UpdatedSince,
            },
            Options = options with
            {
                DateFormat = DateFormat ?? options.DateFormat,
                TimeZoneId = TimeZoneId ?? options.TimeZoneId,
                CollectionSeparator = CollectionSeparator ?? options.CollectionSeparator,
                ExpandCollectionsToRows = ExpandCollectionsToRows ?? false,
            },
        };
    }
}
