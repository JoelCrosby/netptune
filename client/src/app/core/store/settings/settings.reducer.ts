import { createReducer, on, Action } from '@ngrx/store';
import { SettingsState, initialState } from './settings.model';
import * as actions from './settings.actions';

const reducer = createReducer(
  initialState,
  on(
    actions.changeTheme,
    (state, { theme }): SettingsState => ({
      ...state,
      theme,
    })
  ),
  on(
    actions.clearSttings,
    (state): SettingsState => ({ ...state, theme: 'LIGHT-THEME' })
  )
);

export const settingsReducer = (
  state: SettingsState | undefined,
  action: Action
): SettingsState => reducer(state, action);
