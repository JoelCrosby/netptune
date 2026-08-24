import { TaskPinScope } from '@core/models/task-pin';
import {
  LucideChartNoAxesColumn,
  LucideGlobe,
  LucideIconInput,
  LucideTable2,
  LucideUser,
} from '@lucide/angular';

export const pinScopeIcons: Record<TaskPinScope, LucideIconInput> = {
  [TaskPinScope.user]: LucideUser,
  [TaskPinScope.board]: LucideTable2,
  [TaskPinScope.project]: LucideChartNoAxesColumn,
  [TaskPinScope.workspace]: LucideGlobe,
};

export const pinScopeBadgeLabel = (
  scope: TaskPinScope,
  scopeName: string
): string => {
  switch (scope) {
    case TaskPinScope.user:
      return $localize`:Badge on a task pinned only for the person looking at it:Pinned by you`;
    case TaskPinScope.workspace:
      return $localize`:Badge on a task pinned for the whole workspace:Workspace`;
    default:
      return scopeName;
  }
};

export const pinScopeTooltip = (
  scope: TaskPinScope,
  scopeName: string
): string => {
  switch (scope) {
    case TaskPinScope.user:
      return $localize`:Tooltip on the marker for a task pinned only for you:Pinned by you`;
    case TaskPinScope.board:
      return $localize`:Tooltip on the marker for a task pinned to a board. NAME is the board name:Pinned to ${scopeName}:NAME:`;
    case TaskPinScope.project:
      return $localize`:Tooltip on the marker for a task pinned to a project. NAME is the project name:Pinned in ${scopeName}:NAME:`;
    default:
      return $localize`:Tooltip on the marker for a task pinned for the whole workspace:Pinned for the whole workspace`;
  }
};

// Where the name of the board or project is not to hand — a card corner knows the scope, not the target.
export const pinScopeMarkerLabel = (scope: TaskPinScope): string => {
  switch (scope) {
    case TaskPinScope.user:
      return $localize`:Tooltip on the marker for a task pinned only for you:Pinned by you`;
    case TaskPinScope.board:
      return $localize`:Tooltip on the marker for a task pinned to the board being viewed:Pinned to this board`;
    case TaskPinScope.project:
      return $localize`:Tooltip on the marker for a task pinned to its project:Pinned to this project`;
    default:
      return $localize`:Tooltip on the marker for a task pinned for the whole workspace:Pinned for the whole workspace`;
  }
};

export const pinScopeGroupName = (
  scope: TaskPinScope,
  scopeName: string
): string => {
  switch (scope) {
    case TaskPinScope.user:
      return $localize`:Heading of the group of tasks you pinned for yourself:Yours`;
    case TaskPinScope.workspace:
      return $localize`:Heading of the group of tasks pinned for the whole workspace:Workspace`;
    default:
      return scopeName;
  }
};

export const pinScopeGroupKind = (scope: TaskPinScope): string | null => {
  switch (scope) {
    case TaskPinScope.board:
      return $localize`:Badge naming the kind of thing a group of pins belongs to:Board`;
    case TaskPinScope.project:
      return $localize`:Badge naming the kind of thing a group of pins belongs to:Project`;
    default:
      return null;
  }
};

export const pinScopeVisibilityNote = (
  scope: TaskPinScope,
  workspaceName: string
): string => {
  switch (scope) {
    case TaskPinScope.user:
      return $localize`:Note describing who can see a pin you made for yourself:Private to you · follows you everywhere in the workspace`;
    case TaskPinScope.board:
      return $localize`:Note describing who can see a board pin:Everyone who can see the board · shows in its bottom banner`;
    case TaskPinScope.project:
      return $localize`:Note describing who can see a project pin:Everyone who can see the project · shows on every board it owns`;
    default:
      return $localize`:Note describing who can see a workspace pin. NAME is the workspace name:Everyone in ${workspaceName}:NAME: · needs the tasks.pin_workspace permission`;
  }
};
