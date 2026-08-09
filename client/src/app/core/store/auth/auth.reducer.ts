import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './auth.actions';
import { AuthState, initialState } from './auth.models';

const reducer = createReducer(
  initialState,

  on(actions.clearUserInfo, (): AuthState => initialState),

  on(actions.sessionEstablished, (state, { user }): AuthState => ({
    ...state,
    isAuthenticated: true,
    currentUser: user,
    tokenExpires: user.expires,
  })),

  on(actions.currentUserLoaded, (state, { user }): AuthState => ({
    ...state,
    isAuthenticated: true,
    currentUser: user,
  }))
);

export const authReducer = (
  state: AuthState | undefined,
  action: Action
): AuthState => reducer(state, action);
