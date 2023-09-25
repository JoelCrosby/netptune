import { AppUser } from '@core/models/appuser';
import { createAction, props } from '@ngrx/store';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { UploadResponse } from '@core/models/upload-result';

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
  props<{ profile: AppUser }>()
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
  '[Profile] Change Password Success'
);

export const changePasswordFail = createAction(
  '[Profile] Change Password Fail'
);

// Upload Profile Image

export const uploadProfilePicture = createAction(
  '[Profile] Upload Profile Picture',
  props<{ data: FormData }>()
);

export const uploadProfilePictureSuccess = createAction(
  '[Profile] Upload Profile Picture Success',
  props<{ response: UploadResponse }>()
);

export const uploadProfilePictureFail = createAction(
  '[Profile] Upload Profile Picture Fail',
  props<{ error: HttpErrorResponse | Error }>()
);
