import { taskPriorityLabels } from '@core/enums/task-priority';
import { EntityType } from '@core/models/entity-type';
import { Status } from '@core/models/status';
import { entityTypeToString } from '@core/transforms/entity-type';
import { isNotNullOrUndefined } from '@core/util/nullish';
import { joinNaturalList, toLowerText } from '@core/util/strings';
import {
  AutomationAction,
  AutomationActionType,
  AutomationDelayUnit,
  AutomationConditionOperator,
  AutomationFieldCondition,
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
  [AutomationTriggerType.taskDueDateApproaching]: 'Task due date approaches',
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
  [TaskChangeField.tags]: 'Tags',
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
  [AutomationActionType.deleteTask]: 'Delete task',
};

export const automationRunStatusLabels: Record<AutomationRunStatus, string> = {
  [AutomationRunStatus.succeeded]: 'Succeeded',
  [AutomationRunStatus.failed]: 'Failed',
  [AutomationRunStatus.skipped]: 'Skipped',
};

export function describeAutomationTrigger(
  trigger: AutomationTrigger,
  statuses: Status[] = []
): string {
  switch (trigger.type) {
    case AutomationTriggerType.taskChanged:
      return describeTaskChangedTrigger(trigger, statuses);
    case AutomationTriggerType.taskStatusChanged:
      return `When a task changes to ${statusLabel(trigger.statusId, statuses)}`;
    case AutomationTriggerType.taskUnassignedFor:
      return `When a task is unassigned for ${trigger.durationDays ?? 1} ${pluralizeDays(trigger.durationDays ?? 1)}`;
    case AutomationTriggerType.taskDueDateApproaching:
      return describeDueDateTrigger(trigger.durationDays ?? 0);
  }
}

function describeDueDateTrigger(durationDays: number): string {
  if (durationDays === 0) return 'When a task is due today';

  return `When a task is due in ${durationDays} ${pluralizeDays(durationDays)}`;
}

function describeTaskChangedTrigger(
  trigger: AutomationTrigger,
  statuses: Status[]
): string {
  const fields = trigger.fields?.length
    ? trigger.fields.map((field) => taskChangeFieldLabels[field])
    : ['selected fields'];
  const fieldText = joinNaturalList(fields.map(toLowerText), 'or');

  if (trigger.conditions?.length) {
    const conditionText = joinNaturalList(
      trigger.conditions.map((condition) =>
        describeFieldCondition(condition, statuses)
      ),
      'and'
    );

    return `When a task's ${fieldText} changes, where ${conditionText}`;
  }

  if (
    trigger.fields?.includes(TaskChangeField.status) &&
    isNotNullOrUndefined(trigger.statusId)
  ) {
    return `When a task's ${fieldText} changes, with status becoming ${statusLabel(trigger.statusId, statuses)}`;
  }

  if (
    trigger.fields?.includes(TaskChangeField.assignees) &&
    isNotNullOrUndefined(trigger.assigneeChangeMode)
  ) {
    return `When a task's ${fieldText} changes, with assignees ${toLowerText(assigneeChangeModeLabels[trigger.assigneeChangeMode])}`;
  }

  return `When a task's ${fieldText} changes`;
}

function describeFieldCondition(
  condition: AutomationFieldCondition,
  statuses: Status[]
): string {
  const field = toLowerText(taskChangeFieldLabels[condition.field]);
  const conditionValue = describeConditionValue(condition, statuses);
  const value = conditionValue ? ` “${conditionValue}”` : '';

  switch (condition.operator) {
    case AutomationConditionOperator.any:
      return `${field} has any change`;
    case AutomationConditionOperator.equals:
      return `${field} equals${value}`;
    case AutomationConditionOperator.notEquals:
      return `${field} does not equal${value}`;
    case AutomationConditionOperator.contains:
      return `${field} contains${value}`;
    case AutomationConditionOperator.isEmpty:
      return `${field} is empty`;
    case AutomationConditionOperator.isNotEmpty:
      return `${field} is not empty`;
    case AutomationConditionOperator.added:
      return `${value.trim() || field} is added`;
    case AutomationConditionOperator.removed:
      return `${value.trim() || field} is removed`;
  }
}

function describeConditionValue(
  condition: AutomationFieldCondition,
  statuses: Status[]
): string | null {
  if (!condition.value) return null;

  if (condition.field !== TaskChangeField.status) return condition.value;

  const statusId = Number(condition.value);

  return Number.isInteger(statusId)
    ? statusLabel(statusId, statuses)
    : condition.value;
}

export function describeAutomationAction(
  action: AutomationAction,
  statuses: Status[] = []
): string {
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
      return describeUpdateTaskAction(action, statuses);
    case AutomationActionType.addComment:
      return action.comment
        ? `Add comment: "${action.comment}"`
        : 'Add a comment';
    case AutomationActionType.deleteTask:
      return describeDeleteTaskAction(action);
  }
}

function describeDeleteTaskAction(action: AutomationAction): string {
  const amount = action.delayAmount ?? 0;

  if (amount <= 0) return 'Delete the task';

  const unit = action.delayUnit ?? AutomationDelayUnit.minutes;
  const label = AutomationDelayUnit[unit];
  const unitLabel = amount === 1 ? label.replace(/s$/, '') : label;

  return `Delete the task after ${amount} ${unitLabel}`;
}

function describeUpdateTaskAction(
  action: AutomationAction,
  statuses: Status[]
): string {
  const updates: string[] = [];

  if (isNotNullOrUndefined(action.statusId)) {
    updates.push(`status to ${statusLabel(action.statusId, statuses)}`);
  }

  if (isNotNullOrUndefined(action.priority)) {
    updates.push(`priority to ${taskPriorityLabels[action.priority]}`);
  }

  return updates.length
    ? `Update the task's ${joinNaturalList(updates)}`
    : 'Update the task';
}

export function describeAutomationActions(
  actions: AutomationAction[],
  statuses: Status[] = []
): string {
  if (!actions.length) return 'No actions configured';

  return actions
    .map((action) => describeAutomationAction(action, statuses))
    .join(', then ');
}

export function describeAutomationRule(
  trigger: AutomationTrigger,
  actions: AutomationAction[],
  statuses: Status[] = []
): string {
  return `${describeAutomationTrigger(trigger, statuses)}, ${describeAutomationActions(actions, statuses)}.`;
}

export function statusLabel(
  statusId: number | null | undefined,
  statuses: Status[] = []
): string {
  if (!isNotNullOrUndefined(statusId)) return 'a selected status';

  return (
    statuses.find((status) => status.id === statusId)?.name ??
    `status #${statusId}`
  );
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
