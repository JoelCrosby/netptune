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
}

export class ActionAuthLoginSuccess implements Action {
  readonly type = AuthActionTypes.LOGIN_SUCCESS;

  constructor(readonly payload: User) {}
}

export class ActionAuthLoginFail implements Action {
  readonly type = AuthActionTypes.LOGIN_FAIL;
}

export class ActionAuthLogout implements Action {
  readonly type = AuthActionTypes.LOGOUT;
}

export type AuthActions =
  | ActionAuthTryLogin
  | ActionAuthLoginSuccess
  | ActionAuthLoginFail
  | ActionAuthLogout;
