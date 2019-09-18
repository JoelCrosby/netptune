import { Action } from '@ngrx/store';
import { AppUser } from '@core/models/appuser';

export enum ProfileActionTypes {
  LoadProfile = '[Profile] Load Profile',
  LoadProfileFail = '[Profile] Load Profile Fail',
  LoadProfileSuccess = '[Profile] Load Profile Success ',
  UpdateProfile = '[Profile] Update Profile',
  UpdateProfileFail = '[Profile] Update Profile Fail',
  UpdateProfileSuccess = '[Profile] Update Profile Success ',
}

export class ActionLoadProfile implements Action {
  readonly type = ProfileActionTypes.LoadProfile;
}

export class ActionLoadProfileSuccess implements Action {
  readonly type = ProfileActionTypes.LoadProfileSuccess;

  constructor(readonly payload: AppUser) {}
}

export class ActionLoadProfileFail implements Action {
  readonly type = ProfileActionTypes.LoadProfileFail;

  constructor(readonly payload: any) {}
}

export class ActionUpdateProfile implements Action {
  readonly type = ProfileActionTypes.UpdateProfile;

  constructor(readonly payload: AppUser) {}
}

export class ActionUpdateProfileSuccess implements Action {
  readonly type = ProfileActionTypes.UpdateProfileSuccess;

  constructor(readonly payload: AppUser) {}
}

export class ActionUpdateProfileFail implements Action {
  readonly type = ProfileActionTypes.UpdateProfileFail;

  constructor(readonly payload: any) {}
}

export type ProfileActions =
  | ActionLoadProfile
  | ActionLoadProfileFail
  | ActionLoadProfileSuccess
  | ActionUpdateProfile
  | ActionUpdateProfileFail
  | ActionUpdateProfileSuccess;
