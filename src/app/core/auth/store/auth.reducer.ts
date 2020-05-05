import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './auth.actions';
import { AuthState, initialState } from './auth.models';

const reducer = createReducer(
  initialState,
  on(actions.tryLogin, state => ({ ...state, loading: true })),
  on(actions.loginSuccess, (state, { userInfo }) => ({
    ...state,
    isAuthenticated: true,
    loading: false,
    currentUser: userInfo,
  })),
  on(actions.loginFail, state => ({
    ...state,
    isAuthenticated: false,
    loading: false,
  })),
  on(actions.register, state => ({ ...state, loading: true })),
  on(actions.registerSuccess, (state, { userInfo }) => ({
    ...state,
    isAuthenticated: true,
    loading: false,
    currentUser: userInfo,
  })),
  on(actions.registerFail, state => ({
    ...state,
    isAuthenticated: false,
    loading: false,
  })),
  on(actions.logout, state => ({
    ...state,
    loading: false,
    isAuthenticated: false,
    currentUser: undefined,
  }))
);

export function authReducer(state: AuthState | undefined, action: Action) {
  return reducer(state, action);
}
