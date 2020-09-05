import { AppUser } from '@core/models/appuser';
import { createAction, props } from '@ngrx/store';
import { HttpErrorResponse } from '@angular/common/http';
import { ClientResponse } from '@core/models/client-response';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';

// Load Profile

export const loadProfile = createAction('[Profile] Load Profile');

export const loadProfileSuccess = createAction(
  '[Profile] Load Profile Success',
  props<{ profile: AppUser }>()
);

export const loadProfileFail = createAction(
  '[Profile] Load Profile Fail',
  props<{ error: HttpErrorResponse | Error }>()
);

// Update Profile

export const updateProfile = createAction(
  '[Profile] Update Profile',
  props<{ profile: Partial<AppUser> }>()
);

export const updateProfileSuccess = createAction(
  '[Profile] Update Profile Success',
  props<{ profile: AppUser }>()
);

export const updateProfileFail = createAction(
  '[Profile] Update Profile Fail',
  props<{ error: HttpErrorResponse | Error }>()
);

// Change Password

export const changePassword = createAction(
  '[Profile] Change Password',
  props<{ request: ChangePasswordRequest }>()
);

export const changePasswordSuccess = createAction(
  '[Profile] Change Password Success',
  props<{ response: ClientResponse }>()
);

export const changePasswordFail = createAction(
  '[Profile] Change Password Fail'
);
