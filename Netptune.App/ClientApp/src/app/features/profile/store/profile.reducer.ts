import { AppUser } from '@app/core/models/appuser';
import { ProfileActions, ProfileActionTypes } from './profile.actions';

export interface ProfileState {
  profile?: AppUser;
  loading: boolean;
  loaded: boolean;
  loadProfileError?: any;
  createProjectError?: any;
  createProjectLoading: boolean;
}

export const initialState: ProfileState = {
  loading: false,
  loaded: false,
  createProjectLoading: false,
};

export function profileReducer(state = initialState, action: ProfileActions): ProfileState {
  switch (action.type) {
    case ProfileActionTypes.LoadProfile:
      return { ...state, loading: true };
    case ProfileActionTypes.LoadProfileFail:
      return { ...state, loading: false, loadProfileError: action.payload };
    case ProfileActionTypes.LoadProfileSuccess:
      return {
        ...state,
        loading: false,
        loaded: true,
        profile: action.payload,
      };
    case ProfileActionTypes.UpdateProfile:
      return { ...state, createProjectLoading: true };
    case ProfileActionTypes.UpdateProfileFail:
      return { ...state, createProjectLoading: false, createProjectError: action.payload };
    case ProfileActionTypes.UpdateProfileSuccess:
      return {
        ...state,
        createProjectLoading: false,
        profile: action.payload,
      };
    default:
      return state;
  }
}
