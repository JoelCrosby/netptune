import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './profile.actions';
import { initialState, ProfileState } from './profile.model';

const reducer = createReducer(
  initialState,

  // Load Profile

  on(
    actions.loadProfile,
    (state): ProfileState => ({ ...state, loadProfileloading: true })
  ),
  on(
    actions.loadProfileFail,
    (state, { error }): ProfileState => ({
      ...state,
      loadProfileloading: false,
      loadProfileError: error,
    })
  ),
  on(
    actions.loadProfileSuccess,
    (state, { profile }): ProfileState => ({
      ...state,
      loadProfileloading: false,
      profileloaded: true,
      profile,
    })
  ),

  // Update Profile

  on(
    actions.updateProfile,
    (state): ProfileState => ({
      ...state,
      updateProfileLoading: true,
    })
  ),
  on(
    actions.updateProfileFail,
    (state, { error }): ProfileState => ({
      ...state,
      updateProfileLoading: false,
      updateProfileError: error,
    })
  ),
  on(
    actions.updateProfileSuccess,
    (state, { profile }): ProfileState => ({
      ...state,
      updateProfileLoading: false,
      profile,
    })
  ),

  // Change Password

  on(
    actions.changePassword,
    (state): ProfileState => ({
      ...state,
      changePasswordLoading: true,
    })
  ),
  on(
    actions.changePasswordFail,
    (state, { error }): ProfileState => ({
      ...state,
      changePasswordLoading: false,
      changePasswordError: error,
    })
  ),
  on(
    actions.changePasswordSuccess,
    (state): ProfileState => ({
      ...state,
      changePasswordLoading: false,
    })
  ),

  // Upload Profile Picture

  on(
    actions.uploadProfilePictureSuccess,
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
