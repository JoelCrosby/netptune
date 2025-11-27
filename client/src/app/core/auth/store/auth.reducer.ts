import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './auth.actions';
import { AuthState, initialState } from './auth.models';

const reducer = createReducer(
  initialState,

  // Clear User Info

  on(actions.clearUserInfo, () => initialState),

  // Current User

  on(actions.currentUser, (state) => ({ ...state, currentUserLoading: true })),
  on(actions.currentUserSuccess, (state, { user }) => ({
    ...state,
    isAuthenticated: true,
    currentUserLoading: false,
    currentUser: user,
  })),
  on(actions.currentUserFail, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    currentUserError: error,
    currentUserLoading: false,
  })),

  // Login

  on(actions.login, (state) => ({ ...state, loginLoading: true })),
  on(actions.loginSuccess, (state, { token }) => ({
    ...state,
    isAuthenticated: true,
    loginLoading: false,
    token,
  })),
  on(actions.loginFail, (state) => ({
    ...state,
    isAuthenticated: false,
    loginError: true,
    loginLoading: false,
  })),
  on(actions.clearError, (state) => ({
    ...state,
    loginError: false,
    loginLoading: false,
  })),
  // Register

  on(actions.register, (state) => ({ ...state, registerLoading: true })),
  on(actions.registerSuccess, (state, { token }) => ({
    ...state,
    isAuthenticated: true,
    registerLoading: false,
    currentUser: token,
    token,
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
  on(actions.confirmEmailSuccess, (state, { token }) => ({
    ...state,
    isAuthenticated: true,
    confirmEmailLoading: false,
    currentUser: token,
    token,
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
  on(actions.requestPasswordResetSuccess, (state) => ({
    ...state,
    requestPasswordResetLoading: false,
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
  on(actions.resetPasswordSuccess, (state, { token }) => ({
    ...state,
    isAuthenticated: true,
    resetPasswordLoading: false,
    currentUser: token,
    token,
  })),
  on(actions.resetPasswordFail, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    resetPasswordLoading: false,
    resetPasswordError: error,
  }))
);

export const authReducer = (
  state: AuthState | undefined,
  action: Action
): AuthState => reducer(state, action);
