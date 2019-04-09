import { Action } from '@ngrx/store';

export enum SettingsActionTypes {
  CLEAR = '[Settings] Clear',
  CHANGE_THEME = '[Settings] Change Theme',
}

export class ActionSettingsClear implements Action {
  readonly type = SettingsActionTypes.CLEAR;
}

export class ActionSettingsChangeTheme implements Action {
  readonly type = SettingsActionTypes.CHANGE_THEME;

  constructor(readonly payload: { theme: string }) {}
}

export type SettingsActions = ActionSettingsChangeTheme | ActionSettingsClear;
