import { IsSlugUniqueResponse } from '@core/models/is-slug-unique-response';
import { AddWorkspaceRequest } from '@core/models/requests/add-workspace-request';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';
import { Workspace } from '@core/models/workspace';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';

export const initWorkspaces = createAction('[Workspaces] Init');

// Load Workspaces

export const loadWorkspaces = createAsyncAction(
  '[Workspaces] Load Workspaces',
  {
    success: props<{ workspaces: Workspace[] }>(),
  }
);

// Create Workspace

export const createWorkspace = createAsyncAction(
  '[Workspaces] Create Workspace',
  {
    init: props<{ request: AddWorkspaceRequest }>(),
    success: props<{ workspace: Workspace }>(),
  }
);

// Edit Workspace

export const editWorkspace = createAsyncAction('[Workspaces] Edit Workspace', {
  init: props<{ request: UpdateWorkspaceRequest }>(),
  success: props<{ workspace: Workspace }>(),
});

// Delete Workspace

export const deleteWorkspace = createAsyncAction(
  '[Workspaces] Delete Workspace',
  {
    init: props<{ workspace: Workspace }>(),
    success: props<{ workspace: Workspace }>(),
  }
);

// Leave Workspace

export const leaveWorkspace = createAsyncAction(
  '[Workspaces] Leave Workspace',
  {
    init: props<{ workspace: Workspace }>(),
    success: props<{ workspace: Workspace }>(),
  }
);

// Set Current Workspace

export const setCurrentWorkspace = createAction(
  '[Core] Set Current Workspace',
  props<{ workspace: Workspace }>()
);

// Select Workspace

export const selectWorkspace = createAction(
  '[Core] Select Workspace',
  props<{ workspace: Workspace }>()
);

// Is Slug Unique

export const isSlugUniue = createAsyncAction('[Workspaces] Is Slug Unique', {
  init: props<{ slug: string }>(),
  success: props<{ response: IsSlugUniqueResponse }>(),
});

// Toogle IsPublic

export const toggleWorkspaceIsPublic = createAction(
  '[Workspaces] Toggle Workspace IsPublic',
  props<{ isPublic: boolean }>()
);
