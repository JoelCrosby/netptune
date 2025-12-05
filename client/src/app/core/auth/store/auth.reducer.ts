import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './auth.actions';
import { AuthState, initialState } from './auth.models';

const reducer = createReducer(
  initialState,

  // Clear User Info

  on(actions.clearUserInfo, (): AuthState => initialState),

  // Current User

  on(
    actions.currentUser,
    (state): AuthState => ({ ...state, currentUserLoading: true })
  ),
  on(
    actions.currentUserSuccess,
    (state, { user }): AuthState => ({
      ...state,
      isAuthenticated: true,
      currentUserLoading: false,
      currentUser: user,
    })
  ),
  on(
    actions.currentUserFail,
    (state, { error }): AuthState => ({
      ...state,
      isAuthenticated: false,
      currentUserError: error,
      currentUserLoading: false,
    })
  ),

  // Login

  on(actions.login, (state): AuthState => ({ ...state, loginLoading: true })),
  on(
    actions.loginSuccess,
    (state, { token }): AuthState => ({
      ...state,
      isAuthenticated: true,
      loginLoading: false,
      token,
    })
  ),
  on(
    actions.loginFail,
    (state): AuthState => ({
      ...state,
      isAuthenticated: false,
      loginError: true,
      loginLoading: false,
    })
  ),
  on(
    actions.clearError,
    (state): AuthState => ({
      ...state,
      loginError: false,
      loginLoading: false,
    })
  ),
  // Register

  on(
    actions.register,
    (state): AuthState => ({ ...state, registerLoading: true })
  ),
  on(
    actions.registerSuccess,
    (state, { token }): AuthState => ({
      ...state,
      isAuthenticated: true,
      registerLoading: false,
      currentUser: token,
      token,
    })
  ),
  on(
    actions.registerFail,
    (state, { error }): AuthState => ({
      ...state,
      isAuthenticated: false,
      registerLoading: false,
      registerError: error,
    })
  ),

  // Confirm Email

  on(
    actions.confirmEmail,
    (state): AuthState => ({
      ...state,
      confirmEmailLoading: true,
    })
  ),
  on(
    actions.confirmEmailSuccess,
    (state, { token }): AuthState => ({
      ...state,
      isAuthenticated: true,
      confirmEmailLoading: false,
      currentUser: token,
      token,
    })
  ),
  on(
    actions.confirmEmailFail,
    (state, { error }): AuthState => ({
      ...state,
      isAuthenticated: false,
      confirmEmailLoading: false,
      confirmEmailLoadingError: error,
    })
  ),

  // Request Password Reset

  on(
    actions.requestPasswordReset,
    (state): AuthState => ({
      ...state,
      requestPasswordResetLoading: true,
    })
  ),
  on(
    actions.requestPasswordResetSuccess,
    (state): AuthState => ({
      ...state,
      requestPasswordResetLoading: false,
    })
  ),
  on(
    actions.requestPasswordResetFail,
    (state, { error }): AuthState => ({
      ...state,
      isAuthenticated: false,
      requestPasswordResetLoading: false,
      requestPasswordResetError: error,
    })
  ),

  // Reset Password

  on(
    actions.resetPassword,
    (state): AuthState => ({
      ...state,
      resetPasswordLoading: true,
    })
  ),
  on(
    actions.resetPasswordSuccess,
    (state, { token }): AuthState => ({
      ...state,
      isAuthenticated: true,
      resetPasswordLoading: false,
      currentUser: token,
      token,
    })
  ),
  on(
    actions.resetPasswordFail,
    (state, { error }): AuthState => ({
      ...state,
      isAuthenticated: false,
      resetPasswordLoading: false,
      resetPasswordError: error,
    })
  )
);

export const authReducer = (
  state: AuthState | undefined,
  action: Action
): AuthState => reducer(state, action);
