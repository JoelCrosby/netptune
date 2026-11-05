import { WorkspaceSetupTemplate } from '@core/models/workspace-setup-template';
import {
  LucideGitBranch,
  LucideSettings2,
  LucideSparkles,
  LucideUsers,
  LucideWorkflow,
} from '@lucide/angular';
import { FlowStepListItem } from '@static/components/flow-step-list/flow-step-list.component';

export type WorkspaceSetupStepKey = 'repository' | 'assistant' | 'team';

const workspaceFlowStepOptionalLabel = $localize`:Marks a workspace setup step as not required:Optional`;

export interface WorkspaceFlowSetupDetails {
  identifier: string;
  colorLabel: string;
  hasDescription: boolean;
  locked?: boolean;
}

export function workspaceFlowSetupStep({
  identifier,
  colorLabel,
  hasDescription,
  locked = false,
}: WorkspaceFlowSetupDetails): FlowStepListItem {
  const summary = hasDescription
    ? $localize`:Summary of a workspace's setup. COLOR is its colour:${colorLabel}:COLOR:, with a description`
    : $localize`:Summary of a workspace's setup. COLOR is its colour:${colorLabel}:COLOR:, no description`;

  return {
    key: 'details',
    keyword: $localize`:Heading of the setup part of creating a workspace:SETUP`,
    icon: LucideSettings2,
    title:
      identifier ||
      $localize`:Title of the setup card before an identifier exists:Workspace details`,
    summary,
    done: locked,
    locked,
  };
}

export function workspaceFlowWorkflowStep(
  template: WorkspaceSetupTemplate | undefined,
  locked = false
): FlowStepListItem {
  return {
    key: 'workflow',
    keyword: $localize`:Heading of the workflow part of creating a workspace:WORKFLOW`,
    icon: LucideWorkflow,
    title:
      template?.name ??
      $localize`:Title of the workflow card before its template is known:Workflow`,
    summary: template?.boardGroups.join(' → ') ?? '',
    done: locked,
    locked,
  };
}

export interface WorkspaceFlowOptionalState {
  key: WorkspaceSetupStepKey;
  summary: string;
  done: boolean;
}

const optionalStepCopy = (key: WorkspaceSetupStepKey) => {
  switch (key) {
    case 'repository':
      return {
        keyword: $localize`:Heading of the repository part of setting up a workspace:REPOSITORY`,
        icon: LucideGitBranch,
        title: $localize`:Title of the repository card:Link a repository`,
      };
    case 'assistant':
      return {
        keyword: $localize`:Heading of the assistant part of setting up a workspace:ASSISTANT`,
        icon: LucideSparkles,
        title: $localize`:Title of the AI assistant key card:AI assistant key`,
      };
    case 'team':
      return {
        keyword: $localize`:Heading of the team part of setting up a workspace:TEAM`,
        icon: LucideUsers,
        title: $localize`:Title of the invite teammates card:Invite teammates`,
      };
  }
};

export function workspaceFlowOptionalSteps(
  states?: WorkspaceFlowOptionalState[]
): FlowStepListItem[] {
  const pending = $localize`:Summary of an optional step that needs the workspace to exist first:Available after creating`;
  const resolved: WorkspaceFlowOptionalState[] = states ?? [
    { key: 'repository', summary: pending, done: false },
    { key: 'assistant', summary: pending, done: false },
    { key: 'team', summary: pending, done: false },
  ];

  return resolved.map((state, index) => {
    return {
      ...optionalStepCopy(state.key),
      key: state.key,
      summary: state.summary,
      done: state.done,
      aside: workspaceFlowStepOptionalLabel,
      locked: !states,
      startsGroup: index === 0,
    };
  });
}
