import { createAction, props } from '@ngrx/store';
import { Workspace } from '@core/models/workspace';

export const loadWorkspaces = createAction('[Workspaces] Load Workspaces');

export const loadWorkspacesSuccess = createAction(
  '[Workspaces] Load Workspaces Success ',
  props<{ workspaces: Workspace[] }>()
);

export const loadWorkspacesFail = createAction(
  '[Workspaces] Load Workspaces Fail',
  props<{ error: any }>()
);

export const createWorkspace = createAction(
  '[Workspaces] Create Workspace',
  props<{ workspace: Workspace }>()
);

export const createWorkspaceSuccess = createAction(
  '[Workspaces] Create Workspace Success',
  props<{ workspace: Workspace }>()
);

export const createWorkspaceFail = createAction(
  '[Workspaces] Create Workspace Fail',
  props<{ error: any }>()
);

export const deleteWorkspace = createAction(
  '[Workspaces] Delete Workspace',
  props<{ workspace: Workspace }>()
);

export const deleteWorkspacesSuccess = createAction(
  '[Workspaces] Delete Workspace Success ',
  props<{ workspace: Workspace }>()
);

export const deleteWorkspacesFail = createAction(
  '[Workspaces] Load Workspaces Fail',
  props<{ workspace: any }>()
);

export const selectWorkspace = createAction(
  '[Core] Select Workspace',
  props<{ workspaceSlug: string }>()
);
