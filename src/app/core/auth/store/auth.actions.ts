import { RegisterRequest } from '@core/models/register-request';
import { createAction, props } from '@ngrx/store';
import { User, LoginRequest, ConfirmEmailRequest } from './auth.models';
import { HttpErrorResponse } from '@angular/common/http';

// Login

export const tryLogin = createAction(
  '[Auth] Try Login',
  props<{ request: LoginRequest }>()
);

export const loginSuccess = createAction(
  '[Auth] Login Succeded',
  props<{ userInfo: User }>()
);

export const loginFail = createAction(
  '[Auth] Login Failed',
  props<{ error: HttpErrorResponse }>()
);

// Logout

export const logout = createAction('[Auth] Logout');

// Register

export const register = createAction(
  '[Auth] Register',
  props<{ request: RegisterRequest }>()
);

export const registerSuccess = createAction(
  '[Auth] Register Succeded',
  props<{ userInfo: User }>()
);

export const registerFail = createAction(
  '[Auth] Register Failed',
  props<{ error: HttpErrorResponse }>()
);

// Confirm email

export const confirmEmail = createAction(
  '[Auth] Confirm Email',
  props<{ request: ConfirmEmailRequest }>()
);

export const confirmEmailSuccess = createAction(
  '[Auth] Confirm Email Succeded',
  props<{ userInfo: User }>()
);

export const confirmEmailFail = createAction(
  '[Auth] Confirm Email Failed',
  props<{ error: HttpErrorResponse }>()
);
