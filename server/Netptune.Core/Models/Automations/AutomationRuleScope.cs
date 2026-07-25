using Netptune.Core.Entities;

namespace Netptune.Core.Models.Automations;

public static class AutomationRuleScope
{
    public static bool Contains(AutomationRule rule, ProjectTask task)
    {
        if (rule.ProjectId.HasValue)
        {
            return task.ProjectId == rule.ProjectId;
        }

        if (rule.SprintId.HasValue)
        {
            return task.SprintId == rule.SprintId;
        }

        if (rule.BoardId.HasValue)
        {
            return IsInBoard(task, rule.BoardId.Value);
        }

        return true;
    }

    private static bool IsInBoard(ProjectTask task, int boardId)
    {
        return task.ProjectTaskInBoardGroups
            .Any(link => link.BoardGroup is not null && link.BoardGroup.BoardId == boardId);
    }
}
