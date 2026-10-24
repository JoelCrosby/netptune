using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Mediator;

using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Ai;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Tasks.Queries;
using Netptune.Handlers.Users.Queries;

namespace Netptune.Ai.Execution;

public sealed partial class AiChangeEditCatalog : IAiChangeEditCatalog
{
    private const int MaximumTextLength = 8000;
    private const decimal SmallestTShirtSize = 1;
    private const decimal LargestTShirtSize = 5;
    private const string DateFormat = "yyyy-MM-dd";
    private const string ClearKey = "clear";
    private static string MissingTaskMessage => AppText.AiChangeErrorTaskChangeMovesNoLongerWorkspace;

    private readonly IMediator Mediator;

    public AiChangeEditCatalog(IMediator mediator)
    {
        Mediator = mediator;
    }

    public AiChangeFieldEditor? FindEditor(AiProposedChange change, string fieldName)
    {
        return AiChangeEditors.Find(change.ToolName, fieldName, change.Payload.RootElement);
    }

    public async Task<AiChangeEditResult> Apply(
        AiProposedChange change,
        IReadOnlyList<AiProposedChange> changeSet,
        IReadOnlyList<AiChangeFieldEdit> edits,
        CancellationToken cancellationToken)
    {
        if (edits.Count == 0)
        {
            return Failed(AppText.AiChangeErrorNoFieldValuesSupplied);
        }

        var fields = AiChangeFieldSerializer.Deserialize(change.Fields);
        var lookups = new EditLookups(change, changeSet);
        var resolved = new List<AiResolvedFieldEdit>();

        // Bounded by the fields of a single change, and EditLookups caches every workspace read.
        foreach (var edit in edits)
        {
            var field = fields.Find(candidate => string.Equals(candidate.Name, edit.Name, StringComparison.Ordinal));

            if (field is null)
            {
                return Failed(Localized.Format(AppText.AiChangeErrorNotPartChange, AiFieldNames.Label(edit.Name)));
            }

            var editor = FindEditor(change, edit.Name);

            if (editor is null)
            {
                return Failed(Localized.Format(AppText.AiChangeErrorCannotEditedHere, AiFieldNames.Label(edit.Name)));
            }

            var request = new FieldEditRequest { Field = field, Editor = editor, Edit = edit };
            var outcome = await Resolve(request, lookups, cancellationToken);

            if (outcome.Error is not null)
            {
                return Failed(outcome.Error);
            }

            resolved.Add(outcome.Edit!);
        }

        return AiProposedChangeEditor.Apply(change.Summary, change.Fields, change.Payload, resolved);
    }

    private async Task<FieldEditOutcome> Resolve(
        FieldEditRequest request,
        EditLookups lookups,
        CancellationToken cancellationToken)
    {
        var isClearing = request.IsClearing;

        if (isClearing && !request.Editor.IsClearable)
        {
            var error = Localized.Format(AppText.AiChangeErrorCannotLeftEmpty, AiFieldNames.Label(request.Field.Name));

            return FieldEditOutcome.Failed(error);
        }

        return request.Editor.Kind switch
        {
            AiChangeEditorKind.Text or AiChangeEditorKind.Markdown => ResolveText(request),
            AiChangeEditorKind.Priority => ResolvePriority(request),
            AiChangeEditorKind.Estimate => ResolveEstimate(request),
            AiChangeEditorKind.Date => ResolveDate(request, lookups),
            AiChangeEditorKind.Status => await ResolveStatus(request, lookups, cancellationToken),
            AiChangeEditorKind.User => await ResolveUser(request, cancellationToken),
            AiChangeEditorKind.Users => await ResolveUsers(request, cancellationToken),
            AiChangeEditorKind.Tags => await ResolveTags(request, lookups, cancellationToken),
            AiChangeEditorKind.Sprint => await ResolveSprint(request, lookups, cancellationToken),
            AiChangeEditorKind.BoardGroup => await ResolveBoardGroup(request, lookups, cancellationToken),
            _ => NotEditable(request.Field.Name),
        };
    }

