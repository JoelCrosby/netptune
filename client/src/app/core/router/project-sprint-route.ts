import { assertInInjectionContext } from '@angular/core';
import {
  QueryParamSignal,
  positiveIntegerParam,
  queryParamSignal,
  queryParamsRoute,
} from './query-param-signal';

export interface ProjectSprintRoute {
  readonly projectId: QueryParamSignal<number | undefined>;
  readonly sprintId: QueryParamSignal<number | undefined>;
  setProject(projectId: number | null): void;
  // Pass the sprint's own project so picking a sprint also selects its project.
  setSprint(sprintId: number | null, sprintProjectId?: number): void;
}

export function projectSprintRoute(): ProjectSprintRoute {
  assertInInjectionContext(projectSprintRoute);

  const queryParams = queryParamsRoute();
  const projectId = queryParamSignal('projectId', positiveIntegerParam);
  const sprintId = queryParamSignal('sprintId', positiveIntegerParam);

  return {
    projectId,
    sprintId,
    setProject: (id) => {
      queryParams.patch({ projectId: id?.toString() ?? null, sprintId: null });
    },
    setSprint: (id, sprintProjectId) => {
      queryParams.patch({
        sprintId: id,
        projectId: sprintProjectId ?? projectId() ?? null,
      });
    },
  };
}
