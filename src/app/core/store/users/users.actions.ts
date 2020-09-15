import { AppUser } from '@core/models/appuser';
import { props, createAction } from '@ngrx/store';
import { HttpErrorResponse } from '@angular/common/http';

export const clearState = createAction('[Users] Clear State');

export const loadUsers = createAction('[Users] Load Users');

export const loadUsersSuccess = createAction(
  '[Users] Load Users Success ',
  props<{ users: AppUser[] }>()
);

export const loadUsersFail = createAction(
  '[Users] Load Users Fail',
  props<{ error: HttpErrorResponse | Error }>()
);

// Invite users to workspace

export const inviteUsersToWorkspace = createAction(
  '[Users] Invite users to workspace',
  props<{ emailAddresses: string[] }>()
);

export const inviteUsersToWorkspaceSuccess = createAction(
  '[Users] Invite users to workspace Success ',
  props<{ emailAddresses: string[] }>()
);

export const inviteUsersToWorkspaceFail = createAction(
  '[Users] Invite users to workspace Fail',
  props<{ error: HttpErrorResponse | Error }>()
);

// Remove users from workspace

export const removeUsersFromWorkspace = createAction(
  '[Users] Remove users from workspace',
  props<{ emailAddresses: string[] }>()
);

export const removeUsersFromWorkspaceSuccess = createAction(
  '[Users] Remove users from workspace Success ',
  props<{ emailAddresses: string[] }>()
);

export const removeUsersFromWorkspaceFail = createAction(
  '[Users] Remove users from workspace Fail',
  props<{ error: HttpErrorResponse | Error }>()
);
