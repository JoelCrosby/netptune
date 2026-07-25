import { Service } from '@angular/core';
import { WorkspaceRole } from '@core/enums/workspace-role';
import {
  AutomationAction,
  AutomationActionRequest,
  AutomationActionType,
  AutomationConditionGroup,
  AutomationDelayUnit,
  AutomationConditionOperator,
  AutomationDateUpdate,
  AutomationDateUpdateMode,
  AutomationFieldCondition,
  AutomationNotificationRecipient,
  AutomationRuleRequest,
  AutomationTrigger,
  AutomationTriggerType,
} from '../models/automation.models';
import { messageVariables } from '../models/automation-copy';

export interface AutomationRuleDraft {
  name: string;
  isEnabled: boolean;
  executionUserId: string | null;
  trigger: AutomationTrigger;
  actions: AutomationAction[];
}

export interface AutomationRuleRequestBuildResult {
  request: AutomationRuleRequest | null;
  error: string | null;
}

@Service()
export class AutomationRuleRequestBuilder {
  build(draft: AutomationRuleDraft): AutomationRuleRequestBuildResult {
    const actions = draft.actions.map(toActionRequest);
    const error =
      validateName(draft.name) ??
      validateExecutionUser(draft.executionUserId) ??
      validateActions(actions) ??
      validateTrigger(draft.trigger);

    if (error) {
      return { request: null, error };
    }

    if (!draft.executionUserId) {
      throw Error('draft.executionUserId not defined.');
    }

    return {
      request: {
        name: draft.name.trim(),
        isEnabled: draft.isEnabled,
        executionUserId: draft.executionUserId,
        trigger: draft.trigger,
        actions,
      },
      error: null,
    };
  }
}

function validateExecutionUser(executionUserId: string | null): string | null {
  return executionUserId ? null : 'Choose an automation service account.';
}

function validateName(name: string): string | null {
  return name.trim() ? null : 'Automation name is required.';
}

function validateActions(actions: AutomationActionRequest[]): string | null {
  if (!actions.length) return 'Add at least one action.';

  const hasUnnamedFlag = actions.some((action) => {
    return action.type === AutomationActionType.flagTask && !action.flagName;
  });

  if (hasUnnamedFlag) {
    return 'Flag actions need a flag name.';
  }

  const hasEmptyComment = actions.some((action) => {
    return action.type === AutomationActionType.addComment && !action.comment;
  });

  if (hasEmptyComment) {
    return 'Add comment actions need a comment.';
  }

  const notifyActions = actions.filter((action) => {
    return action.type === AutomationActionType.notifyTaskAssignees;
  });

  const hasNoRecipients = notifyActions.some((action) => {
    return !action.recipients?.length;
  });

  if (hasNoRecipients) {
    return 'Notify actions need at least one recipient.';
  }

  const hasNoRecipientUsers = notifyActions.some((action) => {
    return (
      action.recipients?.includes(
        AutomationNotificationRecipient.specificUsers
      ) && !action.recipientUserIds?.length
    );
  });

  if (hasNoRecipientUsers) {
    return 'Choose at least one user to notify.';
  }

  const hasNoRecipientRoles = notifyActions.some((action) => {
    return (
      action.recipients?.includes(
        AutomationNotificationRecipient.workspaceRoles
      ) && !action.recipientRoles?.length
    );
  });

  if (hasNoRecipientRoles) {
    return 'Choose at least one workspace role to notify.';
  }

  const invalidMessageVariables = notifyActions
    .map((action) => unknownMessageVariables(action.message))
    .find((variables) => variables.length);

  if (invalidMessageVariables) {
    return `Unknown message variables: ${invalidMessageVariables.join(', ')}.`;
  }

  const createTaskActions = actions.filter((action) => {
    return action.type === AutomationActionType.createTask;
  });

  const hasUnnamedCreateTask = createTaskActions.some((action) => {
    return !action.taskName;
  });

  if (hasUnnamedCreateTask) {
    return 'Create task actions need a task name.';
  }

  const hasConflictingAssignees = createTaskActions.some((action) => {
    return !!action.copyAssignees && !!action.assigneeIds?.length;
  });

  if (hasConflictingAssignees) {
    return 'Create task actions cannot copy and set assignees at the same time.';
  }

  const invalidCreateTaskDate = createTaskActions.some((action) => {
    return [action.startDate, action.dueDate].some(isIncompleteDateUpdate);
  });

  if (invalidCreateTaskDate) {
    return 'Complete each configured task date.';
  }

  const invalidCreateTaskVariables = createTaskActions
    .flatMap((action) => [action.taskName, action.taskDescription])
    .map(unknownMessageVariables)
    .find((variables) => variables.length);

  if (invalidCreateTaskVariables) {
    return `Unknown message variables: ${invalidCreateTaskVariables.join(', ')}.`;
  }

  const hasEmptyTaskUpdate = actions.some((action) => {
    return (
      action.type === AutomationActionType.updateTask && !hasTaskUpdate(action)
    );
  });

  if (hasEmptyTaskUpdate) {
    return 'Task update actions need at least one field update.';
  }

  const invalidDateUpdate = actions.some((action) => {
    return (
      action.type === AutomationActionType.updateTask &&
      [action.startDate, action.dueDate].some(isIncompleteDateUpdate)
    );
  });

  if (invalidDateUpdate) {
    return 'Complete each configured task date update.';
  }

  const invalidDelay = actions.some((action) => {
    return (
      action.type === AutomationActionType.deleteTask &&
      (!Number.isInteger(action.delayAmount) ||
        action.delayAmount === null ||
        action.delayAmount === undefined ||
        action.delayAmount < 0)
    );
  });

  if (invalidDelay) {
    return 'Delete task action delay must be a whole number of 0 or more.';
  }

  const excessiveDelay = actions.some((action) => {
    return (
      action.type === AutomationActionType.deleteTask &&
      toDelayMinutes(action) > 525600
    );
  });

  if (excessiveDelay) {
    return 'Delete task action delay cannot exceed 365 days.';
  }

  return null;
}

