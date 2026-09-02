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
  [AutomationTriggerType.taskUnassignedFor]: $localize`:Name of an automation trigger event:Task is unassigned`,
  [AutomationTriggerType.taskChanged]: $localize`:Name of an automation trigger event:Task changes`,
  [AutomationTriggerType.taskDueDateApproaching]: $localize`:Name of an automation trigger event:Task due date approaches`,
  [AutomationTriggerType.taskCreated]: $localize`:Name of an automation trigger event:Task is created`,
  [AutomationTriggerType.taskOverdue]: $localize`:Name of an automation trigger event:Task becomes overdue`,
  [AutomationTriggerType.taskHasNoDueDate]: $localize`:Name of an automation trigger event:Task has no due date`,
  [AutomationTriggerType.taskInactiveFor]: $localize`:Name of an automation trigger event:Task remains inactive`,
  [AutomationTriggerType.sprintStarted]: $localize`:Name of an automation trigger event:Sprint starts`,
  [AutomationTriggerType.sprintCompleted]: $localize`:Name of an automation trigger event:Sprint completes`,
  [AutomationTriggerType.sprintEndingSoon]: $localize`:Name of an automation trigger event:Sprint end approaches`,
  [AutomationTriggerType.taskBlocked]: $localize`:Name of an automation trigger event:Task becomes blocked`,
  [AutomationTriggerType.taskUnblocked]: $localize`:Name of an automation trigger event:Task becomes unblocked`,
  [AutomationTriggerType.subtasksCompleted]: $localize`:Name of an automation trigger event:All subtasks complete`,
};

export const automationTriggerTypes: readonly AutomationTriggerType[] = [
  AutomationTriggerType.taskChanged,
  AutomationTriggerType.taskCreated,
  AutomationTriggerType.taskUnassignedFor,
  AutomationTriggerType.taskDueDateApproaching,
  AutomationTriggerType.taskOverdue,
  AutomationTriggerType.taskHasNoDueDate,
  AutomationTriggerType.taskInactiveFor,
  AutomationTriggerType.taskBlocked,
  AutomationTriggerType.taskUnblocked,
  AutomationTriggerType.subtasksCompleted,
  AutomationTriggerType.sprintStarted,
  AutomationTriggerType.sprintCompleted,
  AutomationTriggerType.sprintEndingSoon,
];

export const automationRunStatuses: readonly AutomationRunStatus[] = [
  AutomationRunStatus.succeeded,
  AutomationRunStatus.failed,
  AutomationRunStatus.skipped,
];

export const taskChangeFieldLabels: Record<TaskChangeField, string> = {
  [TaskChangeField.name]: $localize`:Task field that an automation can watch for changes:Name`,
  [TaskChangeField.description]: $localize`:Task field that an automation can watch for changes:Description`,
  [TaskChangeField.status]: $localize`:Task field that an automation can watch for changes:Status`,
  [TaskChangeField.assignees]: $localize`:Task field that an automation can watch for changes:Assignees`,
  [TaskChangeField.owner]: $localize`:Task field that an automation can watch for changes:Owner`,
  [TaskChangeField.priority]: $localize`:Task field that an automation can watch for changes:Priority`,
  [TaskChangeField.estimate]: $localize`:Task field that an automation can watch for changes:Estimate`,
  [TaskChangeField.dueDate]: $localize`:Task field that an automation can watch for changes:Due date`,
  [TaskChangeField.tags]: $localize`:Task field that an automation can watch for changes:Tags`,
  [TaskChangeField.startDate]: $localize`:Task field that an automation can watch for changes:Start date`,
  [TaskChangeField.sprint]: $localize`:Task field that an automation can watch for changes:Sprint`,
  [TaskChangeField.boardGroup]: $localize`:Task field that an automation can watch for changes:Board group`,
};

