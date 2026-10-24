import { numberFormat } from '@core/util/locale';
import {
  AutomationActionResult,
  AutomationRule,
  AutomationRuleWarning,
  AutomationRun,
  AutomationWarningCode,
  AutomationWarningLocation,
} from './automation.models';

type MessageRenderer = (args: string[]) => string;

const hoursFormat = numberFormat({ maximumFractionDigits: 1 });

const describeWindow = (minutesText: string): string => {
  const minutes = Number(minutesText);

  if (minutes < 60) {
    return $localize`:A length of time an automation limit counts runs over:${minutes}:minutes: minutes`;
  }

  const tenthsOfAnHour = Math.round(minutes / 6);

  if (tenthsOfAnHour === 10) {
    return $localize`:A length of time an automation limit counts runs over:an hour`;
  }

  const hours = hoursFormat.format(tenthsOfAnHour / 10);

  return $localize`:A length of time an automation limit counts runs over:${hours}:hours: hours`;
};

const MESSAGES: Record<string, MessageRenderer> = {
  actionNotExecutable: () =>
    $localize`:Result of an automation action:The action did not produce an executable contribution.`,
  taskDeletedByEarlierAction: () =>
    $localize`:Result of an automation action:The task was deleted by an earlier action.`,
  noExecutionHandler: () =>
    $localize`:Result of an automation action:No execution handler is registered for the action type.`,
  actionTypeUnsupported: () =>
    $localize`:Result of an automation action:The action type is not supported.`,
  cancelledAfterPlanningFailure: () =>
    $localize`:Result of an automation action:The run was cancelled because a later action could not be planned.`,
  notPlannedAfterEarlierFailure: () =>
    $localize`:Result of an automation action:The action was not planned because an earlier action failed.`,
  notRunWhileUnauthorized: () =>
    $localize`:Result of an automation action:The action was not run because the automation principal was not authorized.`,
  principalMissing: () =>
    $localize`:Result of an automation run:The automation rule has no execution principal.`,
  principalUnavailable: () =>
    $localize`:Result of an automation run:The automation execution principal is no longer available.`,
  principalDisabled: () =>
    $localize`:Result of an automation run:The automation execution principal is disabled.`,
  principalLacksPermissions: () =>
    $localize`:Result of an automation run:The automation execution principal no longer has the required permissions.`,
  deleteScheduled: () =>
    $localize`:Result of an automation action:The action was scheduled for later execution.`,
  taskAlreadyDeleted: () =>
    $localize`:Result of an automation action:The task was already deleted.`,
  taskAlreadyFlagged: () =>
    $localize`:Result of an automation action:The task already has a flag from this rule.`,
  taskAlreadyMatches: () =>
    $localize`:Result of an automation action:The task already has the configured values.`,
  taskNotInProject: () =>
    $localize`:Result of an automation action:The triggering task is not in a project.`,
  taskKeyUnavailable: () =>
    $localize`:Result of an automation action:Unable to reserve a task key for the project.`,
  projectUnavailable: () =>
    $localize`:Result of an automation action:The triggering task's project is no longer available.`,
  boardGroupUnavailable: () =>
    $localize`:Result of an automation action:The selected board group is not available for the project.`,
  boardGroupUnavailableForTask: () =>
    $localize`:Result of an automation action:The selected board group is not available for the task's project.`,
  taskStatusUnavailable: () =>
    $localize`:Result of an automation action:A task status is not available in the workspace.`,
  statusUnavailable: () =>
    $localize`:Result of an automation action:The selected status is not available in the workspace.`,
  assigneeUnavailable: () =>
    $localize`:Result of an automation action:An assignee is no longer available in the workspace.`,
  ownerOrAssigneeUnavailable: () =>
    $localize`:Result of an automation action:An owner or assignee is no longer available in the workspace.`,
  tagUnavailable: () =>
    $localize`:Result of an automation action:A configured tag is no longer available in the workspace.`,
  sprintUnavailable: () =>
    $localize`:Result of an automation action:The selected sprint is not available for the project.`,
  sprintUnavailableForTask: () =>
    $localize`:Result of an automation action:The selected sprint is not available for the task's project.`,
  invalidDateRange: () =>
    $localize`:Result of an automation action:Task start date must be on or before its due date`,
  taskMissing: () =>
    $localize`:Result of an automation action:The task no longer exists.`,
  taskReloadFailed: () =>
    $localize`:Result of an automation action:The updated task could not be reloaded.`,
  relationTypeUnavailable: () =>
    $localize`:Result of an automation action:The selected relation type is no longer available in the workspace.`,
  relationTargetMissing: () =>
    $localize`:Result of an automation action:The action has no task to link to.`,
  relationToSelf: () =>
    $localize`:Result of an automation action:A task cannot be related to itself.`,
  relationTargetUnavailable: () =>
    $localize`:Result of an automation action:The task to link to is no longer available in the workspace.`,
  relationAlreadyLinked: () =>
    $localize`:Result of an automation action:These tasks are already linked by this relation.`,
  relationNothingToRemove: () =>
    $localize`:Result of an automation action:The task has no relations of this type to remove.`,
  createdTaskAlreadyRelated: () =>
    $localize`:Result of an automation action:The created task already has a relation of the selected type.`,
  relationSingleSource: ([relationType]) =>
    $localize`:Result of an automation action:That task already has a "${relationType}:relationType:" link. A task can only have one.`,
  relationCycle: ([relationType]) =>
    $localize`:Result of an automation action:This would create a circular "${relationType}:relationType:" chain.`,
  disabledAfterFailures: ([count, window]) => {
    const period = describeWindow(window);

    return $localize`:Why an automation rule was turned off:Disabled automatically after ${count}:count: failed runs within ${period}:period:.`;
  },
  disabledAfterRuns: ([count, window]) => {
    const period = describeWindow(window);

    return $localize`:Why an automation rule was turned off:Disabled automatically after ${count}:count: runs within ${period}:period:.`;
  },
};

