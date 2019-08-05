import { Action } from '@ngrx/store';

export enum AuthActionTypes {
  TRY_LOGIN = '[Auth] Try Login',
  LOGIN_FAIL = '[Auth] Login Failed',
  LOGIN_SUCCESS = '[Auth] Login Succeded',
  LOGOUT = '[Auth] Logout',
  REGISTER = '[Auth] Register',
  REGISTER_FAIL = '[Auth] Register Failed',
  REGISTER_SUCCESS = '[Auth] Register Succeded',
}

export class ActionAuthTryLogin implements Action {
  readonly type = AuthActionTypes.TRY_LOGIN;

  constructor(readonly payload: { email: string; password: string }) {}
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

export class ActionAuthRegister implements Action {
  readonly type = AuthActionTypes.REGISTER;

  constructor(readonly payload: { email: string; password: string }) {}
}

export class ActionAuthRegisterSuccess implements Action {
  readonly type = AuthActionTypes.REGISTER_SUCCESS;

  constructor(readonly payload: any) {}
}

export class ActionAuthRegisterFail implements Action {
  readonly type = AuthActionTypes.REGISTER_FAIL;

  constructor(readonly payload: { error: any }) {}
}

export type AuthActions =
  | ActionAuthTryLogin
  | ActionAuthLoginSuccess
  | ActionAuthLoginFail
  | ActionAuthLogout
  | ActionAuthRegister
  | ActionAuthRegisterSuccess
  | ActionAuthRegisterFail;
