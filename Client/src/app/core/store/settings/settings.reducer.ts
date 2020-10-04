import { createReducer, on, Action } from '@ngrx/store';
import { SettingsState, initialState } from './settings.model';
import * as actions from './settings.actions';

const reducer = createReducer(
  initialState,
  on(actions.changeTheme, (state, { theme }) => ({
    ...state,
    theme,
  })),
  on(actions.clearSttings, (state) => ({ ...state, theme: 'DEFAULT-THEME' }))
);

export function settingsReducer(
  state: SettingsState | undefined,
  action: Action
): SettingsState {
  return reducer(state, action);
}
