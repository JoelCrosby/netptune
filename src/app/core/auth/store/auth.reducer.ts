import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './auth.actions';
import { AuthState, initialState } from './auth.models';

const reducer = createReducer(
  initialState,
  on(actions.tryLogin, (state) => ({ ...state, loading: true })),
  on(actions.loginSuccess, (state, { userInfo }) => ({
    ...state,
    isAuthenticated: true,
    loading: false,
    currentUser: userInfo,
  })),
  on(actions.loginFail, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    loginError: error,
    loading: false,
  })),
  on(actions.register, (state) => ({ ...state, loading: true })),
  on(actions.registerSuccess, (state, { userInfo }) => ({
    ...state,
    isAuthenticated: true,
    loading: false,
    currentUser: userInfo,
  })),
  on(actions.registerFail, (state) => ({
    ...state,
    isAuthenticated: false,
    loading: false,
  })),
  on(actions.confirmEmail, (state) => ({
    ...state,
    confirmEmailLoading: true,
  })),
  on(actions.confirmEmailSuccess, (state, { userInfo }) => ({
    ...state,
    isAuthenticated: true,
    confirmEmailLoading: false,
    currentUser: userInfo,
  })),
  on(actions.confirmEmailFail, (state) => ({
    ...state,
    isAuthenticated: false,
    confirmEmailLoading: false,
  }))
);

export function authReducer(state: AuthState | undefined, action: Action) {
  return reducer(state, action);
}
