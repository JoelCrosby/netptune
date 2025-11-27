import { AppUser } from '@core/models/appuser';
import { HttpErrorResponse } from '@angular/common/http';

export interface ProfileState {
  profile?: AppUser;
  profileloaded: boolean;
  loadProfileloading: boolean;
  loadProfileError?: HttpErrorResponse | Error;
  updateProfileError?: HttpErrorResponse | Error;
  updateProfileLoading: boolean;
  changePasswordLoading: boolean;
  changePasswordError?: HttpErrorResponse | Error;
}

export const initialState: ProfileState = {
  profileloaded: false,
  loadProfileloading: true,
  updateProfileLoading: false,
  changePasswordLoading: false,
};
