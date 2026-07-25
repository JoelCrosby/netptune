import { taskPriorityLabels } from '@core/enums/task-priority';
import { workspaceRoleLabels } from '@core/enums/workspace-role';
import { WorkspaceAppUser } from '@core/models/appuser';
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
  AutomationDateUpdateMode,
  AutomationConditionOperator,
  AutomationFieldCondition,
  AutomationNotificationRecipient,
  AutomationRelationOperation,
  AutomationRunStatus,
  AutomationActionResultStatus,
  AutomationTrigger,
  AutomationTriggerType,
  TaskChangeField,
} from './automation.models';

export const triggerTypeLabels: Record<AutomationTriggerType, string> = {
  [AutomationTriggerType.taskUnassignedFor]: 'Task is unassigned',
  [AutomationTriggerType.taskChanged]: 'Task changes',
  [AutomationTriggerType.taskDueDateApproaching]: 'Task due date approaches',
  [AutomationTriggerType.taskCreated]: 'Task is created',
  [AutomationTriggerType.taskOverdue]: 'Task becomes overdue',
  [AutomationTriggerType.taskHasNoDueDate]: 'Task has no due date',
  [AutomationTriggerType.taskInactiveFor]: 'Task remains inactive',
  [AutomationTriggerType.sprintStarted]: 'Sprint starts',
  [AutomationTriggerType.sprintCompleted]: 'Sprint completes',
  [AutomationTriggerType.sprintEndingSoon]: 'Sprint end approaches',
  [AutomationTriggerType.taskBlocked]: 'Task becomes blocked',
  [AutomationTriggerType.taskUnblocked]: 'Task becomes unblocked',
  [AutomationTriggerType.subtasksCompleted]: 'All subtasks complete',
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
  [TaskChangeField.sprint]: 'Sprint',
  [TaskChangeField.boardGroup]: 'Board group',
};

export const actionTypeLabels: Record<AutomationActionType, string> = {
  [AutomationActionType.notifyTaskAssignees]: 'Notify task assignees',
  [AutomationActionType.flagTask]: 'Flag task',
  [AutomationActionType.updateTask]: 'Update task',
  [AutomationActionType.addComment]: 'Add comment',
  [AutomationActionType.deleteTask]: 'Delete task',
  [AutomationActionType.createTask]: 'Create task',
  [AutomationActionType.manageTaskRelation]: 'Manage task relation',
};

export const conditionOperatorLabels: Record<
  AutomationConditionOperator,
  string
> = {
  [AutomationConditionOperator.any]: 'changed',
  [AutomationConditionOperator.equals]: 'equals',
  [AutomationConditionOperator.notEquals]: 'does not equal',
  [AutomationConditionOperator.contains]: 'contains',
  [AutomationConditionOperator.isEmpty]: 'is empty',
  [AutomationConditionOperator.isNotEmpty]: 'is not empty',
  [AutomationConditionOperator.added]: 'added',
  [AutomationConditionOperator.removed]: 'removed',
};

export const conditionGroupOperatorLabels: Record<
  AutomationConditionGroupOperator,
  string
> = {
  [AutomationConditionGroupOperator.all]: 'All of',
  [AutomationConditionGroupOperator.any]: 'Any of',
  [AutomationConditionGroupOperator.none]: 'None of',
};

export const notificationRecipientLabels: Record<
  AutomationNotificationRecipient,
  string
> = {
  [AutomationNotificationRecipient.assignees]: 'Assignees',
  [AutomationNotificationRecipient.taskOwner]: 'Task owner',
  [AutomationNotificationRecipient.triggeringUser]: 'Triggering user',
  [AutomationNotificationRecipient.specificUsers]: 'Specific users',
  [AutomationNotificationRecipient.projectMembers]: 'Project members',
  [AutomationNotificationRecipient.workspaceRoles]: 'Workspace roles',
};

export const messageVariables = [
  'task.name',
  'task.key',
  'task.status',
  'task.priority',
  'task.startDate',
  'task.dueDate',
  'project.name',
  'workspace.name',
  'rule.name',
];

