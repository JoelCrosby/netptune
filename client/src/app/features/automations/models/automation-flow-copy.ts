import { ServiceAccount } from '@core/models/service-account';
import { Status } from '@core/models/status';
import { joinNaturalList, toLowerText } from '@core/util/strings';
import {
  AutomationCopySegment,
  AutomationScopeKind,
  countAutomationConditions,
  describeAutomationConditionsSegments,
  describeAutomationTrigger,
  taskChangeFieldLabels,
} from './automation-copy';
import { AutomationTrigger, AutomationTriggerType } from './automation.models';

// Copy for the step cards the editor and the detail rail share, which name each part of a rule
// in a title and summarise it underneath.

export interface AutomationScopeIds {
  projectId?: number | null;
  boardId?: number | null;
  sprintId?: number | null;
}

export interface AutomationScopeSources {
  projects: readonly { id: number; name: string }[];
  boards: readonly { id: number; name: string }[];
  sprints: readonly { id: number; name: string }[];
}

export interface AutomationScopeTarget {
  kind: AutomationScopeKind;
  name: string;
}

export function resolveAutomationScope(
  ids: AutomationScopeIds,
  sources: AutomationScopeSources
): AutomationScopeTarget {
  const { projectId, boardId, sprintId } = ids;

  if (projectId !== null && projectId !== undefined) {
    const project = sources.projects.find((item) => item.id === projectId);

    return { kind: 'project', name: project?.name ?? '…' };
  }

  if (boardId !== null && boardId !== undefined) {
    const board = sources.boards.find((item) => item.id === boardId);

    return { kind: 'board', name: board?.name ?? '…' };
  }

  if (sprintId !== null && sprintId !== undefined) {
    const sprint = sources.sprints.find((item) => item.id === sprintId);

    return { kind: 'sprint', name: sprint?.name ?? '…' };
  }

  return { kind: 'workspace', name: '' };
}

export function describeServiceAccountName(
  accounts: readonly ServiceAccount[],
  userId: string | null
): string {
  const account = accounts.find((candidate) => candidate.userId === userId);

  return (
    account?.name ??
    $localize`:Shown in place of the service account an automation runs as when none is chosen:No service account`
  );
}

export function describeAutomationScope(scope: AutomationScopeTarget): string {
  const name = scope.name;

  switch (scope.kind) {
    case 'project':
      return $localize`:Where an automation listens. PROJECT is a project name:Runs on the ${name}:PROJECT: project`;
    case 'board':
      return $localize`:Where an automation listens. BOARD is a board name:Runs on the ${name}:BOARD: board`;
    case 'sprint':
      return $localize`:Where an automation listens. SPRINT is a sprint name:Runs on ${name}:SPRINT:`;
    case 'workspace':
      return $localize`:Where an automation listens when it covers every task:Runs on the whole workspace`;
  }
}

export function describeAutomationRunsAs(
  account: string,
  scope: AutomationScopeTarget
): string {
  const name = scope.name;

  switch (scope.kind) {
    case 'project':
      return $localize`:Who an automation acts as and where it listens. ACCOUNT is a service account, PROJECT a project name:Runs as ${account}:ACCOUNT: on the ${name}:PROJECT: project`;
    case 'board':
      return $localize`:Who an automation acts as and where it listens. ACCOUNT is a service account, BOARD a board name:Runs as ${account}:ACCOUNT: on the ${name}:BOARD: board`;
    case 'sprint':
      return $localize`:Who an automation acts as and where it listens. ACCOUNT is a service account, SPRINT a sprint name:Runs as ${account}:ACCOUNT: on ${name}:SPRINT:`;
    case 'workspace':
      return $localize`:Who an automation acts as when it covers every task. ACCOUNT is a service account:Runs as ${account}:ACCOUNT: on the whole workspace`;
  }
}

// The trigger step describes the event alone, because the conditions have their own step.
export function describeTriggerStepSummary(
  trigger: AutomationTrigger
): AutomationCopySegment[] {
  const event = { ...trigger, conditionGroup: null };

  if (event.type !== AutomationTriggerType.taskChanged) {
    return [{ type: 'text', text: describeAutomationTrigger(event) }];
  }

  const fields = joinNaturalList(
    (event.fields ?? []).map((field) =>
      toLowerText(taskChangeFieldLabels[field])
    ),
    'and'
  );
  const text = fields
    ? $localize`:Summary of the fields a task-changed trigger watches. FIELDS lists them:Watching ${fields}:FIELDS:`
    : $localize`:Summary of a task-changed trigger with no watched fields:No fields watched yet`;

  return [{ type: 'text', text }];
}

export function describeConditionsStepTitle(
  trigger: AutomationTrigger
): string {
  const count = countAutomationConditions(trigger.conditionGroup);

  if (count === 0) {
    return $localize`:Title of the conditions step when it has none:No conditions`;
  }

  return count === 1
    ? $localize`:Title of the conditions step with one condition:1 condition`
    : $localize`:Title of the conditions step. COUNT is greater than one:${count}:COUNT: conditions`;
}

export function describeConditionsStepSummary(
  trigger: AutomationTrigger,
  statuses: Status[]
): AutomationCopySegment[] {
  const count = countAutomationConditions(trigger.conditionGroup);

  if (!count) {
    return [
      {
        type: 'text',
        text: $localize`:Summary of the conditions step when it has none:Every task continues`,
      },
    ];
  }

  return describeAutomationConditionsSegments(trigger, statuses);
}

export function automationActionKeyword(index: number): string {
  return index === 0
    ? $localize`:Heading of the first action in the rule:THEN`
    : $localize`:Heading of a follow-on action in the rule:AND THEN`;
}
