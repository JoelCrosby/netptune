import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './profile.actions';
import { initialState, ProfileState } from './profile.model';

const reducer = createReducer(
  initialState,

  // Load Profile

  on(actions.loadProfile.init, (state): ProfileState => ({
    ...state,
    loadProfileloading: true,
    loadProfileError: undefined,
  })),
  on(actions.loadProfile.fail, (state, { error }): ProfileState => ({
    ...state,
    loadProfileloading: false,
    loadProfileError: error,
  })),
  on(actions.loadProfile.success, (state, { profile }): ProfileState => ({
    ...state,
    loadProfileloading: false,
    loadProfileError: undefined,
    profileloaded: true,
    profile,
  })),

  // Update Profile

  on(actions.updateProfile.init, (state): ProfileState => ({
    ...state,
    updateProfileLoading: true,
  })),
  on(actions.updateProfile.fail, (state, { error }): ProfileState => ({
    ...state,
    updateProfileLoading: false,
    updateProfileError: error,
  })),
  on(actions.updateProfile.success, (state, { profile }): ProfileState => ({
    ...state,
    updateProfileLoading: false,
    profile,
  })),

  // Change Password

  on(actions.changePassword.init, (state): ProfileState => ({
    ...state,
    changePasswordLoading: true,
    changePasswordError: undefined,
  })),
  on(actions.changePassword.fail, (state, { error }): ProfileState => ({
    ...state,
    changePasswordLoading: false,
    changePasswordError: error,
  })),
  on(actions.changePassword.success, (state): ProfileState => ({
    ...state,
    changePasswordLoading: false,
    changePasswordError: undefined,
  })),

  // Set Password

  on(actions.setPassword.init, (state): ProfileState => ({
    ...state,
    setPasswordLoading: true,
    setPasswordError: undefined,
  })),
  on(actions.setPassword.fail, (state, { error }): ProfileState => ({
    ...state,
    setPasswordLoading: false,
    setPasswordError: error,
  })),
  on(actions.setPassword.success, (state): ProfileState => ({
    ...state,
    setPasswordLoading: false,
    setPasswordError: undefined,
    hasPassword: true,
  })),

  // Load Login Methods

  on(actions.loadLoginMethods.success, (state, { methods }): ProfileState => ({
    ...state,
    loginProviders: methods.providers,
    hasPassword: methods.hasPassword,
  })),

  // Upload Profile Picture

  on(
    actions.uploadProfilePicture.success,
    (state, { response }): ProfileState => {
      if (!response?.uri || !state.profile) {
        return state;
      }

      return {
        ...state,
        profile: {
          ...state.profile,
          pictureUrl: response?.uri,
        },
      };
    }
  )
);

export const profileReducer = (
  state: ProfileState | undefined,
  action: Action
): ProfileState => reducer(state, action);
