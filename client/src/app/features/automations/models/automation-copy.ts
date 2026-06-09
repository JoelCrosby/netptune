import { taskStatusLabels, TaskStatus } from '@core/enums/project-task-status';
import { taskPriorityLabels } from '@core/enums/task-priority';
import { EntityType } from '@core/models/entity-type';
import { entityTypeToString } from '@core/transforms/entity-type';
import { isNotNullOrUndefined } from '@core/util/nullish';
import { joinNaturalList, toLowerText } from '@core/util/strings';
import {
  AutomationAction,
  AutomationActionType,
  AutomationRunStatus,
  AutomationTrigger,
  AutomationTriggerType,
  AssigneeChangeMode,
  TaskChangeField,
} from './automation.models';

export const triggerTypeLabels: Record<AutomationTriggerType, string> = {
  [AutomationTriggerType.taskStatusChanged]: 'Task status changes',
  [AutomationTriggerType.taskUnassignedFor]: 'Task is unassigned',
  [AutomationTriggerType.taskChanged]: 'Task changes',
};

export const taskChangeFieldLabels: Record<TaskChangeField, string> = {
  [TaskChangeField.name]: 'Name',
  [TaskChangeField.description]: 'Description',
  [TaskChangeField.status]: 'Status',
  [TaskChangeField.assignees]: 'Assignees',
  [TaskChangeField.owner]: 'Owner',
  [TaskChangeField.priority]: 'Priority',
  [TaskChangeField.estimate]: 'Estimate',
};

export const assigneeChangeModeLabels: Record<AssigneeChangeMode, string> = {
  [AssigneeChangeMode.addedOrRemoved]: 'Added or removed',
  [AssigneeChangeMode.added]: 'Added',
  [AssigneeChangeMode.removed]: 'Removed',
};

export const actionTypeLabels: Record<AutomationActionType, string> = {
  [AutomationActionType.notifyTaskAssignees]: 'Notify task assignees',
  [AutomationActionType.flagTask]: 'Flag task',
  [AutomationActionType.updateTask]: 'Update task',
};

export const automationRunStatusLabels: Record<AutomationRunStatus, string> = {
  [AutomationRunStatus.succeeded]: 'Succeeded',
  [AutomationRunStatus.failed]: 'Failed',
  [AutomationRunStatus.skipped]: 'Skipped',
};

export function describeAutomationTrigger(trigger: AutomationTrigger): string {
  switch (trigger.type) {
    case AutomationTriggerType.taskChanged:
      return describeTaskChangedTrigger(trigger);
    case AutomationTriggerType.taskStatusChanged:
      return `When a task changes to ${statusLabel(trigger.status)}`;
    case AutomationTriggerType.taskUnassignedFor:
      return `When a task is unassigned for ${trigger.durationDays ?? 1} ${pluralizeDays(trigger.durationDays ?? 1)}`;
  }
}

function describeTaskChangedTrigger(trigger: AutomationTrigger): string {
  const fields = trigger.fields?.length
    ? trigger.fields.map((field) => taskChangeFieldLabels[field])
    : ['selected fields'];
  const fieldText = joinNaturalList(fields.map(toLowerText), 'or');

  if (
    trigger.fields?.includes(TaskChangeField.status) &&
    isNotNullOrUndefined(trigger.status)
  ) {
    return `When a task's ${fieldText} changes, with status becoming ${statusLabel(trigger.status)}`;
  }

  if (
    trigger.fields?.includes(TaskChangeField.assignees) &&
    isNotNullOrUndefined(trigger.assigneeChangeMode)
  ) {
    return `When a task's ${fieldText} changes, with assignees ${toLowerText(assigneeChangeModeLabels[trigger.assigneeChangeMode])}`;
  }

  return `When a task's ${fieldText} changes`;
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
    case AutomationActionType.updateTask:
      return describeUpdateTaskAction(action);
  }
}

function describeUpdateTaskAction(action: AutomationAction): string {
  const updates: string[] = [];

  if (isNotNullOrUndefined(action.status)) {
    updates.push(`status to ${statusLabel(action.status)}`);
  }

  if (isNotNullOrUndefined(action.priority)) {
    updates.push(`priority to ${taskPriorityLabels[action.priority]}`);
  }

  return updates.length
    ? `update the task's ${joinNaturalList(updates)}`
    : 'update the task';
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
  return isNotNullOrUndefined(status)
    ? taskStatusLabels[status]
    : 'a selected status';
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
  if (!isNotNullOrUndefined(entityType)) return 'Workspace';

  const label = entityTypeToString(entityType);
  return entityId ? `${label} #${entityId}` : label;
}

function pluralizeDays(days: number): string {
  return days === 1 ? 'day' : 'days';
}
