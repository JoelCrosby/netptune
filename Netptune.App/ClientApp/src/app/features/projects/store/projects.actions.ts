import { AddProjectRequest } from '@core/models/project';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { createAction, props } from '@ngrx/store';

export const loadProjects = createAction('[Projects] Load Projects');

export const loadProjectsSuccess = createAction(
  '[Projects] Load Projects Success ',
  props<{ projects: ProjectViewModel[] }>()
);

export const loadProjectsFail = createAction(
  '[Projects] Load Projects Fail',
  props<{ error: any }>()
);

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
  props<{ error: any }>()
);

export const selectProject = createAction(
  '[Projects] Select Project',
  props<{ project: ProjectViewModel }>()
);

export const deleteProject = createAction(
  '[Projects] Delete Project',
  props<{ project: ProjectViewModel }>()
);

export const deleteProjectSuccess = createAction(
  '[Projects] Delete Project Success',
  props<{ project: ProjectViewModel }>()
);

export const deleteProjectFail = createAction(
  '[Projects] Delete Project Fail',
  props<{ error: any }>()
);
