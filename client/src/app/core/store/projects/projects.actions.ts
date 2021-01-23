import { AddProjectRequest } from '@core/models/project';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { createAction, props } from '@ngrx/store';
import { HttpErrorResponse } from '@angular/common/http';
import { ClientResponse } from '@core/models/client-response';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';

export const clearState = createAction('[Projects] Clear State');

// Load Projects

export const loadProjects = createAction('[Projects] Load Projects');

export const loadProjectsSuccess = createAction(
  '[Projects] Load Projects Success ',
  props<{ projects: ProjectViewModel[] }>()
);

export const loadProjectsFail = createAction(
  '[Projects] Load Projects Fail',
  props<{ error: HttpErrorResponse }>()
);

// Load Project Detail

export const loadProjectDetail = createAction(
  '[Projects] Load Project Detail',
  props<{ projectKey: string }>()
);

export const loadProjectDetailSuccess = createAction(
  '[Projects] Load Project Detail Success ',
  props<{ project: ProjectViewModel }>()
);

export const loadProjectDetailFail = createAction(
  '[Projects] Load Project Detail Fail',
  props<{ error: HttpErrorResponse }>()
);

// Create Project

export const createProject = createAction(
  '[Projects] Create Project',
  props<{ project: AddProjectRequest }>()
);

export const createProjectSuccess = createAction(
  '[Projects] Create Project Success',
  props<{ project: ProjectViewModel }>()
);

export const createProjectFail = createAction(
  '[Projects] Create Project Fail',
  props<{ error: HttpErrorResponse }>()
);

// Update Project

export const updateProject = createAction(
  '[Projects] Update Project',
  props<{ project: UpdateProjectRequest }>()
);

export const updateProjectSuccess = createAction(
  '[Projects] Update Project Success',
  props<{ project: ProjectViewModel }>()
);

export const updateProjectFail = createAction(
  '[Projects] Update Project Fail',
  props<{ error: HttpErrorResponse }>()
);

// Select Project

export const selectProject = createAction(
  '[Projects] Select Project',
  props<{ project: ProjectViewModel }>()
);

// Delete Project

export const deleteProject = createAction(
  '[Projects] Delete Project',
  props<{ project: ProjectViewModel }>()
);

export const deleteProjectSuccess = createAction(
  '[Projects] Delete Project Success',
  props<{ response: ClientResponse; projectId: number }>()
);

export const deleteProjectFail = createAction(
  '[Projects] Delete Project Fail',
  props<{ error: HttpErrorResponse }>()
);

// Get Project Boards

export const getProjectBoards = createAction(
  '[Projects] Get Project Boards',
  props<{ projectId: number }>()
);

export const getProjectBoardsSuccess = createAction(
  '[Projects] Get Project Boards Success',
  props<{ boards: BoardViewModel[] }>()
);

export const getProjectBoardsFail = createAction(
  '[Projects] Get Project Boards Fail',
  props<{ error: HttpErrorResponse }>()
);
