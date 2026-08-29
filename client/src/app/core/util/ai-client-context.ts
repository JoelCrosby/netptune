import {
  AiClientContext,
  AiContextChip,
  AiContextKind,
} from '@core/models/ai-context';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { referenceRoute } from '@core/util/ai-references';

const SECTIONS: Record<string, string> = {
  boards: $localize`:Names the screen the user is on:board`,
  tasks: $localize`:Names the screen the user is on:task list`,
  sprints: $localize`:Names the screen the user is on:sprints`,
  backlog: $localize`:Names the screen the user is on:backlog`,
  projects: $localize`:Names the screen the user is on:projects`,
  reports: $localize`:Names the screen the user is on:reports`,
  calendar: $localize`:Names the screen the user is on:calendar`,
  roadmap: $localize`:Names the screen the user is on:roadmap`,
  automations: $localize`:Names the screen the user is on:automations`,
  assistant: $localize`:Names the screen the user is on:assistant`,
};

const KIND_LABELS: Record<AiContextKind, string> = {
  view: $localize`:Names the kind of thing a context chip points at:View`,
  project: $localize`:Names the kind of thing a context chip points at:Project`,
  board: $localize`:Names the kind of thing a context chip points at:Board`,
  sprint: $localize`:Names the kind of thing a context chip points at:Sprint`,
  task: $localize`:Names the kind of thing a context chip points at:Task`,
};

/** The first segment is the workspace slug, so the section is the one after it. */
export const readView = (url: string): string | undefined => {
  const segments = url
    .split('?')[0]
    .split('/')
    .filter((segment) => segment.length > 0);

  const section = segments[1];

  if (section === undefined) {
    return undefined;
  }

  return SECTIONS[section] ?? section;
};

export const viewChip = (view: string): AiContextChip => {
  return {
    kind: 'view',
    label: KIND_LABELS.view,
    name: view,
    route: null,
    context: { view },
  };
};

const routeFor = (
  workspace: string | null,
  kind: AiContextKind,
  id: string | number
): string[] | null => {
  if (workspace === null) {
    return null;
  }

  return referenceRoute(workspace, kind, String(id));
};

export const projectChip = (
  project: ProjectViewModel,
  workspace: string | null
): AiContextChip => {
  return {
    kind: 'project',
    label: KIND_LABELS.project,
    name: project.name,
    route: routeFor(workspace, 'project', project.id),
    context: { projectId: project.id, projectName: project.name },
  };
};

export const boardChip = (
  board: BoardViewModel,
  workspace: string | null
): AiContextChip => {
  return {
    kind: 'board',
    label: KIND_LABELS.board,
    name: board.name,
    route: routeFor(workspace, 'board', board.identifier),
    context: { boardId: board.id, boardName: board.name },
  };
};

export const sprintChip = (
  sprint: SprintViewModel,
  workspace: string | null
): AiContextChip => {
  return {
    kind: 'sprint',
    label: KIND_LABELS.sprint,
    name: sprint.name,
    route: routeFor(workspace, 'sprint', sprint.id),
    context: { sprintId: sprint.id, sprintName: sprint.name },
  };
};

export const taskChip = (
  task: TaskViewModel,
  workspace: string | null
): AiContextChip => {
  return {
    kind: 'task',
    label: KIND_LABELS.task,
    name: task.systemId,
    description: task.name,
    route: routeFor(workspace, 'task', task.systemId),
    context: { taskSystemId: task.systemId, taskName: task.name },
  };
};

/**
 * Describes the screen behind the chat so "this task" resolves without a lookup.
 * Everything here is already on the client — this adds no round trips.
 */
export const buildClientContext = (
  chips: readonly AiContextChip[]
): AiClientContext | null => {
  const context = chips.reduce<AiClientContext>(
    (merged, chip) => ({ ...merged, ...chip.context }),
    {}
  );

  const hasContext = Object.values(context).some(
    (value) => value !== undefined
  );

  return hasContext ? context : null;
};
