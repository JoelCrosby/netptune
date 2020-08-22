import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './auth.actions';
import { AuthState, initialState } from './auth.models';

const reducer = createReducer(
  initialState,

  // Clear User Info

  on(actions.clearUserInfo, () => initialState),

  // Login

  on(actions.login, (state) => ({ ...state, loginLoading: true })),
  on(actions.loginSuccess, (state, { userInfo }) => ({
    ...state,
    isAuthenticated: true,
    loginLoading: false,
    currentUser: userInfo,
  })),
  on(actions.loginFail, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    loginError: error,
    loginLoading: false,
  })),

  // Register

  on(actions.register, (state) => ({ ...state, registerLoading: true })),
  on(actions.registerSuccess, (state, { userInfo }) => ({
    ...state,
    isAuthenticated: true,
    registerLoading: false,
    currentUser: userInfo,
  })),
  on(actions.registerFail, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    registerLoading: false,
    registerError: error,
  })),

  // Confirm Email

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
  on(actions.confirmEmailFail, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    confirmEmailLoading: false,
    confirmEmailLoadingError: error,
  })),

  // Request Password Reset

  on(actions.requestPasswordReset, (state) => ({
    ...state,
    requestPasswordResetLoading: true,
  })),
  on(actions.requestPasswordResetSuccess, (state, { response }) => ({
    ...state,
    requestPasswordResetLoading: false,
    requestPasswordReset: response,
  })),
  on(actions.requestPasswordResetFail, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    requestPasswordResetLoading: false,
    requestPasswordResetError: error,
  })),

  // Reset Password

  on(actions.resetPassword, (state) => ({
    ...state,
    resetPasswordLoading: true,
  })),
  on(actions.resetPasswordSuccess, (state, { userInfo }) => ({
    ...state,
    isAuthenticated: true,
    resetPasswordLoading: false,
    currentUser: userInfo,
  })),
  on(actions.resetPasswordFail, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    resetPasswordLoading: false,
    resetPasswordError: error,
  }))
);

export function authReducer(state: AuthState | undefined, action: Action) {
  return reducer(state, action);
}