const render = (
  code: string | null | undefined,
  args: string[] | null | undefined,
  fallback: string | null | undefined
): string | null => {
  const renderer = code ? MESSAGES[code] : undefined;

  if (!renderer) {
    return fallback ?? null;
  }

  return renderer(args ?? []);
};

export const describeRunMessage = (run: AutomationRun): string | null => {
  return render(run.messageCode, run.messageArguments, run.message);
};

export const describeActionResultMessage = (
  result: AutomationActionResult
): string | null => {
  return render(result.messageCode, result.messageArguments, result.message);
};

export const describeAutoDisabledReason = (
  rule: AutomationRule
): string | null => {
  return render(
    rule.autoDisabledReasonCode,
    rule.autoDisabledReasonArguments,
    rule.autoDisabledReason
  );
};

const describeRuleLevelWarning = (
  code: AutomationWarningCode
): string | null => {
  switch (code) {
    case AutomationWarningCode.missingProject:
      return $localize`:Warning on an automation rule:This rule is scoped to a project that no longer exists.`;
    case AutomationWarningCode.missingBoard:
      return $localize`:Warning on an automation rule:This rule is scoped to a board that no longer exists.`;
    case AutomationWarningCode.missingSprint:
      return $localize`:Warning on an automation rule:This rule is scoped to a sprint that no longer exists.`;
    case AutomationWarningCode.missingExecutionUser:
      return $localize`:Warning on an automation rule:The account this rule runs as is no longer a member of this workspace.`;
    default:
      return null;
  }
};

const describeConditionWarning = (
  code: AutomationWarningCode,
  value: string
): string | null => {
  switch (code) {
    case AutomationWarningCode.missingStatus:
      return $localize`:Warning on an automation rule:A condition checks a status that no longer exists.`;
    case AutomationWarningCode.missingSprint:
      return $localize`:Warning on an automation rule:A condition checks a sprint that no longer exists.`;
    case AutomationWarningCode.missingTag:
      return $localize`:Warning on an automation rule:A condition checks the tag "${value}:tag:", which no longer exists.`;
    case AutomationWarningCode.missingUser:
      return $localize`:Warning on an automation rule:A condition checks a member who is no longer in this workspace.`;
    default:
      return null;
  }
};

const describeActionWarning = (
  code: AutomationWarningCode,
  value: string
): string | null => {
  switch (code) {
    case AutomationWarningCode.missingStatus:
      return $localize`:Warning on an automation rule:This action sets a status that no longer exists.`;
    case AutomationWarningCode.missingSprint:
      return $localize`:Warning on an automation rule:This action sets a sprint that no longer exists.`;
    case AutomationWarningCode.missingBoardGroup:
      return $localize`:Warning on an automation rule:This action moves tasks to a board group that no longer exists.`;
    case AutomationWarningCode.missingTask:
      return $localize`:Warning on an automation rule:This action links to a task that no longer exists.`;
    case AutomationWarningCode.missingRelationType:
      return $localize`:Warning on an automation rule:This action uses a relation type that no longer exists.`;
    case AutomationWarningCode.missingUser:
      return $localize`:Warning on an automation rule:This action references a member who is no longer in this workspace.`;
    case AutomationWarningCode.missingTag:
      return $localize`:Warning on an automation rule:This action uses the tag "${value}:tag:", which no longer exists.`;
    default:
      return null;
  }
};

const WARNING_RENDERERS: Record<
  AutomationWarningLocation,
  (code: AutomationWarningCode, value: string) => string | null
> = {
  [AutomationWarningLocation.rule]: describeRuleLevelWarning,
  [AutomationWarningLocation.condition]: describeConditionWarning,
  [AutomationWarningLocation.action]: describeActionWarning,
};

export const describeRuleWarning = (warning: AutomationRuleWarning): string => {
  const describe = WARNING_RENDERERS[warning.location];
  const described = describe?.(warning.code, warning.value ?? '');

  return described ?? warning.message;
};
