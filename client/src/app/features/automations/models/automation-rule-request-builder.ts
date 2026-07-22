import {
  AutomationAction,
  AutomationActionRequest,
  AutomationActionType,
  AutomationRuleRequest,
  AutomationTrigger,
  AutomationTriggerType,
  TaskChangeField,
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

  return null;
}

function validateTrigger(trigger: AutomationTrigger): string | null {
  if (trigger.type === AutomationTriggerType.taskChanged) {
    if (!trigger.fields?.length) {
      return 'Choose at least one task field to watch.';
    }

    if (
      trigger.fields.includes(TaskChangeField.status) &&
      trigger.statusId === null
    ) {
      return 'Choose a status to watch.';
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
  };
}
