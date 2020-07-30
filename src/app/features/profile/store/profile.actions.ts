import { AppUser } from '@core/models/appuser';
import { createAction, props } from '@ngrx/store';
import { HttpErrorResponse } from '@angular/common/http';

export const loadProfile = createAction('[Profile] Load Profile');

export const loadProfileSuccess = createAction(
  '[Profile] Load Profile Success',
  props<{ profile: AppUser }>()
);

export const loadProfileFail = createAction(
  '[Profile] Load Profile Fail',
  props<{ error: HttpErrorResponse | Error }>()
);

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
