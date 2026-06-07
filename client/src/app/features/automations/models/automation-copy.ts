import { taskStatusLabels, TaskStatus } from '@core/enums/project-task-status';
import { EntityType } from '@core/models/entity-type';
import { entityTypeToString } from '@core/transforms/entity-type';
import {
  AutomationAction,
  AutomationActionType,
  AutomationRunStatus,
  AutomationTrigger,
  AutomationTriggerType,
} from './automation.models';

export const triggerTypeLabels: Record<AutomationTriggerType, string> = {
  [AutomationTriggerType.taskStatusChanged]: 'Task status changes',
  [AutomationTriggerType.taskUnassignedFor]: 'Task is unassigned',
};

export const actionTypeLabels: Record<AutomationActionType, string> = {
  [AutomationActionType.notifyTaskAssignees]: 'Notify task assignees',
  [AutomationActionType.flagTask]: 'Flag task',
};

export const automationRunStatusLabels: Record<AutomationRunStatus, string> = {
  [AutomationRunStatus.succeeded]: 'Succeeded',
  [AutomationRunStatus.failed]: 'Failed',
  [AutomationRunStatus.skipped]: 'Skipped',
};

export function describeAutomationTrigger(trigger: AutomationTrigger): string {
  switch (trigger.type) {
    case AutomationTriggerType.taskStatusChanged:
      return `When a task changes to ${statusLabel(trigger.status)}`;
    case AutomationTriggerType.taskUnassignedFor:
      return `When a task is unassigned for ${trigger.durationDays ?? 1} ${pluralizeDays(trigger.durationDays ?? 1)}`;
  }
}

export function describeAutomationAction(action: AutomationAction): string {
  switch (action.type) {
    case AutomationActionType.notifyTaskAssignees:
      return action.message
        ? `notify assignees: "${action.message}"`
        : 'notify task assignees';
    case AutomationActionType.flagTask:
      return action.flagName
        ? `flag the task as "${action.flagName}"`
        : 'flag the task';
  }
}

export function describeAutomationActions(actions: AutomationAction[]): string {
  if (!actions.length) return 'No actions configured';

  return actions.map(describeAutomationAction).join(', then ');
}

export function describeAutomationRule(
  trigger: AutomationTrigger,
  actions: AutomationAction[]
): string {
  return `${describeAutomationTrigger(trigger)}, ${describeAutomationActions(actions)}.`;
}

export function statusLabel(status: TaskStatus | null | undefined): string {
  return status === null || status === undefined
    ? 'a selected status'
    : taskStatusLabels[status];
}

export function runStatusClass(status: AutomationRunStatus): string {
  switch (status) {
    case AutomationRunStatus.succeeded:
      return 'bg-green-500/10 text-green-600 dark:text-green-400';
    case AutomationRunStatus.failed:
      return 'bg-red-500/10 text-red-600 dark:text-red-400';
    case AutomationRunStatus.skipped:
      return 'bg-amber-500/10 text-amber-600 dark:text-amber-400';
  }
}

export function entityTargetLabel(
  entityType: EntityType | null | undefined,
  entityId: number | null | undefined
): string {
  if (entityType === null || entityType === undefined) return 'Workspace';

  const label = entityTypeToString(entityType);
  return entityId ? `${label} #${entityId}` : label;
}

function pluralizeDays(days: number): string {
  return days === 1 ? 'day' : 'days';
}
