import { taskPriorityLabels } from '@core/enums/task-priority';
import { workspaceRoleLabels } from '@core/enums/workspace-role';
import { WorkspaceAppUser } from '@core/models/appuser';
import { Status } from '@core/models/status';
import { isNotNullOrUndefined } from '@core/util/nullish';
import { joinNaturalList, toLowerText } from '@core/util/strings';
import { TextSegment } from '@static/components/segment-text/segment-text.component';
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
  [AutomationTriggerType.pullRequestOpened]: $localize`:Name of an automation trigger event:Pull request opened`,
  [AutomationTriggerType.pullRequestMerged]: $localize`:Name of an automation trigger event:Pull request merged`,
  [AutomationTriggerType.checksFailed]: $localize`:Name of an automation trigger event:Checks fail`,
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
  AutomationTriggerType.pullRequestOpened,
  AutomationTriggerType.pullRequestMerged,
  AutomationTriggerType.checksFailed,
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

export const actionTypeDescriptions: Record<AutomationActionType, string> = {
  [AutomationActionType.notifyTaskAssignees]: $localize`:Description of an automation action:Sends an in-app notification.`,
  [AutomationActionType.flagTask]: $localize`:Description of an automation action:Marks the task for attention.`,
  [AutomationActionType.updateTask]: $localize`:Description of an automation action:Changes fields on the task.`,
  [AutomationActionType.addComment]: $localize`:Description of an automation action:Posts a comment as the service account.`,
  [AutomationActionType.deleteTask]: $localize`:Description of an automation action:Removes the task, optionally after a delay.`,
  [AutomationActionType.createTask]: $localize`:Description of an automation action:Adds a new task.`,
  [AutomationActionType.manageTaskRelation]: $localize`:Description of an automation action:Adds or removes a task relation.`,
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

export function toTextSegments(
  segments: AutomationCopySegment[],
  statuses: Status[] = []
): TextSegment[] {
  return segments.map((segment) => {
    if (segment.type === 'text') return segment;

    const status = statuses.find((item) => item.id === segment.statusId);

    return {
      type: 'status',
      name: status?.name ?? statusLabel(segment.statusId, statuses),
      color: status?.color,
      category: status?.category ?? null,
    };
  });
}

export function describeAutomationTriggerSegments(
  trigger: AutomationTrigger,
  statuses: Status[] = []
): AutomationCopySegment[] {
  const conditionGroup =
    trigger.type === AutomationTriggerType.taskChanged
      ? trigger.conditionGroup
      : null;

  if (!conditionGroup) {
    return textSegments(describeAutomationTrigger(trigger, statuses));
  }

  return fillSlots(describeTaskChangedWithConditions(trigger, slot(0)), [
    describeConditionGroupSegments(conditionGroup),
  ]);
}

export function describeAutomationActionSegments(
  action: AutomationAction,
  statuses: Status[] = []
): AutomationCopySegment[] {
  const statusId =
    action.type === AutomationActionType.updateTask ? action.statusId : null;
  const hasStatusUpdate = isNotNullOrUndefined(statusId);

  if (!hasStatusUpdate) {
    return textSegments(describeAutomationAction(action, statuses));
  }

  return fillSlots(describeUpdateTaskAction(action, slot(0)), [
    [{ type: 'status', statusId }],
  ]);
}

export function describeAutomationConditionsSegments(
  trigger: AutomationTrigger
): AutomationCopySegment[] {
  if (trigger.conditionGroup) {
    return describeConditionGroupSegments(trigger.conditionGroup);
  }

  return textSegments(
    $localize`:Shown when an automation has no conditions:Every matching task continues`
  );
}

export function countAutomationConditions(
  group: AutomationConditionGroup | null | undefined
): number {
  if (!group) return 0;

  return group.groups.reduce(
    (total, nested) => total + countAutomationConditions(nested),
    group.conditions.length
  );
}

function describeFieldConditionSegments(
  condition: AutomationFieldCondition
): AutomationCopySegment[] {
  const value = conditionValueSegments(condition);
  const message = describeFieldConditionMessage(
    condition,
    value.length ? slot(0) : null
  );

  return fillSlots(message, [value]);
}

function conditionValueSegments(
  condition: AutomationFieldCondition
): AutomationCopySegment[] {
  if (!condition.value) return [];

  const statusId = Number(condition.value);
  const isStatusValue =
    condition.field === TaskChangeField.status && Number.isInteger(statusId);

  if (isStatusValue) {
    return [{ type: 'status', statusId }];
  }

  return textSegments(quotedValue(condition.value));
}

function describeConditionGroupSegments(
  group: AutomationConditionGroup
): AutomationCopySegment[] {
  const members = [
    ...group.conditions.map(describeFieldConditionSegments),
    ...group.groups.map((nestedGroup) => [
      ...textSegments('('),
      ...describeConditionGroupSegments(nestedGroup),
      ...textSegments(')'),
    ]),
  ];
  const conjunction =
    group.operator === AutomationConditionGroupOperator.all ? 'and' : 'or';
  const segments = joinSegmentList(members, conjunction);

  if (group.operator !== AutomationConditionGroupOperator.none) {
    return segments;
  }

  return fillSlots(
    $localize`:Automation condition summary. CONDITIONS is a list of conditions none of which may match:none of (${slot(0)}:CONDITIONS:)`,
    [segments]
  );
}

function toConditionText(
  segments: AutomationCopySegment[],
  statuses: Status[]
): string {
  return segments
    .map((segment) => {
      if (segment.type === 'text') return segment.text;

      return quotedValue(statusLabel(segment.statusId, statuses));
    })
    .join('');
}

function quotedValue(value: string): string {
  return `“${value}”`;
}

function slot(index: number): string {
  return `\uE000${index}\uE001`;
}

function fillSlots(
  message: string,
  slots: AutomationCopySegment[][]
): AutomationCopySegment[] {
  return message.split(/\uE000(\d+)\uE001/).flatMap((part, index) => {
    const isSlot = index % 2 === 1;

    if (isSlot) return slots[Number(part)] ?? [];

    return part ? textSegments(part) : [];
  });
}

function joinSegmentList(
  groups: AutomationCopySegment[][],
  conjunction: 'and' | 'or'
): AutomationCopySegment[] {
  const message = joinNaturalList(
    groups.map((_, index) => slot(index)),
    conjunction
  );

  return fillSlots(message, groups);
}

function textSegments(text: string): AutomationCopySegment[] {
  return [{ type: 'text', text }];
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
    case AutomationTriggerType.pullRequestOpened:
      return describePullRequestOpenedTrigger(trigger.targetBranches ?? []);
    case AutomationTriggerType.pullRequestMerged:
      return describePullRequestMergedTrigger(trigger.targetBranches ?? []);
    case AutomationTriggerType.checksFailed:
      return $localize`:Automation trigger description:When checks fail on a task's linked code`;
  }
}

