import { LoginRequest } from '@app/core/models/login-request';
import { RegisterRequest } from '@app/core/models/register-request';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';
import {
  AuthCodeRequest,
  AuthErrorKey,
  LoginResponse,
  ResetPasswordRequest,
  UserResponse,
} from './auth.models';

// Init

export const initAuth = createAction('[Auth] Init');

// Current User

export const currentUser = createAsyncAction('[Auth] Current User', {
  success: props<{ user: UserResponse }>(),
});

// Login

export const login = createAsyncAction('[Auth] Login', {
  init: props<{ request: LoginRequest }>(),
  success: props<{ user: LoginResponse }>(),
});

// Logout — no Fail flow, and the trigger uses a custom no-arg creator, so it is
// not a clean trio and stays as plain actions.

export const logout = createAction(
  '[Auth] Logout',
  (payload: { silent?: boolean } = {}) => payload
);

export const logoutSuccess = createAction('[Auth] Logout Success');

// Register

export const register = createAsyncAction('[Auth] Register', {
  init: props<{ request: RegisterRequest }>(),
  success: props<{ user: LoginResponse }>(),
});

// Confirm email

export const confirmEmail = createAsyncAction('[Auth] Confirm Email', {
  init: props<{ request: AuthCodeRequest }>(),
  success: props<{ user: LoginResponse }>(),
});

// Request Password Reset

export const requestPasswordReset = createAsyncAction(
  '[Auth] Request Password Reset',
  {
    init: props<{ email: string }>(),
  }
);

// Reset Password

export const resetPassword = createAsyncAction('[Auth] Reset Password', {
  init: props<{ request: ResetPasswordRequest }>(),
  success: props<{ user: LoginResponse }>(),
});

// Clear Error

export const clearError = createAction(
  '[Auth] Clear Error',
  props<{ error: AuthErrorKey }>()
);

// Refresh Token — standalone success dispatched by the token refresh flow, not
// part of a trio.

export const refreshTokenSuccess = createAction(
  '[Auth] Refresh Token Succeeded',
  props<{ user: LoginResponse }>()
);

// Clear User Info

export const clearUserInfo = createAction('[Auth] Clear User Info');
