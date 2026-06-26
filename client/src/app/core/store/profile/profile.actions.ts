import { AppUser } from '@core/models/appuser';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { UploadResponse } from '@core/models/upload-result';
import { createAsyncAction } from '@core/util/create-async-action';
import { props } from '@ngrx/store';

// Load Profile

export const loadProfile = createAsyncAction('[Profile] Load Profile', {
  success: props<{ profile: AppUser }>(),
  fail: props<{ error: HttpErrorResponse | Error }>(),
});

// Update Profile

export const updateProfile = createAsyncAction('[Profile] Update Profile', {
  init: props<{ profile: AppUser; image?: FormData }>(),
  success: props<{ profile: AppUser }>(),
  fail: props<{ error: HttpErrorResponse | Error }>(),
});

// Change Password

export const changePassword = createAsyncAction('[Profile] Change Password', {
  init: props<{ request: ChangePasswordRequest }>(),
  fail: props<{ error: HttpErrorResponse | Error }>(),
});

// Load Login Providers

export const loadLoginProviders = createAsyncAction(
  '[Profile] Load Login Providers',
  {
    success: props<{ providers: string[] }>(),
    fail: props<{ error: HttpErrorResponse | Error }>(),
  }
);

// Upload Profile Image

export const uploadProfilePicture = createAsyncAction(
  '[Profile] Upload Profile Picture',
  {
    init: props<{ data: FormData }>(),
    success: props<{ response: UploadResponse }>(),
    fail: props<{ error: HttpErrorResponse | Error }>(),
  }
);
