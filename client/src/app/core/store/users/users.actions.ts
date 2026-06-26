import { HttpErrorResponse } from '@angular/common/http';
import { WorkspaceAppUser } from '@core/models/appuser';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[Users] Clear State');

// Load Users

export const loadUsers = createAsyncAction('[Users] Load Users', {
  success: props<{
    users: WorkspaceAppUser[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  }>(),
  fail: props<{ error: HttpErrorResponse | Error }>(),
});

export const setUsersPageSize = createAction(
  '[Users] Set Users Page Size',
  props<{ pageSize: number }>()
);

export const setUsersPage = createAction(
  '[Users] Set Users Page',
  props<{ page: number }>()
);

// Load User

export const loadUser = createAsyncAction('[Users] Load User', {
  init: props<{ userId: string }>(),
  success: props<{ user: WorkspaceAppUser }>(),
  fail: props<{ error: HttpErrorResponse | Error }>(),
});

// Invite users to workspace

export const inviteUsersToWorkspace = createAsyncAction(
  '[Users] Invite users to workspace',
  {
    init: props<{ emailAddresses: string[] }>(),
    success: props<{ emailAddresses: string[] }>(),
    fail: props<{ error: HttpErrorResponse | Error }>(),
  }
);

// Remove users from workspace

export const removeUsersFromWorkspace = createAsyncAction(
  '[Users] Remove users from workspace',
  {
    init: props<{ emailAddresses: string[] }>(),
    success: props<{ emailAddresses: string[] }>(),
    fail: props<{ error: HttpErrorResponse | Error }>(),
  }
);

// Resend invite

export const resendInvite = createAsyncAction('[Users] Resend Invite', {
  init: props<{ email: string }>(),
  fail: props<{ error: HttpErrorResponse | Error }>(),
});

// Toggle user permission

export const toggleUserPermission = createAsyncAction(
  '[Users] Toggle User Permission',
  {
    init: props<{ userId: string; permission: string }>(),
    success: props<{ userId: string; permission: string }>(),
    fail: props<{ error: HttpErrorResponse | Error }>(),
  }
);