function describePullRequestOpenedTrigger(targetBranches: string[]): string {
  const branches = targetBranches.join(', ');

  return branches
    ? $localize`:Automation trigger description. BRANCHES is a comma-separated list of branch names:When a pull request into ${branches}:BRANCHES: is opened for a task`
    : $localize`:Automation trigger description:When a pull request is opened for a task`;
}

function describePullRequestMergedTrigger(targetBranches: string[]): string {
  const branches = targetBranches.join(', ');

  return branches
    ? $localize`:Automation trigger description. BRANCHES is a comma-separated list of branch names:When a task's pull request is merged into ${branches}:BRANCHES:`
    : $localize`:Automation trigger description:When a task's pull request is merged`;
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
  if (trigger.conditionGroup) {
    const conditions = describeConditionGroup(trigger.conditionGroup, statuses);

    return describeTaskChangedWithConditions(trigger, conditions);
  }

  return $localize`:Automation trigger description. FIELDS is a list of field names:When a task's ${watchedFieldsText(trigger)}:FIELDS: changes`;
}

function describeTaskChangedWithConditions(
  trigger: AutomationTrigger,
  conditions: string
): string {
  return $localize`:Automation trigger description. FIELDS is a list of field names and CONDITIONS a condition summary:When a task's ${watchedFieldsText(trigger)}:FIELDS: changes, if ${conditions}:CONDITIONS:`;
}

function watchedFieldsText(trigger: AutomationTrigger): string {
  const fields = trigger.fields?.length
    ? trigger.fields.map((field) => taskChangeFieldLabels[field])
    : [
        $localize`:Stands in for the watched fields when none are chosen:selected fields`,
      ];

  return joinNaturalList(fields.map(toLowerText), 'or');
}

function describeConditionGroup(
  group: AutomationConditionGroup,
  statuses: Status[]
): string {
  return toConditionText(describeConditionGroupSegments(group), statuses);
}