export const actionTypeLabels: Record<AutomationActionType, string> = {
  [AutomationActionType.notifyTaskAssignees]: $localize`:Name of an automation action:Notify task assignees`,
  [AutomationActionType.flagTask]: $localize`:Name of an automation action:Flag task`,
  [AutomationActionType.updateTask]: $localize`:Name of an automation action:Update task`,
  [AutomationActionType.addComment]: $localize`:Name of an automation action:Add comment`,
  [AutomationActionType.deleteTask]: $localize`:Name of an automation action:Delete task`,
  [AutomationActionType.createTask]: $localize`:Name of an automation action:Create task`,
  [AutomationActionType.manageTaskRelation]: $localize`:Name of an automation action:Manage task relation`,
};

export const conditionOperatorLabels: Record<
  AutomationConditionOperator,
  string
> = {
  [AutomationConditionOperator.any]: $localize`:Operator used in an automation condition:changed`,
  [AutomationConditionOperator.equals]: $localize`:Operator used in an automation condition:equals`,
  [AutomationConditionOperator.notEquals]: $localize`:Operator used in an automation condition:does not equal`,
  [AutomationConditionOperator.contains]: $localize`:Operator used in an automation condition:contains`,
  [AutomationConditionOperator.isEmpty]: $localize`:Operator used in an automation condition:is empty`,
  [AutomationConditionOperator.isNotEmpty]: $localize`:Operator used in an automation condition:is not empty`,
  [AutomationConditionOperator.added]: $localize`:Operator used in an automation condition:added`,
  [AutomationConditionOperator.removed]: $localize`:Operator used in an automation condition:removed`,
};

export const conditionGroupOperatorLabels: Record<
  AutomationConditionGroupOperator,
  string
> = {
  [AutomationConditionGroupOperator.all]: $localize`:Operator combining conditions in a group:All of`,
  [AutomationConditionGroupOperator.any]: $localize`:Operator combining conditions in a group:Any of`,
  [AutomationConditionGroupOperator.none]: $localize`:Operator combining conditions in a group:None of`,
};

export const notificationRecipientLabels: Record<
  AutomationNotificationRecipient,
  string
> = {
  [AutomationNotificationRecipient.assignees]: $localize`:Who an automation notifies:Assignees`,
  [AutomationNotificationRecipient.taskOwner]: $localize`:Who an automation notifies:Task owner`,
  [AutomationNotificationRecipient.triggeringUser]: $localize`:Who an automation notifies:Triggering user`,
  [AutomationNotificationRecipient.specificUsers]: $localize`:Who an automation notifies:Specific users`,
  [AutomationNotificationRecipient.projectMembers]: $localize`:Who an automation notifies:Project members`,
  [AutomationNotificationRecipient.workspaceRoles]: $localize`:Who an automation notifies:Workspace roles`,
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
  'task.name': $localize`:Example value shown in the notification preview:Fix login redirect`,
  'task.key': 'NETP-128',
  'task.status': $localize`:Example value shown in the notification preview:In Progress`,
  'task.priority': $localize`:Example value shown in the notification preview:High`,
  'task.startDate': '2026-08-03',
  'task.dueDate': '2026-08-10',
  'project.name': $localize`:Example value shown in the notification preview:Website Redesign`,
  'workspace.name': $localize`:Example value shown in the notification preview:Acme`,
  'rule.name': $localize`:Example value shown in the notification preview:Untitled automation`,
};

export const notificationRecipientPreviewLabels: Record<
  AutomationNotificationRecipient,
  string
> = {
  [AutomationNotificationRecipient.assignees]: $localize`:Who an automation notifies, shown in the preview:Everyone assigned to the task`,
  [AutomationNotificationRecipient.taskOwner]: $localize`:Who an automation notifies, shown in the preview:The task owner`,
  [AutomationNotificationRecipient.triggeringUser]: $localize`:Who an automation notifies, shown in the preview:The user whose change ran the rule`,
  [AutomationNotificationRecipient.specificUsers]: $localize`:Who an automation notifies, shown in the preview:Chosen users`,
  [AutomationNotificationRecipient.projectMembers]:
    "Everyone in the task's project",
  [AutomationNotificationRecipient.workspaceRoles]: $localize`:Who an automation notifies, shown in the preview:Chosen workspace roles`,
};

export type AutomationScopeKind = 'workspace' | 'project' | 'board' | 'sprint';