function isIncompleteDateUpdate(
  date: AutomationDateUpdate | null | undefined
): boolean {
  if (date === null || date === undefined) return false;

  const isMissingDate =
    date.mode === AutomationDateUpdateMode.absolute && !date.date;
  const isMissingOffset =
    (date.mode === AutomationDateUpdateMode.relativeDays ||
      date.mode === AutomationDateUpdateMode.relativeBusinessDays) &&
    !Number.isInteger(date.offset);

  return isMissingDate || isMissingOffset;
}

function toDelayMinutes(action: AutomationActionRequest): number {
  const amount = action.delayAmount ?? 0;

  return amount * toDelayMultiplier(action.delayUnit);
}

function toDelayMultiplier(
  unit: AutomationDelayUnit | null | undefined
): number {
  if (unit === AutomationDelayUnit.days) return 1440;

  if (unit === AutomationDelayUnit.hours) return 60;

  return 1;
}

function validateTrigger(trigger: AutomationTrigger): string | null {
  if (trigger.type === AutomationTriggerType.taskChanged) {
    if (!trigger.fields?.length) {
      return 'Choose at least one task field to watch.';
    }
  }

  const hasInvalidUnassignedDuration =
    trigger.type === AutomationTriggerType.taskUnassignedFor &&
    !isDurationInRange(trigger.durationDays, 1);

  if (hasInvalidUnassignedDuration) {
    return 'Unassigned duration must be 1 to 365 days.';
  }

  const hasInvalidDueDateLeadTime =
    trigger.type === AutomationTriggerType.taskDueDateApproaching &&
    !isDurationInRange(trigger.durationDays, 0);

  if (hasInvalidDueDateLeadTime) {
    return 'Due-date lead time must be 0 to 365 days.';
  }

  const hasInvalidInactiveDuration =
    trigger.type === AutomationTriggerType.taskInactiveFor &&
    !isDurationInRange(trigger.durationDays, 1);

  if (hasInvalidInactiveDuration) {
    return 'Inactive duration must be 1 to 365 days.';
  }

  if (trigger.conditionGroup) {
    const supportsChangeOperators =
      trigger.type === AutomationTriggerType.taskChanged;
    const groupError = validateConditionGroup(
      trigger.conditionGroup,
      supportsChangeOperators
    );

    if (groupError) return groupError;
  }

  return null;
}

