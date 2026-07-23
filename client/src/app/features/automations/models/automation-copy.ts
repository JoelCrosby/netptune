import { taskPriorityLabels } from '@core/enums/task-priority';
import { EntityType } from '@core/models/entity-type';
import { Status } from '@core/models/status';
import { entityTypeToString } from '@core/transforms/entity-type';
import { isNotNullOrUndefined } from '@core/util/nullish';
import { joinNaturalList, toLowerText } from '@core/util/strings';
import {
  AutomationAction,
  AutomationActionType,
  AutomationConditionGroup,
  AutomationConditionGroupOperator,
  AutomationDelayUnit,
  AutomationConditionOperator,
  AutomationFieldCondition,
  AutomationRunStatus,
  AutomationActionResultStatus,
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

export const automationActionResultStatusLabels: Record<
  AutomationActionResultStatus,
  string
> = {
  [AutomationActionResultStatus.pending]: 'Pending',
  [AutomationActionResultStatus.succeeded]: 'Succeeded',
  [AutomationActionResultStatus.failed]: 'Failed',
  [AutomationActionResultStatus.skipped]: 'Skipped',
  [AutomationActionResultStatus.scheduled]: 'Scheduled',
};

export type AutomationCopySegment =
  { type: 'text'; text: string } | { type: 'status'; statusId: number };

export function describeAutomationTriggerSegments(
  trigger: AutomationTrigger,
  statuses: Status[] = []
): AutomationCopySegment[] {
  if (trigger.type === AutomationTriggerType.taskStatusChanged) {
    return statusSentence('When a task changes to ', trigger.statusId);
  }

  if (trigger.type !== AutomationTriggerType.taskChanged) {
    return textSegments(describeAutomationTrigger(trigger, statuses));
  }

  const fields = trigger.fields?.length
    ? trigger.fields.map((field) => taskChangeFieldLabels[field])
    : ['selected fields'];
  const fieldText = joinNaturalList(fields.map(toLowerText), 'or');

  if (trigger.conditionGroup) {
    return [
      ...textSegments(`When a task's ${fieldText} changes, if `),
      ...describeConditionGroupSegments(trigger.conditionGroup, statuses),
    ];
  }

  if (trigger.conditions?.length) {
    const conditionSegments = trigger.conditions.map((condition) =>
      describeFieldConditionSegments(condition, statuses)
    );

    return [
      ...textSegments(`When a task's ${fieldText} changes, where `),
      ...joinSegments(conditionSegments, ' or '),
    ];
  }

  const hasStatus = trigger.fields?.includes(TaskChangeField.status);

  if (hasStatus && isNotNullOrUndefined(trigger.statusId)) {
    return statusSentence(
      `When a task's ${fieldText} changes, with status becoming `,
      trigger.statusId
    );
  }

  return textSegments(describeAutomationTrigger(trigger, statuses));
}

export function describeAutomationActionSegments(
  action: AutomationAction,
  statuses: Status[] = []
): AutomationCopySegment[] {
  const hasStatusUpdate =
    action.type === AutomationActionType.updateTask &&
    isNotNullOrUndefined(action.statusId);

  if (!hasStatusUpdate) {
    return textSegments(describeAutomationAction(action, statuses));
  }

  const updates: AutomationCopySegment[][] = [
    statusSentence('status to ', action.statusId),
  ];

  if (isNotNullOrUndefined(action.priority)) {
    updates.push(
      textSegments(`priority to ${taskPriorityLabels[action.priority]}`)
    );
  }

  return [
    ...textSegments("Update the task's "),
    ...joinSegments(updates, ' and '),
  ];
}

export function describeAutomationActionsSegments(
  actions: AutomationAction[],
  statuses: Status[] = []
): AutomationCopySegment[] {
  if (!actions.length) return textSegments('No actions configured');

  return joinSegments(
    actions.map((action) => describeAutomationActionSegments(action, statuses)),
    ', then '
  );
}

export function describeAutomationConditionsSegments(
  trigger: AutomationTrigger,
  statuses: Status[] = []
): AutomationCopySegment[] {
  if (trigger.conditionGroup) {
    return describeConditionGroupSegments(trigger.conditionGroup, statuses);
  }

  if (trigger.conditions?.length) {
    return joinSegments(
      trigger.conditions.map((condition) =>
        describeFieldConditionSegments(condition, statuses)
      ),
      ' or '
    );
  }

  return textSegments('Every matching task continues');
}

export function describeAutomationRuleSegments(
  trigger: AutomationTrigger,
  actions: AutomationAction[],
  statuses: Status[] = []
): AutomationCopySegment[] {
  return [
    ...describeAutomationTriggerSegments(trigger, statuses),
    ...textSegments(', '),
    ...describeAutomationActionsSegments(actions, statuses),
    ...textSegments('.'),
  ];
}

function describeFieldConditionSegments(
  condition: AutomationFieldCondition,
  statuses: Status[]
): AutomationCopySegment[] {
  const hasStatusValue =
    condition.field === TaskChangeField.status &&
    condition.value &&
    Number.isInteger(Number(condition.value));
  const hasSupportedOperator =
    condition.operator === AutomationConditionOperator.equals ||
    condition.operator === AutomationConditionOperator.notEquals;

  if (!hasStatusValue || !hasSupportedOperator) {
    return textSegments(describeFieldCondition(condition, statuses));
  }

  const operator =
    condition.operator === AutomationConditionOperator.equals
      ? 'equals'
      : 'does not equal';

  return statusSentence(`status ${operator} `, Number(condition.value));
}

function describeConditionGroupSegments(
  group: AutomationConditionGroup,
  statuses: Status[]
): AutomationCopySegment[] {
  const members = [
    ...group.conditions.map((condition) =>
      describeFieldConditionSegments(condition, statuses)
    ),
    ...group.groups.map((nestedGroup) => [
      ...textSegments('('),
      ...describeConditionGroupSegments(nestedGroup, statuses),
      ...textSegments(')'),
    ]),
  ];
  const separator =
    group.operator === AutomationConditionGroupOperator.all ? ' and ' : ' or ';
  const segments = joinSegments(members, separator);

  return group.operator === AutomationConditionGroupOperator.none
    ? [...textSegments('none of ('), ...segments, ...textSegments(')')]
    : segments;
}

function statusSentence(
  prefix: string,
  statusId: number | null | undefined
): AutomationCopySegment[] {
  if (!isNotNullOrUndefined(statusId)) {
    return textSegments(`${prefix}a selected status`);
  }

  return [...textSegments(prefix), { type: 'status', statusId }];
}

function textSegments(text: string): AutomationCopySegment[] {
  return [{ type: 'text', text }];
}

function joinSegments(
  groups: AutomationCopySegment[][],
  separator: string
): AutomationCopySegment[] {
  return groups.flatMap((group, index) =>
    index === 0 ? group : [...textSegments(separator), ...group]
  );
}

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

  if (trigger.conditionGroup) {
    return `When a task's ${fieldText} changes, if ${describeConditionGroup(trigger.conditionGroup, statuses)}`;
  }

  if (trigger.conditions?.length) {
    const conditionText = joinNaturalList(
      trigger.conditions.map((condition) =>
        describeFieldCondition(condition, statuses)
      ),
      'or'
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

function describeConditionGroup(
  group: AutomationConditionGroup,
  statuses: Status[]
): string {
  const members = [
    ...group.conditions.map((condition) =>
      describeFieldCondition(condition, statuses)
    ),
    ...group.groups.map(
      (nestedGroup) => `(${describeConditionGroup(nestedGroup, statuses)})`
    ),
  ];
  const conjunction =
    group.operator === AutomationConditionGroupOperator.all ? 'and' : 'or';
  const description = joinNaturalList(members, conjunction);

  return group.operator === AutomationConditionGroupOperator.none
    ? `none of (${description})`
    : description;
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
