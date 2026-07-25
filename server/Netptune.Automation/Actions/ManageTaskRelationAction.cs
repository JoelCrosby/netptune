using System.Text.Json;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Automation.Actions;

internal sealed class ManageTaskRelationAction : IAutomationAction
{
    public AutomationActionType Type => AutomationActionType.ManageTaskRelation;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>
    {
        NetptunePermissions.Tasks.Update,
    };

    public string? Validate(AutomationActionRequest request)
    {
        var hasRelationType = request.RelationTypeId is > 0;

        if (!hasRelationType)
        {
            return "Relation actions require a relation type.";
        }

        var operation = request.RelationOperation ?? AutomationRelationOperation.Add;
        var hasSupportedOperation = Enum.IsDefined(operation);

        if (!hasSupportedOperation)
        {
            return "Relation actions have an unsupported operation.";
        }

        var direction = request.RelationDirection ?? AutomationRelationDirection.TaskIsSource;
        var hasSupportedDirection = Enum.IsDefined(direction);

        if (!hasSupportedDirection)
        {
            return "Relation actions have an unsupported direction.";
        }

        var isAdd = operation == AutomationRelationOperation.Add;
        var hasRelatedTask = request.RelatedTaskId is > 0;

        if (isAdd && !hasRelatedTask)
        {
            return "Relation actions require a task to link to.";
        }

        return null;
    }

    public JsonDocument CreateConfig(AutomationActionRequest request)
    {
        var operation = request.RelationOperation ?? AutomationRelationOperation.Add;

        return JsonSerializer.SerializeToDocument(new
        {
            relationOperation = operation,
            relationDirection = request.RelationDirection ?? AutomationRelationDirection.TaskIsSource,
            relationTypeId = request.RelationTypeId,
            relatedTaskId = request.RelatedTaskId,
        }, JsonOptions.Default);
    }

    public AutomationActionViewModel ToViewModel(AutomationAction action)
    {
        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            RelationOperation = ReadOperation(action),
            RelationDirection = ReadDirection(action),
            RelationTypeId = JsonUtils.ReadInt(action.Config, "relationTypeId"),
            RelatedTaskId = JsonUtils.ReadInt(action.Config, "relatedTaskId"),
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var action = context.Action;
        var relationTypeId = JsonUtils.ReadInt(action.Config, "relationTypeId");

        if (relationTypeId is null)
        {
            return new AutomationActionPlanContribution();
        }

        return new AutomationActionPlanContribution
        {
            Relation = new AutomationRelationContribution
            {
                Operation = ReadOperation(action),
                Direction = ReadDirection(action),
                RelationTypeId = relationTypeId.Value,
                RelatedTaskId = JsonUtils.ReadInt(action.Config, "relatedTaskId"),
            },
        };
    }

    private static AutomationRelationOperation ReadOperation(AutomationAction action)
    {
        var operation = JsonUtils.ReadEnum<AutomationRelationOperation>(action.Config, "relationOperation");

        return operation ?? AutomationRelationOperation.Add;
    }

    private static AutomationRelationDirection ReadDirection(AutomationAction action)
    {
        var direction = JsonUtils.ReadEnum<AutomationRelationDirection>(action.Config, "relationDirection");

        return direction ?? AutomationRelationDirection.TaskIsSource;
    }
}
