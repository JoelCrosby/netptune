import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './auth.actions';
import { AuthState, initialState } from './auth.models';

const reducer = createReducer(
  initialState,

  // Clear User Info

  on(actions.clearUserInfo, (): AuthState => initialState),

  // Current User

  on(
    actions.currentUser.init,
    (state): AuthState => ({ ...state, currentUserLoading: true })
  ),
  on(
    actions.currentUser.success,
    (state, { user }): AuthState => ({
      ...state,
      isAuthenticated: true,
      currentUserLoading: false,
      currentUser: user,
    })
  ),
  on(
    actions.currentUser.fail,
    (state, { error }): AuthState => ({
      ...state,
      currentUserError: error,
      currentUserLoading: false,
    })
  ),

  // Login

  on(actions.login.init, (state): AuthState => ({ ...state, loginLoading: true })),
  on(
    actions.login.success,
    (state, { user }): AuthState => ({
      ...state,
      isAuthenticated: true,
      loginLoading: false,
      currentUser: user,
      tokenExpires: user.expires,
    })
  ),
  on(
    actions.login.fail,
    (state): AuthState => ({
      ...state,
      isAuthenticated: false,
      loginError: true,
      loginLoading: false,
    })
  ),
  on(
    actions.refreshTokenSuccess,
    (state, { user }): AuthState => ({
      ...state,
      isAuthenticated: true,
      currentUser: user,
      tokenExpires: user.expires,
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
    actions.register.init,
    (state): AuthState => ({ ...state, registerLoading: true })
  ),
  on(
    actions.register.success,
    (state, { user }): AuthState => ({
      ...state,
      isAuthenticated: true,
      registerLoading: false,
      currentUser: user,
      tokenExpires: user.expires,
    })
  ),
  on(
    actions.register.fail,
    (state, { error }): AuthState => ({
      ...state,
      isAuthenticated: false,
      registerLoading: false,
      registerError: error,
    })
  ),

  // Confirm Email

  on(
    actions.confirmEmail.init,
    (state): AuthState => ({
      ...state,
      confirmEmailLoading: true,
    })
  ),
  on(
    actions.confirmEmail.success,
    (state, { user }): AuthState => ({
      ...state,
      isAuthenticated: true,
      confirmEmailLoading: false,
      currentUser: user,
      tokenExpires: user.expires,
    })
  ),
  on(
    actions.confirmEmail.fail,
    (state, { error }): AuthState => ({
      ...state,
      isAuthenticated: false,
      confirmEmailLoading: false,
      confirmEmailLoadingError: error,
    })
  ),

  // Request Password Reset

  on(
    actions.requestPasswordReset.init,
    (state): AuthState => ({
      ...state,
      requestPasswordResetLoading: true,
    })
  ),
  on(
    actions.requestPasswordReset.success,
    (state): AuthState => ({
      ...state,
      requestPasswordResetLoading: false,
    })
  ),
  on(
    actions.requestPasswordReset.fail,
    (state, { error }): AuthState => ({
      ...state,
      isAuthenticated: false,
      requestPasswordResetLoading: false,
      requestPasswordResetError: error,
    })
  ),

  // Reset Password

  on(
    actions.resetPassword.init,
    (state): AuthState => ({
      ...state,
      resetPasswordLoading: true,
    })
  ),
  on(
    actions.resetPassword.success,
    (state, { user }): AuthState => ({
      ...state,
      isAuthenticated: true,
      resetPasswordLoading: false,
      currentUser: user,
      tokenExpires: user.expires,
    })
  ),
  on(
    actions.resetPassword.fail,
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
