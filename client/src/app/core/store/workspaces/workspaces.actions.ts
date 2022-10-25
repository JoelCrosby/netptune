import { HttpErrorResponse } from '@angular/common/http';
import { IsSlugUniqueResponse } from '@core/models/is-slug-unique-response';
import { AddWorkspaceRequest } from '@core/models/requests/add-workspace-request';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';
import { Workspace } from '@core/models/workspace';
import { createAction, props } from '@ngrx/store';

// Load Workspaces

export const loadWorkspaces = createAction('[Workspaces] Load Workspaces');

export const loadWorkspacesSuccess = createAction(
  '[Workspaces] Load Workspaces Success ',
  props<{ workspaces: Workspace[] }>()
);

export const loadWorkspacesFail = createAction(
  '[Workspaces] Load Workspaces Fail',
  props<{ error: HttpErrorResponse }>()
);

// Create Workspace

export const createWorkspace = createAction(
  '[Workspaces] Create Workspace',
  props<{ request: AddWorkspaceRequest }>()
);

export const createWorkspaceSuccess = createAction(
  '[Workspaces] Create Workspace Success',
  props<{ workspace: Workspace }>()
);

export const createWorkspaceFail = createAction(
  '[Workspaces] Create Workspace Fail',
  props<{ error: HttpErrorResponse }>()
);

// Edit Workspace

export const editWorkspace = createAction(
  '[Workspaces] Edit Workspace',
  props<{ request: UpdateWorkspaceRequest }>()
);

export const editWorkspaceSuccess = createAction(
  '[Workspaces] Edit Workspace Success',
  props<{ workspace: Workspace }>()
);

export const editWorkspaceFail = createAction(
  '[Workspaces] Edit Workspace Fail',
  props<{ error: HttpErrorResponse }>()
);

// Delete Workspace

export const deleteWorkspace = createAction(
  '[Workspaces] Delete Workspace',
  props<{ workspace: Workspace }>()
);

export const deleteWorkspaceSuccess = createAction(
  '[Workspaces] Delete Workspace Success ',
  props<{ workspace: Workspace }>()
);

export const deleteWorkspaceFail = createAction(
  '[Workspaces] Delete Workspace Fail',
  props<{ error: HttpErrorResponse }>()
);

// Select Workspace

export const selectWorkspace = createAction(
  '[Core] Select Workspace',
  props<{ workspace: Workspace }>()
);

// Select Workspace

export const isSlugUniue = createAction(
  '[Workspaces] Is Slug Unique',
  props<{ slug: string }>()
);

export const isSlugUniueSuccess = createAction(
  '[Workspaces] Is Slug Unique Success ',
  props<{ response: IsSlugUniqueResponse }>()
);

export const isSlugUniueFail = createAction(
  '[Workspaces] Is Slug Unique Fail',
  props<{ error: HttpErrorResponse }>()
);