export const scopeKindLabels: Record<AutomationScopeKind, string> = {
  workspace: $localize`:Scope an automation can be limited to:Whole workspace`,
  project: $localize`:Scope an automation can be limited to:A single project`,
  board: $localize`:Scope an automation can be limited to:A single board`,
  sprint: $localize`:Scope an automation can be limited to:A single sprint`,
};

export const automationRunStatusLabels: Record<AutomationRunStatus, string> = {
  [AutomationRunStatus.succeeded]: $localize`:Outcome status of an automation run:Succeeded`,
  [AutomationRunStatus.failed]: $localize`:Outcome status of an automation run:Failed`,
  [AutomationRunStatus.skipped]: $localize`:Outcome status of an automation run:Skipped`,
};

export const automationActionResultStatusLabels: Record<
  AutomationActionResultStatus,
  string
> = {
  [AutomationActionResultStatus.pending]: $localize`:Outcome status of a single automation action:Pending`,
  [AutomationActionResultStatus.succeeded]: $localize`:Outcome status of a single automation action:Succeeded`,
  [AutomationActionResultStatus.failed]: $localize`:Outcome status of a single automation action:Failed`,
  [AutomationActionResultStatus.skipped]: $localize`:Outcome status of a single automation action:Skipped`,
  [AutomationActionResultStatus.scheduled]: $localize`:Outcome status of a single automation action:Scheduled`,
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
    : [
        $localize`:Stands in for the watched fields when none are chosen:selected fields`,
      ];
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
      return describeUnassignedForTrigger(trigger.durationDays ?? 1);
    case AutomationTriggerType.taskDueDateApproaching:
      return describeDueDateTrigger(trigger.durationDays ?? 0);
    case AutomationTriggerType.taskCreated:
      return $localize`:Automation trigger description:When a task is created`;
    case AutomationTriggerType.taskOverdue:
      return $localize`:Automation trigger description:When a task becomes overdue`;
    case AutomationTriggerType.taskHasNoDueDate:
      return $localize`:Automation trigger description:When a task has no due date`;
    case AutomationTriggerType.taskInactiveFor:
      return describeInactiveForTrigger(trigger.durationDays ?? 1);
    case AutomationTriggerType.sprintStarted:
      return $localize`:Automation trigger description:When a sprint starts, for every task in it`;
    case AutomationTriggerType.sprintCompleted:
      return $localize`:Automation trigger description:When a sprint completes, for every task in it`;
    case AutomationTriggerType.sprintEndingSoon:
      return describeSprintEndingTrigger(trigger.durationDays ?? 0);
    case AutomationTriggerType.taskBlocked:
      return $localize`:Automation trigger description:When a task becomes blocked by an incomplete task`;
    case AutomationTriggerType.taskUnblocked:
      return $localize`:Automation trigger description:When a task is no longer blocked`;
    case AutomationTriggerType.subtasksCompleted:
      return $localize`:Automation trigger description:When every subtask of a task is complete`;
  }
}

// Each whole sentence is one message. $localize cannot evaluate ICU, so the
// plural is a ternary; fr/de/es share English's one/other split, and a locale
// with more plural categories would need these moved into a template ICU.
function describeUnassignedForTrigger(durationDays: number): string {
  return durationDays === 1
    ? $localize`:Automation trigger description:When a task is unassigned for 1 day`
    : $localize`:Automation trigger description. DAYS is a count greater than one:When a task is unassigned for ${durationDays}:DAYS: days`;
}

function describeInactiveForTrigger(durationDays: number): string {
  return durationDays === 1
    ? $localize`:Automation trigger description:When a task has no activity for 1 day`
    : $localize`:Automation trigger description. DAYS is a count greater than one:When a task has no activity for ${durationDays}:DAYS: days`;
}

function describeSprintEndingTrigger(durationDays: number): string {
  if (durationDays === 0) {
    return $localize`:Automation trigger description:When a sprint ends today, for every task in it`;
  }

  return durationDays === 1
    ? $localize`:Automation trigger description:When a sprint ends in 1 day, for every task in it`
    : $localize`:Automation trigger description. DAYS is a count greater than one:When a sprint ends in ${durationDays}:DAYS: days, for every task in it`;
}