function describeFieldConditionMessage(
  condition: AutomationFieldCondition,
  conditionValue: string | null
): string {
  const field = toLowerText(taskChangeFieldLabels[condition.field]);
  const value = conditionValue ? ` ${conditionValue}` : '';

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
      return $localize`:Automation condition summary. SUBJECT is the compared value, or the field name when there is none:${conditionValue ?? field}:SUBJECT: is added`;
    case AutomationConditionOperator.removed:
      return $localize`:Automation condition summary. SUBJECT is the compared value, or the field name when there is none:${conditionValue ?? field}:SUBJECT: is removed`;
  }
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
      return describeUpdateTaskAction(
        action,
        statusLabel(action.statusId, statuses)
      );
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
    ? $localize`:Automation action summary. AUDIENCE is who is notified and MESSAGE the notification text:Notify ${audience}:AUDIENCE:: "${action.message}:MESSAGE:"`
    : describeNotifyVerb(audience);
}

function describeNotifyVerb(audience: string): string {
  return $localize`:Automation action summary. AUDIENCE is who is notified:Notify ${audience}:AUDIENCE:`;
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

    return (
      user?.displayName ??
      $localize`:Stands in for a user who is no longer in the workspace:Unknown user`
    );
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
        : $localize`:Notification recipients when no workspace roles are chosen:workspace roles`;
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
  status: string
): string {
  const updates = describeTaskUpdates(action, status);

  return updates.length
    ? $localize`:Automation action summary. UPDATES is a list of task changes:Update the task: ${joinNaturalList(updates)}:UPDATES:`
    : $localize`:Automation action summary when no fields are set:Update the task`;
}

function describeTaskUpdates(
  action: AutomationAction,
  status: string
): string[] {
  const updates: string[] = [];

  if (isNotNullOrUndefined(action.statusId)) {
    updates.push(
      $localize`:Task change in an automation summary. STATUS is a status name:status to ${status}:STATUS:`
    );
  }

  if (isNotNullOrUndefined(action.priority)) {
    updates.push(
      $localize`:Task change in an automation summary. PRIORITY is a priority name:priority to ${taskPriorityLabels[action.priority]}:PRIORITY:`
    );
  }

  if (action.taskName) {
    updates.push(
      $localize`:Task change in an automation summary. NAME is the new task name:name to "${action.taskName}:NAME:"`
    );
  }

  if (action.clearDescription) {
    updates.push(
      $localize`:Task change in an automation summary:clear the description`
    );
  } else if (action.taskDescription) {
    updates.push(
      $localize`:Task change in an automation summary:set the description`
    );
  }

  if (action.clearOwner) {
    updates.push(
      $localize`:Task change in an automation summary:clear the owner`
    );
  } else if (action.ownerId) {
    updates.push(
      $localize`:Task change in an automation summary:set the owner`
    );
  }

  if (action.assigneeIds !== null && action.assigneeIds !== undefined) {
    updates.push(
      describeAssigneeUpdate(action.assigneeIds, !!action.appendAssignees)
    );
  }

  if (action.addTags?.length) {
    updates.push(
      $localize`:Task change in an automation summary. TAGS is a list of tag names:add tags ${action.addTags.join(', ')}:TAGS:`
    );
  }

  if (action.removeTags?.length) {
    updates.push(
      $localize`:Task change in an automation summary. TAGS is a list of tag names:remove tags ${action.removeTags.join(', ')}:TAGS:`
    );
  }

  if (action.startDate) {
    updates.push(
      $localize`:Task change in an automation summary. DATE_UPDATE describes the new date:start date: ${describeDateUpdate(action.startDate)}:DATE_UPDATE:`
    );
  }

  if (action.dueDate) {
    updates.push(
      $localize`:Task change in an automation summary. DATE_UPDATE describes the new date:due date: ${describeDateUpdate(action.dueDate)}:DATE_UPDATE:`
    );
  }

  if (action.clearEstimate) {
    updates.push(
      $localize`:Task change in an automation summary:clear the estimate`
    );
  } else if (
    isNotNullOrUndefined(action.estimateType) ||
    isNotNullOrUndefined(action.estimateValue)
  ) {
    updates.push(
      $localize`:Task change in an automation summary:set the estimate`
    );
  }

  if (action.clearSprint) {
    updates.push(
      $localize`:Task change in an automation summary:move to the backlog`
    );
  } else if (isNotNullOrUndefined(action.sprintId)) {
    updates.push(
      $localize`:Task change in an automation summary:move to a sprint`
    );
  }

  if (isNotNullOrUndefined(action.boardGroupId)) {
    updates.push(
      $localize`:Task change in an automation summary:move to a board group`
    );
  }

  return updates;
}

