import { AppUser } from '@core/models/appuser';
import { HttpErrorResponse } from '@angular/common/http';

export interface ProfileState {
  profile?: AppUser;
  profileloaded: boolean;
  loadProfileloading: boolean;
  loadProfileError?: HttpErrorResponse | Error;
  updateProfileError?: HttpErrorResponse | Error;
  updateProfileLoading: boolean;
}

export const initialState: ProfileState = {
  profileloaded: false,
  loadProfileloading: true,
  updateProfileLoading: false,
};