function describeDueDateTrigger(durationDays: number): string {
  if (durationDays === 0) {
    return $localize`:Automation trigger description:When a task is due today`;
  }

  return durationDays === 1
    ? $localize`:Automation trigger description:When a task is due in 1 day`
    : $localize`:Automation trigger description. DAYS is a count greater than one:When a task is due in ${durationDays}:DAYS: days`;
}

function describeTaskChangedTrigger(
  trigger: AutomationTrigger,
  statuses: Status[]
): string {
  const fields = trigger.fields?.length
    ? trigger.fields.map((field) => taskChangeFieldLabels[field])
    : [
        $localize`:Stands in for the watched fields when none are chosen:selected fields`,
      ];
  const fieldText = joinNaturalList(fields.map(toLowerText), 'or');

  if (trigger.conditionGroup) {
    return $localize`:Automation trigger description. FIELDS is a list of field names and CONDITIONS a condition summary:When a task's ${fieldText}:FIELDS: changes, if ${describeConditionGroup(trigger.conditionGroup, statuses)}:CONDITIONS:`;
  }

  return $localize`:Automation trigger description. FIELDS is a list of field names:When a task's ${fieldText}:FIELDS: changes`;
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
      return $localize`:Automation condition summary. FIELD is a field name:${field}:FIELD: has any change`;
    case AutomationConditionOperator.equals:
      return $localize`:Automation condition summary. FIELD is a field name and VALUE the compared value:${field}:FIELD: equals${value}:VALUE:`;
    case AutomationConditionOperator.notEquals:
      return $localize`:Automation condition summary. FIELD is a field name and VALUE the compared value:${field}:FIELD: does not equal${value}:VALUE:`;
    case AutomationConditionOperator.contains:
      return $localize`:Automation condition summary. FIELD is a field name and VALUE the compared value:${field}:FIELD: contains${value}:VALUE:`;
    case AutomationConditionOperator.isEmpty:
      return $localize`:Automation condition summary. FIELD is a field name:${field}:FIELD: is empty`;
    case AutomationConditionOperator.isNotEmpty:
      return $localize`:Automation condition summary. FIELD is a field name:${field}:FIELD: is not empty`;
    case AutomationConditionOperator.added:
      return $localize`:Automation condition summary. SUBJECT is the compared value, or the field name when there is none:${value.trim() || field}:SUBJECT: is added`;
    case AutomationConditionOperator.removed:
      return $localize`:Automation condition summary. SUBJECT is the compared value, or the field name when there is none:${value.trim() || field}:SUBJECT: is removed`;
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
        ? $localize`:Automation action summary. FLAG is the flag name:Flag the task as "${action.flagName}:FLAG:"`
        : $localize`:Automation action summary when the flag has no name:Flag the task`;
    case AutomationActionType.updateTask:
      return describeUpdateTaskAction(action, statuses);
    case AutomationActionType.addComment:
      return action.comment
        ? $localize`:Automation action summary. COMMENT is the comment text:Add comment: "${action.comment}:COMMENT:"`
        : $localize`:Automation action summary when the comment is empty:Add a comment`;
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
    return {
      text: $localize`:Shown when no notification recipients have been chosen:No users chosen yet`,
      isIncomplete: true,
    };
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
    return {
      text: $localize`:Shown when no workspace roles have been chosen:No workspace roles chosen yet`,
      isIncomplete: true,
    };
  }

  const labels = roles.map((role) => {
    return $localize`:Notification recipients. ROLE is a workspace role name:Everyone with the ${workspaceRoleLabels[role]}:ROLE: role`;
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

      return count === 1
        ? $localize`:Notification recipients, exactly one chosen user:1 chosen user`
        : $localize`:Notification recipients. COUNT is a number greater than one:${count}:COUNT: chosen users`;
    }

    return toLowerText(notificationRecipientLabels[recipient]);
  });

  return joinNaturalList(parts);
}

