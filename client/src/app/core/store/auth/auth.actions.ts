import { createAction, props } from '@ngrx/store';
import { LoginResponse, UserResponse } from './auth.models';

// The signed-in session. Everything that authenticates — login, register, email
// confirmation, password reset and token refresh — ends here.

export const sessionEstablished = createAction(
  '[Auth] Session Established',
  props<{ user: LoginResponse }>()
);

export const currentUserLoaded = createAction(
  '[Auth] Current User Loaded',
  props<{ user: UserResponse }>()
);

export const logoutSuccess = createAction('[Auth] Logout Success');

export const clearUserInfo = createAction('[Auth] Clear User Info');