function validateConditionGroup(
  group: AutomationConditionGroup,
  supportsChangeOperators = true,
  depth = 1,
  count = { value: 0 }
): string | null {
  if (depth > 4) return 'Condition groups can be nested up to 4 levels.';

  if (!group.conditions.length && !group.groups.length) {
    return 'Add at least one condition to each condition group.';
  }

  count.value += group.conditions.length;

  if (count.value > 50) return 'Automations can have up to 50 conditions.';

  const hasInvalidCondition = group.conditions.some((condition) =>
    isInvalidCondition(condition, supportsChangeOperators)
  );

  if (hasInvalidCondition) {
    return 'Complete each field condition before saving.';
  }

  for (const nestedGroup of group.groups) {
    const error = validateConditionGroup(
      nestedGroup,
      supportsChangeOperators,
      depth + 1,
      count
    );

    if (error) return error;
  }

  return null;
}

function isInvalidCondition(
  condition: AutomationFieldCondition,
  supportsChangeOperators: boolean
): boolean {
  const isChangeOperator =
    condition.operator === AutomationConditionOperator.any ||
    condition.operator === AutomationConditionOperator.added ||
    condition.operator === AutomationConditionOperator.removed;
  const requiresValue =
    condition.operator === AutomationConditionOperator.equals ||
    condition.operator === AutomationConditionOperator.notEquals ||
    condition.operator === AutomationConditionOperator.contains;

  return (
    (isChangeOperator && !supportsChangeOperators) ||
    (requiresValue && !condition.value?.trim())
  );
}

function isDurationInRange(
  durationDays: number | null | undefined,
  minimum: number
): durationDays is number {
  return (
    Number.isInteger(durationDays) &&
    durationDays !== null &&
    durationDays !== undefined &&
    durationDays >= minimum &&
    durationDays <= 365
  );
}

type NotifyFields = Required<
  Pick<
    AutomationActionRequest,
    'message' | 'recipients' | 'recipientUserIds' | 'recipientRoles'
  >
>;

type FlagFields = Required<
  Pick<AutomationActionRequest, 'flagName' | 'flagDescription'>
>;

type TaskUpdateFields = Required<
  Pick<
    AutomationActionRequest,
    | 'statusId'
    | 'priority'
    | 'taskName'
    | 'taskDescription'
    | 'clearDescription'
    | 'ownerId'
    | 'clearOwner'
    | 'assigneeIds'
    | 'addTags'
    | 'removeTags'
    | 'startDate'
    | 'dueDate'
    | 'estimateType'
    | 'estimateValue'
    | 'clearEstimate'
    | 'sprintId'
    | 'clearSprint'
    | 'boardGroupId'
  >
>;

type CreateTaskFields = Required<
  Pick<AutomationActionRequest, 'copyAssignees' | 'linkRelationTypeId'>
>;

type DeleteFields = Required<
  Pick<AutomationActionRequest, 'delayAmount' | 'delayUnit'>
>;

function toActionRequest(action: AutomationAction): AutomationActionRequest {
  return {
    type: action.type,
    ...toNotifyFields(action),
    comment: toComment(action),
    ...toFlagFields(action),
    ...toTaskUpdateFields(action),
    ...toCreateTaskFields(action),
    ...toDeleteFields(action),
  };
}

function toNotifyFields(action: AutomationAction): NotifyFields {
  if (action.type !== AutomationActionType.notifyTaskAssignees) {
    return {
      message: null,
      recipients: [],
      recipientUserIds: [],
      recipientRoles: [],
    };
  }

  const recipients = toRecipients(action);

  return {
    message: toTrimmedOrNull(action.message),
    recipients,
    recipientUserIds: toRecipientUserIds(action, recipients),
    recipientRoles: toRecipientRoles(action, recipients),
  };
}

function toRecipients(
  action: AutomationAction
): AutomationNotificationRecipient[] {
  if (!action.recipients?.length) {
    return [AutomationNotificationRecipient.assignees];
  }

  return action.recipients;
}

function toRecipientUserIds(
  action: AutomationAction,
  recipients: AutomationNotificationRecipient[]
): string[] {
  if (!recipients.includes(AutomationNotificationRecipient.specificUsers)) {
    return [];
  }

  return action.recipientUserIds ?? [];
}

function toRecipientRoles(
  action: AutomationAction,
  recipients: AutomationNotificationRecipient[]
): WorkspaceRole[] {
  if (!recipients.includes(AutomationNotificationRecipient.workspaceRoles)) {
    return [];
  }

  return action.recipientRoles ?? [];
}