function describeRelationAction(action: AutomationAction): string {
  const isRemoval =
    action.relationOperation === AutomationRelationOperation.remove;

  if (isRemoval) {
    return $localize`:Automation action summary:Remove the configured task relations`;
  }

  return $localize`:Automation action summary:Link the task to the configured task`;
}

function describeCreateTaskAction(action: AutomationAction): string {
  const name = action.taskName?.trim();
  const created = name
    ? $localize`:Automation action summary. NAME is the task name:Create task "${name}:NAME:"`
    : $localize`:Automation action summary when the new task has no name:Create a task`;

  return action.linkRelationTypeId
    ? $localize`:Automation action summary. ACTION is a create-task phrase such as 'Create task "Fix bug"':${created}:ACTION: and link it`
    : created;
}

function describeDeleteTaskAction(action: AutomationAction): string {
  const amount = action.delayAmount ?? 0;

  if (amount <= 0) {
    return $localize`:Automation action summary:Delete the task`;
  }

  // The previous version stripped a trailing "s" from the enum key to make the
  // singular, which only works in English. Each unit and plurality is now its own
  // message. fr/de/es share English's one/other split.
  const unit = action.delayUnit ?? AutomationDelayUnit.minutes;
  const isSingle = amount === 1;

  if (unit === AutomationDelayUnit.hours) {
    return isSingle
      ? $localize`:Automation action summary:Delete the task after 1 hour`
      : $localize`:Automation action summary. COUNT is greater than one:Delete the task after ${amount}:COUNT: hours`;
  }

  if (unit === AutomationDelayUnit.days) {
    return isSingle
      ? $localize`:Automation action summary:Delete the task after 1 day`
      : $localize`:Automation action summary. COUNT is greater than one:Delete the task after ${amount}:COUNT: days`;
  }

  return isSingle
    ? $localize`:Automation action summary:Delete the task after 1 minute`
    : $localize`:Automation action summary. COUNT is greater than one:Delete the task after ${amount}:COUNT: minutes`;
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
    : $localize`:Automation action summary when no fields are set:Update the task`;
}

function describeDateUpdate(
  update: NonNullable<AutomationAction['startDate']>
): string {
  switch (update.mode) {
    case AutomationDateUpdateMode.absolute:
      return (
        update.date ||
        $localize`:Stands in for a date that has not been chosen yet:selected date`
      );
    case AutomationDateUpdateMode.relativeDays:
      return $localize`:Date offset in an automation summary. OFFSET is a number of days, which may be negative:${update.offset ?? 0}:OFFSET: calendar days from the run date`;
    case AutomationDateUpdateMode.relativeBusinessDays:
      return $localize`:Date offset in an automation summary. OFFSET is a number of working days, which may be negative:${update.offset ?? 0}:OFFSET: business days from the run date`;
    case AutomationDateUpdateMode.clear:
      return $localize`:Automation date update that empties the field:clear`;
  }
}

export function describeAutomationActions(
  actions: AutomationAction[],
  statuses: Status[] = []
): string {
  if (!actions.length) {
    return $localize`:Shown when an automation has no actions:No actions configured`;
  }

  return actions
    .map((action) => describeAutomationAction(action, statuses))
    .join(', then ');
}

export function describeAutomationRule(
  trigger: AutomationTrigger,
  actions: AutomationAction[],
  statuses: Status[] = []
): string {
  return $localize`:Whole automation rule summary. TRIGGER is the trigger sentence and ACTIONS the action sentence:${describeAutomationTrigger(trigger, statuses)}:TRIGGER:, ${describeAutomationActions(actions, statuses)}:ACTIONS:.`;
}

export function statusLabel(
  statusId: number | null | undefined,
  statuses: Status[] = []
): string {
  if (!isNotNullOrUndefined(statusId)) {
    return $localize`:Stands in for a status that has not been chosen:a selected status`;
  }

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
  if (!isNotNullOrUndefined(entityType)) {
    return $localize`:The workspace entity, used when no narrower scope applies:Workspace`;
  }

  const label = entityTypeToString(entityType);
  return entityId ? `${label} #${entityId}` : label;
}
