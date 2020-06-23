import { AppUser } from '@core/models/appuser';

export interface ProfileState {
  profile?: AppUser;
  profileloaded: boolean;
  loadProfileloading: boolean;
  loadProfileError?: any;
  updateProfileError?: any;
  updateProfileLoading: boolean;
}

export const initialState: ProfileState = {
  profileloaded: false,
  loadProfileloading: true,
  updateProfileLoading: false,
};
