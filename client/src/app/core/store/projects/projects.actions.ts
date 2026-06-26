import { AddProjectRequest } from '@core/models/project';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';

export const clearState = createAction('[Projects] Clear State');

// Load Projects

export const loadProjects = createAsyncAction('[Projects] Load Projects', {
  success: props<{ projects: ProjectViewModel[] }>(),
});

// Load Project Detail

export const loadProjectDetail = createAsyncAction(
  '[Projects] Load Project Detail',
  {
    init: props<{ projectKey: string }>(),
    success: props<{ project: ProjectViewModel }>(),
  }
);

// Clear Project Detail — sync action, remains plain.

export const clearProjectDetail = createAction(
  '[Projects] Clear Project Detail'
);

// Create Project

export const createProject = createAsyncAction('[Projects] Create Project', {
  init: props<{ project: AddProjectRequest }>(),
  success: props<{ project: ProjectViewModel }>(),
});

// Update Project

export const updateProject = createAsyncAction('[Projects] Update Project', {
  init: props<{ project: UpdateProjectRequest }>(),
  success: props<{ project: ProjectViewModel }>(),
});

// Select Project — sync action, remains plain.

export const selectProject = createAction(
  '[Projects] Select Project',
  props<{ project: ProjectViewModel }>()
);

// Delete Project

export const deleteProject = createAsyncAction('[Projects] Delete Project', {
  init: props<{ project: ProjectViewModel }>(),
  success: props<{ projectId: number }>(),
});

// Get Project Boards

export const getProjectBoards = createAsyncAction(
  '[Projects] Get Project Boards',
  {
    init: props<{ projectId: number }>(),
    success: props<{ boards: BoardViewModel[] }>(),
  }
);
