import {
  AutomationAction,
  AutomationActionRequest,
  AutomationActionType,
  AutomationConditionGroup,
  AutomationDelayUnit,
  AutomationConditionOperator,
  AutomationRuleRequest,
  AutomationTrigger,
  AutomationTriggerType,
} from './automation.models';

export interface AutomationRuleDraft {
  name: string;
  isEnabled: boolean;
  trigger: AutomationTrigger;
  actions: AutomationAction[];
}

export interface AutomationRuleRequestBuildResult {
  request: AutomationRuleRequest | null;
  error: string | null;
}

export function buildAutomationRuleRequest(
  draft: AutomationRuleDraft
): AutomationRuleRequestBuildResult {
  const actions = draft.actions.map(toActionRequest);
  const error =
    validateName(draft.name) ??
    validateActions(actions) ??
    validateTrigger(draft.trigger);

  if (error) return { request: null, error };

  return {
    request: {
      name: draft.name.trim(),
      isEnabled: draft.isEnabled,
      trigger: draft.trigger,
      actions,
    },
    error: null,
  };
}

function validateName(name: string): string | null {
  return name.trim() ? null : 'Automation name is required.';
}

function validateActions(actions: AutomationActionRequest[]): string | null {
  if (!actions.length) return 'Add at least one action.';

  if (
    actions.some(
      (action) =>
        action.type === AutomationActionType.flagTask && !action.flagName
    )
  ) {
    return 'Flag actions need a flag name.';
  }

  if (
    actions.some(
      (action) =>
        action.type === AutomationActionType.addComment && !action.comment
    )
  ) {
    return 'Add comment actions need a comment.';
  }

  if (
    actions.some(
      (action) =>
        action.type === AutomationActionType.updateTask &&
        action.statusId === null &&
        action.priority === null
    )
  ) {
    return 'Task update actions need a status or priority.';
  }

  const delayedDeletion = actions.find(
    (action) =>
      action.type === AutomationActionType.deleteTask &&
      (!Number.isInteger(action.delayAmount) ||
        action.delayAmount === null ||
        action.delayAmount === undefined ||
        action.delayAmount < 0)
  );

  if (delayedDeletion) {
    return 'Delete task action delay must be a whole number of 0 or more.';
  }

  const excessiveDelay = actions.some((action) => {
    if (action.type !== AutomationActionType.deleteTask) return false;

    const amount = action.delayAmount ?? 0;
    const multiplier =
      action.delayUnit === AutomationDelayUnit.days
        ? 1440
        : action.delayUnit === AutomationDelayUnit.hours
          ? 60
          : 1;

    return amount * multiplier > 525600;
  });

  if (excessiveDelay) {
    return 'Delete task action delay cannot exceed 365 days.';
  }

  return null;
}

function validateTrigger(trigger: AutomationTrigger): string | null {
  if (trigger.type === AutomationTriggerType.taskChanged) {
    if (!trigger.fields?.length) {
      return 'Choose at least one task field to watch.';
    }

    const invalidCondition = trigger.conditions?.find((condition) => {
      const requiresValue =
        condition.operator === AutomationConditionOperator.equals ||
        condition.operator === AutomationConditionOperator.notEquals ||
        condition.operator === AutomationConditionOperator.contains;

      return (
        !trigger.fields?.includes(condition.field) ||
        (requiresValue && !condition.value?.trim())
      );
    });

    if (invalidCondition) {
      return 'Complete each field condition before saving.';
    }

    if (trigger.conditionGroup) {
      const groupError = validateConditionGroup(trigger.conditionGroup);

      if (groupError) return groupError;
    }
  }

  if (
    trigger.type === AutomationTriggerType.taskUnassignedFor &&
    !isDurationInRange(trigger.durationDays, 1)
  ) {
    return 'Unassigned duration must be 1 to 365 days.';
  }

  if (
    trigger.type === AutomationTriggerType.taskDueDateApproaching &&
    !isDurationInRange(trigger.durationDays, 0)
  ) {
    return 'Due-date lead time must be 0 to 365 days.';
  }

  return null;
}

function validateConditionGroup(
  group: AutomationConditionGroup,
  depth = 1,
  count = { value: 0 }
): string | null {
  if (depth > 4) return 'Condition groups can be nested up to 4 levels.';

  if (!group.conditions.length && !group.groups.length) {
    return 'Add at least one condition to each condition group.';
  }

  count.value += group.conditions.length;

  if (count.value > 50) return 'Automations can have up to 50 conditions.';

  const invalidCondition = group.conditions.find((condition) => {
    const requiresValue =
      condition.operator === AutomationConditionOperator.equals ||
      condition.operator === AutomationConditionOperator.notEquals ||
      condition.operator === AutomationConditionOperator.contains;

    return requiresValue && !condition.value?.trim();
  });

  if (invalidCondition) {
    return 'Complete each field condition before saving.';
  }

  for (const nestedGroup of group.groups) {
    const error = validateConditionGroup(nestedGroup, depth + 1, count);

    if (error) return error;
  }

  return null;
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

function toActionRequest(action: AutomationAction): AutomationActionRequest {
  return {
    type: action.type,
    message:
      action.type === AutomationActionType.notifyTaskAssignees
        ? action.message?.trim() || null
        : null,
    comment:
      action.type === AutomationActionType.addComment
        ? action.comment?.trim() || null
        : null,
    flagName:
      action.type === AutomationActionType.flagTask
        ? action.flagName?.trim() || null
        : null,
    flagDescription:
      action.type === AutomationActionType.flagTask
        ? action.flagDescription?.trim() || null
        : null,
    statusId:
      action.type === AutomationActionType.updateTask
        ? (action.statusId ?? null)
        : null,
    priority:
      action.type === AutomationActionType.updateTask
        ? (action.priority ?? null)
        : null,
    delayAmount:
      action.type === AutomationActionType.deleteTask
        ? (action.delayAmount ?? 0)
        : null,
    delayUnit:
      action.type === AutomationActionType.deleteTask
        ? (action.delayUnit ?? null)
        : null,
  };
}
