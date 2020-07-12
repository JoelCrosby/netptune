import { createAction, props } from '@ngrx/store';
import { Workspace } from '@core/models/workspace';
import { HttpErrorResponse } from '@angular/common/http';

export const loadWorkspaces = createAction('[Workspaces] Load Workspaces');

export const loadWorkspacesSuccess = createAction(
  '[Workspaces] Load Workspaces Success ',
  props<{ workspaces: Workspace[] }>()
);

export const loadWorkspacesFail = createAction(
  '[Workspaces] Load Workspaces Fail',
  props<{ error: HttpErrorResponse }>()
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
  props<{ error: HttpErrorResponse }>()
);

export const editWorkspace = createAction(
  '[Workspaces] Edit Workspace',
  props<{ workspace: Workspace }>()
);

export const editWorkspaceSuccess = createAction(
  '[Workspaces] Edit Workspace Success',
  props<{ workspace: Workspace }>()
);

export const editWorkspaceFail = createAction(
  '[Workspaces] Edit Workspace Fail',
  props<{ error: HttpErrorResponse }>()
);

export const deleteWorkspace = createAction(
  '[Workspaces] Delete Workspace',
  props<{ workspace: Workspace }>()
);

export const deleteWorkspaceSuccess = createAction(
  '[Workspaces] Delete Workspace Success ',
  props<{ workspace: Workspace }>()
);

export const deleteWorkspaceFail = createAction(
  '[Workspaces] Load Workspaces Fail',
  props<{ error: HttpErrorResponse }>()
);

export const selectWorkspace = createAction(
  '[Core] Select Workspace',
  props<{ workspace: Workspace }>()
);
