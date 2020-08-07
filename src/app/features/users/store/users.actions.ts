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

// Invite user to workspace

export const inviteUserToWorkspace = createAction(
  '[Users] Invite user to workspace',
  props<{ emailAddres: string }>()
);