export const messageVariableSampleValues: Record<string, string> = {
  'task.name': 'Fix login redirect',
  'task.key': 'NETP-128',
  'task.status': 'In Progress',
  'task.priority': 'High',
  'task.startDate': '2026-08-03',
  'task.dueDate': '2026-08-10',
  'project.name': 'Website Redesign',
  'workspace.name': 'Acme',
  'rule.name': 'Untitled automation',
};

export const notificationRecipientPreviewLabels: Record<
  AutomationNotificationRecipient,
  string
> = {
  [AutomationNotificationRecipient.assignees]: 'Everyone assigned to the task',
  [AutomationNotificationRecipient.taskOwner]: 'The task owner',
  [AutomationNotificationRecipient.triggeringUser]:
    'The user whose change ran the rule',
  [AutomationNotificationRecipient.specificUsers]: 'Chosen users',
  [AutomationNotificationRecipient.projectMembers]:
    "Everyone in the task's project",
  [AutomationNotificationRecipient.workspaceRoles]: 'Chosen workspace roles',
};

export type AutomationScopeKind = 'workspace' | 'project' | 'board' | 'sprint';

export const scopeKindLabels: Record<AutomationScopeKind, string> = {
  workspace: 'Whole workspace',
  project: 'A single project',
  board: 'A single board',
  sprint: 'A single sprint',
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

  return textSegments(describeAutomationTrigger(trigger, statuses));
}

export function describeAutomationActionSegments(
  action: AutomationAction,
  statuses: Status[] = []
): AutomationCopySegment[] {
  const hasExpandedTaskUpdate =
    action.type === AutomationActionType.updateTask &&
    (action.taskName !== null ||
      action.taskDescription !== null ||
      !!action.clearDescription ||
      action.ownerId !== null ||
      !!action.clearOwner ||
      action.assigneeIds !== null ||
      !!action.addTags?.length ||
      !!action.removeTags?.length ||
      action.startDate !== null ||
      action.dueDate !== null ||
      action.estimateType !== null ||
      action.estimateValue !== null ||
      !!action.clearEstimate ||
      action.sprintId !== null ||
      !!action.clearSprint ||
      action.boardGroupId !== null);

  if (hasExpandedTaskUpdate) {
    return textSegments(describeAutomationAction(action, statuses));
  }

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
    case AutomationTriggerType.taskUnassignedFor:
      return `When a task is unassigned for ${trigger.durationDays ?? 1} ${pluralizeDays(trigger.durationDays ?? 1)}`;
    case AutomationTriggerType.taskDueDateApproaching:
      return describeDueDateTrigger(trigger.durationDays ?? 0);
    case AutomationTriggerType.taskCreated:
      return 'When a task is created';
    case AutomationTriggerType.taskOverdue:
      return 'When a task becomes overdue';
    case AutomationTriggerType.taskHasNoDueDate:
      return 'When a task has no due date';
    case AutomationTriggerType.taskInactiveFor:
      return `When a task has no activity for ${trigger.durationDays ?? 1} ${pluralizeDays(trigger.durationDays ?? 1)}`;
    case AutomationTriggerType.sprintStarted:
      return 'When a sprint starts, for every task in it';
    case AutomationTriggerType.sprintCompleted:
      return 'When a sprint completes, for every task in it';
    case AutomationTriggerType.sprintEndingSoon:
      return describeSprintEndingTrigger(trigger.durationDays ?? 0);
    case AutomationTriggerType.taskBlocked:
      return 'When a task becomes blocked by an incomplete task';
    case AutomationTriggerType.taskUnblocked:
      return 'When a task is no longer blocked';
    case AutomationTriggerType.subtasksCompleted:
      return 'When every subtask of a task is complete';
  }
}

