import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';

export interface AiClientContext {
  view?: string;
  projectId?: number;
  projectName?: string;
  boardId?: number;
  sprintId?: number;
  taskSystemId?: string;
  taskName?: string;
}

interface AiClientContextSource {
  url: string;
  project?: ProjectViewModel;
  task?: TaskViewModel | null;
}

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

/** The first segment is the workspace slug, so the section is the one after it. */
const readSegments = (url: string): string[] => {
  return url.split('?')[0].split('/').filter((segment) => segment.length > 0);
};

const readIdentifier = (segments: string[], section: string): number | undefined => {
  const index = segments.indexOf(section);
  const identifier = index < 0 ? undefined : Number(segments[index + 1]);

  return Number.isFinite(identifier) ? identifier : undefined;
};

/**
 * Describes the screen behind the chat so "this task" resolves without a lookup.
 * Everything here is already on the client — this adds no round trips.
 */
export const buildClientContext = (
  source: AiClientContextSource
): AiClientContext | null => {
  const segments = readSegments(source.url);
  const section = segments[1];
  const context: AiClientContext = {
    view: SECTIONS[section] ?? section,
    projectId: source.project?.id,
    projectName: source.project?.name,
    boardId: readIdentifier(segments, 'boards'),
    sprintId: readIdentifier(segments, 'sprints'),
    taskSystemId: source.task?.systemId,
    taskName: source.task?.name,
  };

  const hasContext = Object.values(context).some((value) => value !== undefined);

  return hasContext ? context : null;
};
