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
  [TaskChangeField.dueDate]: 'Due date',
  [TaskChangeField.startDate]: 'Start date',
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
  [AutomationActionType.addComment]: 'Add comment',
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
      return `When a task changes to ${statusLabel(trigger.statusId)}`;
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
    isNotNullOrUndefined(trigger.statusId)
  ) {
    return `When a task's ${fieldText} changes, with status becoming ${statusLabel(trigger.statusId)}`;
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
        ? `Notify assignees: "${action.message}"`
        : 'Notify task assignees';
    case AutomationActionType.flagTask:
      return action.flagName
        ? `Flag the task as "${action.flagName}"`
        : 'Flag the task';
    case AutomationActionType.updateTask:
      return describeUpdateTaskAction(action);
    case AutomationActionType.addComment:
      return action.comment
        ? `Add comment: "${action.comment}"`
        : 'Add a comment';
  }
}

function describeUpdateTaskAction(action: AutomationAction): string {
  const updates: string[] = [];

  if (isNotNullOrUndefined(action.statusId)) {
    updates.push(`status to ${statusLabel(action.statusId)}`);
  }

  if (isNotNullOrUndefined(action.priority)) {
    updates.push(`priority to ${taskPriorityLabels[action.priority]}`);
  }

  return updates.length
    ? `Update the task's ${joinNaturalList(updates)}`
    : 'Update the task';
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

export function statusLabel(statusId: number | null | undefined): string {
  return isNotNullOrUndefined(statusId)
    ? `status #${statusId}`
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