function describeAssigneeUpdate(
  assigneeIds: string[],
  append: boolean
): string {
  const count = assigneeIds.length;

  if (append) {
    return count === 1
      ? $localize`:Task change in an automation summary:add 1 assignee`
      : $localize`:Task change in an automation summary. COUNT is greater than one:add ${count}:COUNT: assignees`;
  }

  if (!count) {
    return $localize`:Task change in an automation summary:unassign everyone`;
  }

  return count === 1
    ? $localize`:Task change in an automation summary:replace the assignees with 1 user`
    : $localize`:Task change in an automation summary. COUNT is greater than one:replace the assignees with ${count}:COUNT: users`;
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

export function statusLabel(
  statusId: number | null | undefined,
  statuses: Status[] = []
): string {
  if (!isNotNullOrUndefined(statusId)) {
    return $localize`:Stands in for a status that has not been chosen:a selected status`;
  }

  return (
    statuses.find((status) => status.id === statusId)?.name ??
    $localize`:Stands in for a status that no longer exists. ID is its number:status #${statusId}:ID:`
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

export interface AutomationActionChip {
  verb: string;
  detail: string;
  full: string;
}

export function describeAutomationTriggerChip(
  trigger: AutomationTrigger
): string {
  if (trigger.type !== AutomationTriggerType.taskChanged) {
    return triggerTypeLabels[trigger.type];
  }

  if (!trigger.fields?.length) {
    return triggerTypeLabels[trigger.type];
  }

  const fields = trigger.fields.map((field) => taskChangeFieldLabels[field]);
  const fieldText = joinNaturalList(
    fields.map((field, index) => (index === 0 ? field : toLowerText(field))),
    'or'
  );

  return $localize`:Short automation trigger label. FIELDS is a list of field names:${fieldText}:FIELDS: changes`;
}

export function describeAutomationConditionChip(
  trigger: AutomationTrigger,
  statuses: Status[] = []
): string | null {
  if (!trigger.conditionGroup) return null;

  const description = describeConditionGroup(trigger.conditionGroup, statuses);

  return description.charAt(0).toUpperCase() + description.slice(1);
}

export function describeAutomationActionChip(
  action: AutomationAction,
  statuses: Status[] = []
): AutomationActionChip {
  return {
    verb: actionChipVerb(action),
    detail: actionChipDetail(action, statuses),
    full: describeAutomationAction(action, statuses),
  };
}

function actionChipVerb(action: AutomationAction): string {
  if (action.type === AutomationActionType.notifyTaskAssignees) {
    return describeNotifyVerb(describeNotificationAudience(action));
  }

  return actionTypeLabels[action.type];
}

function actionChipDetail(
  action: AutomationAction,
  statuses: Status[]
): string {
  switch (action.type) {
    case AutomationActionType.notifyTaskAssignees:
      return quoted(action.message);
    case AutomationActionType.flagTask:
      return quoted(action.flagName);
    case AutomationActionType.updateTask:
      return joinNaturalList(
        describeTaskUpdates(action, statusLabel(action.statusId, statuses))
      );
    case AutomationActionType.addComment:
      return quoted(action.comment);
    case AutomationActionType.deleteTask:
      return describeDeleteDelay(action);
    case AutomationActionType.createTask:
      return quoted(action.taskName?.trim());
    case AutomationActionType.manageTaskRelation:
      return action.relationOperation === AutomationRelationOperation.remove
        ? $localize`:Short automation action detail:remove configured relations`
        : $localize`:Short automation action detail:link configured task`;
  }
}

function describeDeleteDelay(action: AutomationAction): string {
  const amount = action.delayAmount ?? 0;

  if (amount <= 0) return '';

  const unit = action.delayUnit ?? AutomationDelayUnit.minutes;
  const isSingle = amount === 1;

  if (unit === AutomationDelayUnit.hours) {
    return isSingle
      ? $localize`:Short automation action detail:after 1 hour`
      : $localize`:Short automation action detail. COUNT is greater than one:after ${amount}:COUNT: hours`;
  }

  if (unit === AutomationDelayUnit.days) {
    return isSingle
      ? $localize`:Short automation action detail:after 1 day`
      : $localize`:Short automation action detail. COUNT is greater than one:after ${amount}:COUNT: days`;
  }

  return isSingle
    ? $localize`:Short automation action detail:after 1 minute`
    : $localize`:Short automation action detail. COUNT is greater than one:after ${amount}:COUNT: minutes`;
}

function quoted(text: string | null | undefined): string {
  return text ? `"${text}"` : '';
}