function describeSprintEndingTrigger(durationDays: number): string {
  if (durationDays === 0) {
    return 'When a sprint ends today, for every task in it';
  }

  return `When a sprint ends in ${durationDays} ${pluralizeDays(durationDays)}, for every task in it`;
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
      return describeNotifyAction(action);
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
    case AutomationActionType.createTask:
      return describeCreateTaskAction(action);
    case AutomationActionType.manageTaskRelation:
      return describeRelationAction(action);
  }
}

function describeNotifyAction(action: AutomationAction): string {
  const audience = describeNotificationAudience(action);

  return action.message
    ? `Notify ${audience}: "${action.message}"`
    : `Notify ${audience}`;
}

export interface NotificationRecipientPreview {
  text: string;
  isIncomplete: boolean;
}

export function previewNotificationRecipients(
  action: AutomationAction,
  users: WorkspaceAppUser[]
): NotificationRecipientPreview[] {
  return selectedRecipients(action).map((recipient) => {
    if (recipient === AutomationNotificationRecipient.specificUsers) {
      return previewChosenUsers(action, users);
    }

    if (recipient === AutomationNotificationRecipient.workspaceRoles) {
      return previewChosenRoles(action);
    }

    return {
      text: notificationRecipientPreviewLabels[recipient],
      isIncomplete: false,
    };
  });
}

export interface NotificationMessageSegment {
  text: string;
  isVariable: boolean;
  isUnknown: boolean;
}

export function previewNotificationMessage(
  message: string | null | undefined,
  ruleName: string
): NotificationMessageSegment[] {
  if (!message?.trim()) {
    return defaultNotificationSegments(ruleName);
  }

  const segments: NotificationMessageSegment[] = [];
  const pattern = /\{\{([^{}]*)\}\}/g;
  let lastIndex = 0;
  let match = pattern.exec(message);

  while (match) {
    const isPrecededByText = match.index > lastIndex;

    if (isPrecededByText) {
      segments.push(literalSegment(message.slice(lastIndex, match.index)));
    }

    segments.push(variableSegment(match[0], match[1].trim(), ruleName));
    lastIndex = match.index + match[0].length;
    match = pattern.exec(message);
  }

  const hasTrailingText = lastIndex < message.length;

  if (hasTrailingText) {
    segments.push(literalSegment(message.slice(lastIndex)));
  }

  return segments;
}

function defaultNotificationSegments(
  ruleName: string
): NotificationMessageSegment[] {
  return [
    literalSegment("Automation '"),
    resolvedSegment(sampleRuleName(ruleName)),
    literalSegment("' matched this task."),
  ];
}

function variableSegment(
  token: string,
  variable: string,
  ruleName: string
): NotificationMessageSegment {
  if (variable.toLowerCase() === 'rule.name') {
    return resolvedSegment(sampleRuleName(ruleName));
  }

  const known = messageVariables.find((candidate) => {
    return candidate.toLowerCase() === variable.toLowerCase();
  });

  if (!known) {
    return { text: token, isVariable: true, isUnknown: true };
  }

  return resolvedSegment(messageVariableSampleValues[known]);
}

function literalSegment(text: string): NotificationMessageSegment {
  return { text, isVariable: false, isUnknown: false };
}

function resolvedSegment(text: string): NotificationMessageSegment {
  return { text, isVariable: true, isUnknown: false };
}

function sampleRuleName(ruleName: string): string {
  return ruleName.trim() || messageVariableSampleValues['rule.name'];
}

function previewChosenUsers(
  action: AutomationAction,
  users: WorkspaceAppUser[]
): NotificationRecipientPreview {
  const chosenIds = action.recipientUserIds ?? [];

  if (!chosenIds.length) {
    return { text: 'No users chosen yet', isIncomplete: true };
  }

  const names = chosenIds.map((id) => {
    const user = users.find((candidate) => candidate.id === id);

    return user?.displayName ?? 'Unknown user';
  });

  return { text: joinNaturalList(names), isIncomplete: false };
}

