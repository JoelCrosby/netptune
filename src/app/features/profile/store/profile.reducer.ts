import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './profile.actions';
import { initialState, ProfileState } from './profile.model';

const reducer = createReducer(
  initialState,

  // Load Profile

  on(actions.loadProfile, (state) => ({ ...state, loadProfileloading: true })),
  on(actions.loadProfileFail, (state, { error }) => ({
    ...state,
    loadProfileloading: false,
    loadProfileError: error,
  })),
  on(actions.loadProfileSuccess, (state, { profile }) => ({
    ...state,
    loadProfileloading: false,
    profileloaded: true,
    profile,
  })),

  // Update Profile

  on(actions.updateProfile, (state) => ({
    ...state,
    updateProfileLoading: true,
  })),
  on(actions.updateProfileFail, (state, { error }) => ({
    ...state,
    updateProfileLoading: false,
    updateProfileError: error,
  })),
  on(actions.updateProfileSuccess, (state, { profile }) => ({
    ...state,
    updateProfileLoading: false,
    profile,
  }))
);

export function profileReducer(
  state: ProfileState | undefined,
  action: Action
): ProfileState {
  return reducer(state, action);
}