    private static FieldEditOutcome ResolveText(FieldEditRequest request)
    {
        var name = request.Field.Name;
        var value = request.Edit.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            var error = Localized.Format(AppText.AiChangeErrorCannotLeftEmpty, AiFieldNames.Label(name));

            return FieldEditOutcome.Failed(error);
        }

        if (value.Length > MaximumTextLength)
        {
            var error = Localized.Format(
                AppText.AiChangeErrorLongerThanCharacters,
                AiFieldNames.Label(name),
                MaximumTextLength);

            return FieldEditOutcome.Failed(error);
        }

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = name,
            After = value,
            Payload = { [name] = JsonValue.Create(value) },
        });
    }

    private static FieldEditOutcome ResolvePriority(FieldEditRequest request)
    {
        var isParsed = Enum.TryParse<TaskPriority>(request.Edit.Value, true, out var priority);
        var isKnown = isParsed && Enum.IsDefined(priority);

        if (!isKnown)
        {
            return FieldEditOutcome.Failed(Localized.Format(AppText.AiChangeErrorNotPriority, request.Edit.Value));
        }

        var priorityName = priority.ToString();

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = request.Field.Name,
            After = priorityName,
            Payload = { ["priority"] = JsonValue.Create(priorityName) },
        });
    }

    private static FieldEditOutcome ResolveEstimate(FieldEditRequest request)
    {
        var name = request.Field.Name;

        if (request.IsClearing)
        {
            return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
            {
                Name = name,
                RemovedPayload = ["estimateType", "estimateValue"],
            });
        }

        var isParsed = Enum.TryParse<EstimateType>(request.Edit.Value, true, out var type);
        var isKnown = isParsed && Enum.IsDefined(type);

        if (!isKnown)
        {
            return FieldEditOutcome.Failed(Localized.Format(AppText.AiChangeErrorNotEstimateUnit, request.Edit.Value));
        }

        var value = request.Edit.Number;

        if (!value.HasValue)
        {
            return FieldEditOutcome.Failed(AppText.AiChangeErrorEstimateNeedsValue);
        }

        var isTShirt = type == EstimateType.TShirt;
        var isTShirtSize = value.Value is >= SmallestTShirtSize and <= LargestTShirtSize && value.Value % 1 == 0;

        if (isTShirt && !isTShirtSize)
        {
            return FieldEditOutcome.Failed(AppText.AiChangeErrorTShirtEstimatesRunXSXL);
        }

        if (value.Value < 0)
        {
            return FieldEditOutcome.Failed(AppText.AiChangeErrorEstimateCannotNegative);
        }

        var formattedValue = value.Value.ToString("0.##", CultureInfo.InvariantCulture);

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = name,
            After = $"{formattedValue} {type}",
            Payload =
            {
                ["estimateType"] = JsonValue.Create(type.ToString()),
                ["estimateValue"] = JsonValue.Create(value.Value),
            },
        });
    }

    private static FieldEditOutcome ResolveDate(FieldEditRequest request, EditLookups lookups)
    {
        var name = request.Field.Name;
        var clearedNames = lookups.ReadCleared();
        var isUpdate = lookups.IsChange(UpdateTaskTool.UpdateChange);

        if (request.IsClearing)
        {
            var withCleared = clearedNames.Append(name).Distinct(StringComparer.Ordinal).ToList();

            return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
            {
                Name = name,
                AfterValues = [],
                Payload = ClearListPayload(isUpdate, withCleared),
                RemovedPayload = [name],
            });
        }

        var isParsed = DateOnly.TryParseExact(
            request.Edit.Value,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date);

        if (!isParsed)
        {
            var error = Localized.Format(AppText.AiChangeErrorMustDateYYYYMMDDForm, AiFieldNames.Label(name));

            return FieldEditOutcome.Failed(error);
        }

        var dateField = AiChangeFields.Date(name, null, date);
        var withoutCleared = clearedNames.Where(cleared => cleared != name).ToList();
        var payload = ClearListPayload(isUpdate, withoutCleared);

        payload[name] = JsonValue.Create(date.ToString(DateFormat));

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = name,
            After = dateField.After,
            AfterValues = dateField.AfterValues,
            Payload = payload,
        });
    }

    // Only an update reads a "clear" list; a creation leaves an unset date out of its payload.
    private static Dictionary<string, JsonNode?> ClearListPayload(bool isUpdate, List<string> clearedNames)
    {
        if (!isUpdate)
        {
            return [];
        }

        return new Dictionary<string, JsonNode?> { [ClearKey] = ToJsonArray(clearedNames) };
    }

    private async Task<FieldEditOutcome> ResolveStatus(
        FieldEditRequest request,
        EditLookups lookups,
        CancellationToken cancellationToken)
    {
        var name = request.Field.Name;

        if (request.IsClearing)
        {
            return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
            {
                Name = name,
                AfterValues = [],
                RemovedPayload = ["statusId"],
            });
        }

        var statusId = ParseId(request.Edit.Value);
        var statuses = await lookups.Statuses(Mediator, cancellationToken);
        var status = statuses.FirstOrDefault(candidate => candidate.Id == statusId);

        if (status is null)
        {
            return FieldEditOutcome.Failed(AppText.AiChangeErrorStatusNotWorkspace);
        }

        var statusValue = AiChangeFields.Status(status.Id, status.Name, status.Color);

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = name,
            After = status.Name,
            AfterValues = [statusValue],
            Payload = { ["statusId"] = JsonValue.Create(status.Id) },
        });
    }

    private async Task<FieldEditOutcome> ResolveUser(FieldEditRequest request, CancellationToken cancellationToken)
    {
        var name = request.Field.Name;

        if (request.IsClearing)
        {
            return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
            {
                Name = name,
                AfterValues = [],
                RemovedPayload = ["assigneeId"],
            });
        }

        var members = await Mediator.Send(new GetWorkspaceMembersByIdQuery([request.Edit.Value!]), cancellationToken);
        var member = members.SingleOrDefault();

        if (member is null)
        {
            return FieldEditOutcome.Failed(AppText.AiChangeErrorPersonNotMemberWorkspace);
        }

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = name,
            After = member.DisplayName,
            AfterValues = [ToUserValue(member)],
            Payload = { ["assigneeId"] = JsonValue.Create(member.Id) },
        });
    }

    private async Task<FieldEditOutcome> ResolveUsers(FieldEditRequest request, CancellationToken cancellationToken)
    {
        var userIds = (request.Edit.Values ?? []).Distinct(StringComparer.Ordinal).ToList();
        var memberList = await Mediator.Send(new GetWorkspaceMembersByIdQuery(userIds), cancellationToken);
        var members = memberList.ToDictionary(member => member.Id, StringComparer.Ordinal);
        var isEveryUserAMember = userIds.All(members.ContainsKey);

        if (!isEveryUserAMember)
        {
            return FieldEditOutcome.Failed(AppText.AiChangeErrorPeopleNotMembersWorkspace);
        }

        var users = userIds.Select(id => ToUserValue(members[id])).ToList();
        var userIdsNode = JsonSerializer.SerializeToNode(userIds);

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = request.Field.Name,
            After = RenderValues(users),
            AfterValues = users,
            Payload = { ["assigneeIds"] = userIdsNode },
        });
    }

    private async Task<FieldEditOutcome> ResolveTags(
        FieldEditRequest request,
        EditLookups lookups,
        CancellationToken cancellationToken)
    {
        var requested = (request.Edit.Values ?? [])
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var proposedNames = lookups.ProposedTagNames();
        var notProposed = requested.Where(tag => !proposedNames.Contains(tag)).ToList();
        var existing = await Mediator.Send(new GetTagsByNameQuery(notProposed), cancellationToken);
        var existingNames = existing.Select(tag => tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = notProposed.Where(tag => !existingNames.Contains(tag)).ToList();

        if (unknown.Count > 0)
        {
            var error = Localized.Format(AppText.AiChangeErrorTagsDoNotExistWorkspace, string.Join(", ", unknown));

            return FieldEditOutcome.Failed(error);
        }

        var tags = requested.Select(AiChangeFields.Tag).ToList();
        var tagsNode = JsonSerializer.SerializeToNode(requested);

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = request.Field.Name,
            After = RenderValues(tags),
            AfterValues = tags,
            Payload = { ["tags"] = tagsNode },
        });
    }

    private async Task<FieldEditOutcome> ResolveSprint(
        FieldEditRequest request,
        EditLookups lookups,
        CancellationToken cancellationToken)
    {
        var name = request.Field.Name;

        if (request.IsClearing)
        {
            return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
            {
                Name = name,
                AfterValues = [],
                RemovedPayload = ["sprintId", "sprintRef"],
            });
        }

        var sprintId = ParseId(request.Edit.Value);

        if (!sprintId.HasValue)
        {
            return FieldEditOutcome.Failed(AppText.AiChangeErrorSprintNotWorkspace);
        }

        var sprint = await AiSprintLookup.Find(Mediator, sprintId.Value, cancellationToken);

        if (sprint is null)
        {
            return FieldEditOutcome.Failed(AppText.AiChangeErrorSprintNotWorkspace);
        }

        var isCompleted = sprint.Status == SprintStatus.Completed;

        if (isCompleted)
        {
            var error = Localized.Format(AppText.AiChangeErrorSprintCompletedCanNoLongerChanged, sprint.Name);

            return FieldEditOutcome.Failed(error);
        }

        var projectId = await lookups.ProjectId(request.Editor, Mediator, cancellationToken);

        if (!projectId.HasValue)
        {
            return FieldEditOutcome.Failed(MissingTaskMessage);
        }

        var isSameProject = sprint.ProjectId == projectId;

        if (!isSameProject)
        {
            var error = Localized.Format(AppText.AiChangeErrorSprintBelongsDifferentProject, sprint.Name);

            return FieldEditOutcome.Failed(error);
        }

        var sprintValue = AiChangeFields.Sprint(sprint.Id, sprint.Name);

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = name,
            After = sprint.Name,
            AfterValues = [sprintValue],
            Payload = { ["sprintId"] = JsonValue.Create(sprint.Id) },
            RemovedPayload = ["sprintRef"],
        });
    }

    private async Task<FieldEditOutcome> ResolveBoardGroup(
        FieldEditRequest request,
        EditLookups lookups,
        CancellationToken cancellationToken)
    {
        var name = request.Field.Name;

        if (request.IsClearing)
        {
            return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
            {
                Name = name,
                AfterValues = [],
                RemovedPayload = ["boardGroupId", "boardGroupRef"],
            });
        }

        var boardGroupId = ParseId(request.Edit.Value);
        var options = await Mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);
        var group = options.FirstOrDefault(option => option.Id == boardGroupId);

        if (group is null)
        {
            return FieldEditOutcome.Failed(AppText.AiChangeErrorBoardGroupNotWorkspace);
        }

        var projectId = await lookups.ProjectId(request.Editor, Mediator, cancellationToken);

        if (!projectId.HasValue)
        {
            return FieldEditOutcome.Failed(MissingTaskMessage);
        }

        var isSameProject = group.ProjectId == projectId;

        if (!isSameProject)
        {
            return FieldEditOutcome.Failed(Localized.Format(AppText.AiChangeErrorBoardDifferentProject, group.Name));
        }

        var isMove = lookups.IsChange(AiTaskChangeDrafts.BoardGroupChange);
        var payload = new Dictionary<string, JsonNode?> { ["boardGroupId"] = JsonValue.Create(group.Id) };

        if (isMove)
        {
            payload["boardIdentifier"] = JsonValue.Create(group.BoardIdentifier);
        }

        var summary = isMove ? MoveSummary(lookups.Change.Summary, group) : null;
        var groupField = AiChangeFields.BoardGroup(group.Id, $"{group.BoardName} · {group.Name}");

        return FieldEditOutcome.Resolved(new AiResolvedFieldEdit
        {
            Name = name,
            After = groupField.After,
            AfterValues = groupField.AfterValues,
            Summary = summary,
            Payload = payload,
            RemovedPayload = ["boardGroupRef"],
        });
    }

    private static string MoveSummary(string summary, BoardGroupOptionViewModel group)
    {
        var quoted = QuotedName().Match(summary);

        if (!quoted.Success)
        {
            return summary;
        }

        var taskName = quoted.Groups[1].Value;

        return Localized.Format(AppText.AiSummaryMoveTaskToBoardGroup, taskName, group.Name, group.BoardName);
    }

    private static FieldEditOutcome NotEditable(string fieldName)
    {
        var label = AiFieldNames.Label(fieldName);
        var error = Localized.Format(AppText.AiChangeErrorCannotEditedHere, label);

        return FieldEditOutcome.Failed(error);
    }

    private static int? ParseId(string? value)
    {
        var isParsed = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id);

        return isParsed ? id : null;
    }

    private static AiChangeValue ToUserValue(UserAvatar member)
    {
        return AiChangeFields.User(member.Id, member.DisplayName, member.ProfilePictureUrl);
    }

    private static string? RenderValues(List<AiChangeValue> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        return string.Join(", ", values.Select(value => value.Display));
    }

    private static JsonArray ToJsonArray(List<string> values)
    {
        var nodes = values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray();

        return new JsonArray(nodes);
    }

    private static AiChangeEditResult Failed(string error)
    {
        return new AiChangeEditResult { Error = error };
    }

    [GeneratedRegex("“([^”]*)”")]
    private static partial Regex QuotedName();

    private sealed record FieldEditRequest
    {
        public required AiChangeFieldViewModel Field { get; init; }

        public required AiChangeFieldEditor Editor { get; init; }

        public required AiChangeFieldEdit Edit { get; init; }

        public bool IsClearing
        {
            get
            {
                var hasValue = !string.IsNullOrWhiteSpace(Edit.Value);
                var isListEditor = Editor.Kind is AiChangeEditorKind.Users or AiChangeEditorKind.Tags;

                return !hasValue && !isListEditor;
            }
        }
    }

    private sealed record FieldEditOutcome
    {
        public AiResolvedFieldEdit? Edit { get; init; }

        public string? Error { get; init; }

        public static FieldEditOutcome Resolved(AiResolvedFieldEdit edit)
        {
            return new FieldEditOutcome { Edit = edit };
        }

        public static FieldEditOutcome Failed(string error)
        {
            return new FieldEditOutcome { Error = error };
        }
    }

    private sealed class EditLookups
    {
        private readonly IReadOnlyList<AiProposedChange> ChangeSet;
        private List<StatusViewModel>? CachedStatuses;
        private int? CachedProjectId;

        public EditLookups(AiProposedChange change, IReadOnlyList<AiProposedChange> changeSet)
        {
            Change = change;
            ChangeSet = changeSet;
        }

        public AiProposedChange Change { get; }

        public bool IsChange(string toolName)
        {
            return string.Equals(Change.ToolName, toolName, StringComparison.Ordinal);
        }

        public List<string> ReadCleared()
        {
            return AiChangePayload.ReadStringArray(Change.Payload.RootElement, ClearKey);
        }

        public HashSet<string> ProposedTagNames()
        {
            return ChangeSet
                .Where(change => string.Equals(change.ToolName, CreateTagAction.Change, StringComparison.Ordinal))
                .Select(change => AiChangePayload.ReadString(change.Payload.RootElement, "name"))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<List<StatusViewModel>> Statuses(IMediator mediator, CancellationToken cancellationToken)
        {
            CachedStatuses ??= await mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken) ?? [];

            return CachedStatuses;
        }

        public async Task<int?> ProjectId(
            AiChangeFieldEditor editor,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            if (editor.ProjectId.HasValue)
            {
                return editor.ProjectId;
            }

            if (CachedProjectId.HasValue)
            {
                return CachedProjectId;
            }

            var taskId = AiChangePayload.ReadInt(Change.Payload.RootElement, "taskId");

            if (!taskId.HasValue)
            {
                return null;
            }

            var task = await mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

            CachedProjectId = task?.ProjectId;

            return CachedProjectId;
        }
    }
}
