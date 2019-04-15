import { Action } from '@ngrx/store';

export enum UsersActionTypes {
  LoadUserss = '[Users] Load Userss',
  
  
}

export class LoadUserss implements Action {
  readonly type = UsersActionTypes.LoadUserss;
}


export type UsersActions = LoadUserss;
