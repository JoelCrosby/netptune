import { Action } from '@ngrx/store';
import { User } from './auth.models';

export enum AuthActionTypes {
  TRY_LOGIN = '[Auth] Try Login',
  LOGIN_FAIL = '[Auth] Login Failed',
  LOGIN_SUCCESS = '[Auth] Login Succeded',
  LOGOUT = '[Auth] Logout',
}

export class ActionAuthTryLogin implements Action {
  readonly type = AuthActionTypes.TRY_LOGIN;

  constructor(readonly payload: { username: string; password: string }) {}
}

export class ActionAuthLoginSuccess implements Action {
  readonly type = AuthActionTypes.LOGIN_SUCCESS;

  constructor(readonly payload: any) {}
}

export class ActionAuthLoginFail implements Action {
  readonly type = AuthActionTypes.LOGIN_FAIL;

  constructor(readonly payload: { error: any }) {}
}

export class ActionAuthLogout implements Action {
  readonly type = AuthActionTypes.LOGOUT;
}

export type AuthActions =
  | ActionAuthTryLogin
  | ActionAuthLoginSuccess
  | ActionAuthLoginFail
  | ActionAuthLogout;