function toComment(action: AutomationAction): string | null {
  if (action.type !== AutomationActionType.addComment) return null;

  return toTrimmedOrNull(action.comment);
}

function toFlagFields(action: AutomationAction): FlagFields {
  if (action.type !== AutomationActionType.flagTask) {
    return { flagName: null, flagDescription: null };
  }

  return {
    flagName: toTrimmedOrNull(action.flagName),
    flagDescription: toTrimmedOrNull(action.flagDescription),
  };
}

function toTaskUpdateFields(action: AutomationAction): TaskUpdateFields {
  const isCreateTask = action.type === AutomationActionType.createTask;

  if (isCreateTask) {
    return {
      ...emptyTaskUpdateFields(),
      statusId: action.statusId ?? null,
      priority: action.priority ?? null,
      taskName: toTrimmedOrNull(action.taskName),
      taskDescription: toTrimmedOrNull(action.taskDescription),
      assigneeIds: action.assigneeIds ?? [],
      addTags: action.addTags ?? [],
      startDate: action.startDate ?? null,
      dueDate: action.dueDate ?? null,
      sprintId: action.sprintId ?? null,
      boardGroupId: action.boardGroupId ?? null,
    };
  }

  if (action.type !== AutomationActionType.updateTask) {
    return emptyTaskUpdateFields();
  }

  return {
    statusId: action.statusId ?? null,
    priority: action.priority ?? null,
    taskName: toTrimmedOrNull(action.taskName),
    taskDescription: toTrimmedOrNull(action.taskDescription),
    clearDescription: !!action.clearDescription,
    ownerId: action.ownerId ?? null,
    clearOwner: !!action.clearOwner,
    assigneeIds: action.assigneeIds ?? null,
    addTags: action.addTags ?? [],
    removeTags: action.removeTags ?? [],
    startDate: action.startDate ?? null,
    dueDate: action.dueDate ?? null,
    estimateType: action.estimateType ?? null,
    estimateValue: action.estimateValue ?? null,
    clearEstimate: !!action.clearEstimate,
    sprintId: action.sprintId ?? null,
    clearSprint: !!action.clearSprint,
    boardGroupId: action.boardGroupId ?? null,
  };
}

function toCreateTaskFields(action: AutomationAction): CreateTaskFields {
  if (action.type !== AutomationActionType.createTask) {
    return {
      copyAssignees: false,
      linkRelationTypeId: null,
    };
  }

  const copyAssignees = !!action.copyAssignees;

  return {
    copyAssignees,
    linkRelationTypeId: action.linkRelationTypeId ?? null,
  };
}

function emptyTaskUpdateFields(): TaskUpdateFields {
  return {
    statusId: null,
    priority: null,
    taskName: null,
    taskDescription: null,
    clearDescription: false,
    ownerId: null,
    clearOwner: false,
    assigneeIds: null,
    addTags: [],
    removeTags: [],
    startDate: null,
    dueDate: null,
    estimateType: null,
    estimateValue: null,
    clearEstimate: false,
    sprintId: null,
    clearSprint: false,
    boardGroupId: null,
  };
}

function toDeleteFields(action: AutomationAction): DeleteFields {
  if (action.type !== AutomationActionType.deleteTask) {
    return { delayAmount: null, delayUnit: null };
  }

  return {
    delayAmount: action.delayAmount ?? 0,
    delayUnit: action.delayUnit ?? null,
  };
}

function toTrimmedOrNull(value: string | null | undefined): string | null {
  return value?.trim() || null;
}

function unknownMessageVariables(message?: string | null): string[] {
  if (!message) return [];

  const matches = message.match(/\{\{([^{}]*)\}\}/g) ?? [];
  const used = matches.map((match) => match.slice(2, -2).trim());

  return [
    ...new Set(
      used.filter((variable) => {
        return !messageVariables.some(
          (known) => known.toLowerCase() === variable.toLowerCase()
        );
      })
    ),
  ];
}

function hasTaskUpdate(action: AutomationActionRequest): boolean {
  return (
    action.statusId !== null ||
    action.priority !== null ||
    action.taskName !== null ||
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
    action.boardGroupId !== null
  );
}
