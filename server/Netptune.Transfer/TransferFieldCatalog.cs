using System.Collections.Frozen;

using Netptune.Core.Constants;
using Netptune.Transfer.Catalog;

namespace Netptune.Transfer;

public static class TransferFieldCatalog
{
    public static TransferRecordType Task { get; } = new()
    {
        Key = EntityRefTypes.Task,
        Name = "Task",
        IsStandaloneExportable = true,
        Fields =
        [
            new TransferField
            {
                Key = TaskFieldKeys.SystemId,
                Name = "System id",
                ValueType = TransferValueType.Text,
                IsExportedByDefault = true,
                Synonyms = ["id", "task id", "issue key", "issue id", "ticket", "key", "reference"],
                Example = "acme-14",
            },
            new TransferField
            {
                Key = TaskFieldKeys.Name,
                Name = "Name",
                ValueType = TransferValueType.Text,
                IsRequiredForImport = true,
                IsExportedByDefault = true,
                Synonyms = ["summary", "title", "subject", "task", "task name"],
                Example = "Fix the export fan-out",
            },
            new TransferField
            {
                Key = TaskFieldKeys.Description,
                Name = "Description",
                ValueType = TransferValueType.LongText,
                IsExportedByDefault = true,
                Synonyms = ["notes", "details", "body", "content"],
            },
            new TransferField
            {
                Key = TaskFieldKeys.Status,
                Name = "Status",
                ValueType = TransferValueType.Ref,
                IsExportedByDefault = true,
                RefType = EntityRefTypes.Status,
                Synonyms = ["state", "workflow status", "column"],
                Example = "in-progress",
            },
            new TransferField
            {
                Key = TaskFieldKeys.Priority,
                Name = "Priority",
                ValueType = TransferValueType.Enum,
                IsExportedByDefault = true,
                Synonyms = ["importance", "severity", "urgency"],
                Example = "High",
            },
            new TransferField
            {
                Key = TaskFieldKeys.EstimateType,
                Name = "Estimate type",
                ValueType = TransferValueType.Enum,
                Synonyms = ["estimate unit", "effort unit"],
                Example = "StoryPoints",
            },
            new TransferField
            {
                Key = TaskFieldKeys.EstimateValue,
                Name = "Estimate",
                ValueType = TransferValueType.Decimal,
                IsExportedByDefault = true,
                Synonyms = ["story points", "estimate", "points", "effort", "size"],
                Example = "3",
            },
            new TransferField
            {
                Key = TaskFieldKeys.StartDate,
                Name = "Start date",
                ValueType = TransferValueType.Date,
                IsExportedByDefault = true,
                Synonyms = ["start", "begin date", "scheduled start"],
                Example = "2026-08-04",
            },
            new TransferField
            {
                Key = TaskFieldKeys.DueDate,
                Name = "Due date",
                ValueType = TransferValueType.Date,
                IsExportedByDefault = true,
                Synonyms = ["due", "deadline", "target date", "end date"],
                Example = "2026-08-18",
            },
            new TransferField
            {
                Key = TaskFieldKeys.Project,
                Name = "Project",
                ValueType = TransferValueType.Ref,
                IsExportedByDefault = true,
                RefType = EntityRefTypes.Project,
                Synonyms = ["project key", "project name"],
                Example = "acme",
            },
            new TransferField
            {
                Key = TaskFieldKeys.Sprint,
                Name = "Sprint",
                ValueType = TransferValueType.Ref,
                IsExportedByDefault = true,
                RefType = EntityRefTypes.Sprint,
                Synonyms = ["iteration", "milestone", "cycle"],
                Example = "acme/sprint-1",
            },
            new TransferField
            {
                Key = TaskFieldKeys.BoardGroup,
                Name = "Board group",
                ValueType = TransferValueType.Ref,
                IsExportedByDefault = true,
                RefType = EntityRefTypes.BoardGroup,
                Synonyms = ["group", "list", "section", "swimlane", "bucket"],
                Example = "acme-default-board/todo",
            },
            new TransferField
            {
                Key = TaskFieldKeys.Assignees,
                Name = "Assignees",
                ValueType = TransferValueType.Ref,
                IsCollection = true,
                IsExportedByDefault = true,
                RefType = EntityRefTypes.User,
                Synonyms = ["assignee", "assigned to", "owner", "responsible"],
                Example = "person@acme.co.uk",
            },
            new TransferField
            {
                Key = TaskFieldKeys.Tags,
                Name = "Tags",
                ValueType = TransferValueType.Ref,
                IsCollection = true,
                IsExportedByDefault = true,
                RefType = EntityRefTypes.Tag,
                Synonyms = ["labels", "categories", "keywords"],
                Example = "backend|urgent",
            },
            new TransferField
            {
                Key = TaskFieldKeys.CreatedBy,
                Name = "Created by",
                ValueType = TransferValueType.Ref,
                IsExportedByDefault = true,
                RefType = EntityRefTypes.User,
                Synonyms = ["reporter", "author", "raised by", "creator"],
                Example = "person@acme.co.uk",
            },
            new TransferField
            {
                Key = TaskFieldKeys.CreatedAt,
                Name = "Created at",
                ValueType = TransferValueType.DateTime,
                IsExportedByDefault = true,
                Synonyms = ["created", "opened", "date created", "raised"],
                Example = "2026-08-04T09:15:00Z",
            },
            new TransferField
            {
                Key = TaskFieldKeys.UpdatedAt,
                Name = "Updated at",
                ValueType = TransferValueType.DateTime,
                IsExportedByDefault = true,
                Synonyms = ["updated", "modified", "last updated", "last modified"],
                Example = "2026-08-05T11:02:00Z",
            },
        ],
    };

    // The archive declares its own, fuller `task` shape — links live in their own files there — so the
    // standalone record type wins for the catalog and the archive definition is used only by the
    // archive reader and writer.
    public static IReadOnlyList<TransferRecordType> All { get; } =
    [
        Task,
        .. ArchiveCatalog.RecordTypes.Where(recordType => recordType.Key != EntityRefTypes.Task),
    ];

    private static readonly FrozenDictionary<string, TransferRecordType> RecordTypesByKey = All
        .ToFrozenDictionary(recordType => recordType.Key, StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, TransferField> FieldsByKey = All
        .SelectMany(recordType => recordType.Fields)
        .ToFrozenDictionary(field => field.Key, StringComparer.OrdinalIgnoreCase);

    public static TransferRecordType? FindRecordType(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return RecordTypesByKey.GetValueOrDefault(key);
    }

    public static TransferField? FindField(string? fieldKey)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return null;
        }

        return FieldsByKey.GetValueOrDefault(fieldKey);
    }

    public static bool IsKnownField(string? fieldKey)
    {
        var field = FindField(fieldKey);

        return field is not null;
    }
}
