import { RegisterRequest } from '@core/models/register-request';
import { createAction, props } from '@ngrx/store';
import { User, LoginRequest } from './auth.models';

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
  props<{ error: any }>()
);

export const logout = createAction('[Auth] Logout');

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
  props<{ error: any }>()
);