function previewChosenRoles(
  action: AutomationAction
): NotificationRecipientPreview {
  const roles = action.recipientRoles ?? [];

  if (!roles.length) {
    return { text: 'No workspace roles chosen yet', isIncomplete: true };
  }

  const labels = roles.map((role) => {
    return `Everyone with the ${workspaceRoleLabels[role]} role`;
  });

  return { text: joinNaturalList(labels), isIncomplete: false };
}

function selectedRecipients(
  action: AutomationAction
): AutomationNotificationRecipient[] {
  if (!action.recipients?.length) {
    return [AutomationNotificationRecipient.assignees];
  }

  return action.recipients;
}

export function describeNotificationAudience(action: AutomationAction): string {
  const parts = selectedRecipients(action).map((recipient) => {
    if (recipient === AutomationNotificationRecipient.workspaceRoles) {
      const roles = action.recipientRoles ?? [];

      return roles.length
        ? joinNaturalList(roles.map((role) => workspaceRoleLabels[role]))
        : 'workspace roles';
    }

    if (recipient === AutomationNotificationRecipient.specificUsers) {
      const count = action.recipientUserIds?.length ?? 0;

      return count === 1 ? '1 chosen user' : `${count} chosen users`;
    }

    return toLowerText(notificationRecipientLabels[recipient]);
  });

  return joinNaturalList(parts);
}

function describeRelationAction(action: AutomationAction): string {
  const isRemoval =
    action.relationOperation === AutomationRelationOperation.remove;

  if (isRemoval) {
    return 'Remove the configured task relations';
  }

  return 'Link the task to the configured task';
}

function describeCreateTaskAction(action: AutomationAction): string {
  const name = action.taskName?.trim();
  const created = name ? `Create task "${name}"` : 'Create a task';

  return action.linkRelationTypeId ? `${created} and link it` : created;
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

  if (action.taskName) {
    updates.push(`name to "${action.taskName}"`);
  }

  if (action.clearDescription) {
    updates.push('clear the description');
  } else if (action.taskDescription) {
    updates.push('set the description');
  }

  if (action.clearOwner) {
    updates.push('clear the owner');
  } else if (action.ownerId) {
    updates.push(`owner to user ${action.ownerId}`);
  }

  if (action.assigneeIds !== null && action.assigneeIds !== undefined) {
    updates.push(
      action.assigneeIds.length
        ? `replace assignees with ${action.assigneeIds.length} selected`
        : 'unassign everyone'
    );
  }

  if (action.addTags?.length) {
    updates.push(`add tags ${action.addTags.join(', ')}`);
  }

  if (action.removeTags?.length) {
    updates.push(`remove tags ${action.removeTags.join(', ')}`);
  }

  if (action.startDate) {
    updates.push(`start date: ${describeDateUpdate(action.startDate)}`);
  }

  if (action.dueDate) {
    updates.push(`due date: ${describeDateUpdate(action.dueDate)}`);
  }

  if (action.clearEstimate) {
    updates.push('clear the estimate');
  } else if (
    isNotNullOrUndefined(action.estimateType) ||
    isNotNullOrUndefined(action.estimateValue)
  ) {
    updates.push('set the estimate');
  }

  if (action.clearSprint) {
    updates.push('move to the backlog');
  } else if (isNotNullOrUndefined(action.sprintId)) {
    updates.push(`move to sprint #${action.sprintId}`);
  }

  if (isNotNullOrUndefined(action.boardGroupId)) {
    updates.push(`move to board group #${action.boardGroupId}`);
  }

  return updates.length
    ? `Update the task's ${joinNaturalList(updates)}`
    : 'Update the task';
}

function describeDateUpdate(
  update: NonNullable<AutomationAction['startDate']>
): string {
  switch (update.mode) {
    case AutomationDateUpdateMode.absolute:
      return update.date || 'selected date';
    case AutomationDateUpdateMode.relativeDays:
      return `${update.offset ?? 0} calendar days from the run date`;
    case AutomationDateUpdateMode.relativeBusinessDays:
      return `${update.offset ?? 0} business days from the run date`;
    case AutomationDateUpdateMode.clear:
      return 'clear';
  }
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
