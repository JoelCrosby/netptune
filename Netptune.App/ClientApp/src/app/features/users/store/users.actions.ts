import { Action } from '@ngrx/store';
import { AppUser } from '@core/models/appuser';

export enum UsersActionTypes {
  LoadUsers = '[Users] Load Users',
  LoadUsersFail = '[Users] Load Users Fail',
  LoadUsersSuccess = '[Users] Load Users Success ',
}

export class ActionLoadUsers implements Action {
  readonly type = UsersActionTypes.LoadUsers;
}

export class ActionLoadUsersSuccess implements Action {
  readonly type = UsersActionTypes.LoadUsersSuccess;

  constructor(readonly payload: AppUser[]) {}
}

export class ActionLoadUsersFail implements Action {
  readonly type = UsersActionTypes.LoadUsersFail;

  constructor(readonly payload: any) {}
}

export type UsersActions = ActionLoadUsers | ActionLoadUsersFail | ActionLoadUsersSuccess;
