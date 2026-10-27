using System.Text.Json;

using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

internal static class AiChangeEditors
{
    private static readonly HashSet<string> TextFields = new(StringComparer.Ordinal)
    {
        "name",
        "description",
        "body",
        "comment",
        "goal",
        "inverseName",
        "repositoryUrl",
    };

    private static readonly HashSet<string> MarkdownFields = new(StringComparer.Ordinal)
    {
        "description",
        "body",
        "comment",
    };

    public static AiChangeFieldEditor? Find(string toolName, string fieldName, JsonElement payload)
    {
        return toolName switch
        {
            CreateTaskTool.ChangeName => ForCreatedTask(fieldName, payload),
            UpdateTaskTool.UpdateChange => ForUpdatedTask(fieldName, payload),
            AiTaskChangeDrafts.AssignChange => ForNamedField(fieldName, "assignees", AiChangeEditorKind.Users),
            AiTaskChangeDrafts.TagsChange => ForNamedField(fieldName, "tags", AiChangeEditorKind.Tags),
            AiTaskChangeDrafts.BoardGroupChange => ForMovedTask(
                fieldName,
                "boardGroup",
                AiChangeEditorKind.BoardGroup,
                payload),
            SetTaskSprintTool.MoveChange => ForMovedTask(fieldName, "sprint", AiChangeEditorKind.Sprint, payload),
            _ => ForText(fieldName, payload),
        };
    }

    private static AiChangeFieldEditor? ForCreatedTask(string fieldName, JsonElement payload)
    {
        var projectId = AiChangePayload.ReadInt(payload, "projectId");
        var isInExistingProject = projectId.HasValue;

        return fieldName switch
        {
            "status" => Clearable(AiChangeEditorKind.Status),
            "assignee" => Clearable(AiChangeEditorKind.User),
            "tags" => Required(AiChangeEditorKind.Tags),
            "priority" => Required(AiChangeEditorKind.Priority),
            "estimate" => Clearable(AiChangeEditorKind.Estimate),
            "startDate" or "dueDate" => Clearable(AiChangeEditorKind.Date),
            "sprint" when isInExistingProject => Clearable(AiChangeEditorKind.Sprint, projectId),
            "boardGroup" when isInExistingProject => Clearable(AiChangeEditorKind.BoardGroup, projectId),
            _ => ForText(fieldName, payload),
        };
    }

    private static AiChangeFieldEditor? ForUpdatedTask(string fieldName, JsonElement payload)
    {
        return fieldName switch
        {
            "status" => Required(AiChangeEditorKind.Status),
            "priority" => Required(AiChangeEditorKind.Priority),
            "estimate" => Required(AiChangeEditorKind.Estimate),
            "startDate" or "dueDate" => Clearable(AiChangeEditorKind.Date),
            _ => ForText(fieldName, payload),
        };
    }

    private static AiChangeFieldEditor? ForNamedField(string fieldName, string editableName, AiChangeEditorKind kind)
    {
        var isEditable = string.Equals(fieldName, editableName, StringComparison.Ordinal);

        return isEditable ? Required(kind) : null;
    }

    private static AiChangeFieldEditor? ForMovedTask(
        string fieldName,
        string editableName,
        AiChangeEditorKind kind,
        JsonElement payload)
    {
        var isEditableField = string.Equals(fieldName, editableName, StringComparison.Ordinal);
        var isExistingTask = AiChangePayload.ReadInt(payload, "taskId").HasValue;

        if (!isEditableField || !isExistingTask)
        {
            return null;
        }

        var projectId = AiChangePayload.ReadInt(payload, "projectId");

        return new AiChangeFieldEditor { Kind = kind, ProjectId = projectId };
    }

    // Only a field whose payload carries the same string under the same name can be rewritten as
    // text; anything else is applied from other keys and an edit to it would be silently dropped.
    private static AiChangeFieldEditor? ForText(string fieldName, JsonElement payload)
    {
        var isTextField = TextFields.Contains(fieldName);
        var hasPayloadText = AiChangePayload.ReadString(payload, fieldName) is not null;

        if (!isTextField || !hasPayloadText)
        {
            return null;
        }

        var isMarkdown = MarkdownFields.Contains(fieldName);

        return Required(isMarkdown ? AiChangeEditorKind.Markdown : AiChangeEditorKind.Text);
    }

    private static AiChangeFieldEditor Required(AiChangeEditorKind kind)
    {
        return new AiChangeFieldEditor { Kind = kind };
    }

    private static AiChangeFieldEditor Clearable(AiChangeEditorKind kind, int? projectId = null)
    {
        return new AiChangeFieldEditor { Kind = kind, IsClearable = true, ProjectId = projectId };
    }
}
