import { SettingsState } from './settings.model';
import { SettingsActions, SettingsActionTypes } from './settings.actions';

export const initialState: SettingsState = {
  language: 'en',
  theme: 'DEFAULT-THEME',
};

export function settingsReducer(state = initialState, action: SettingsActions): SettingsState {
  switch (action.type) {
    case SettingsActionTypes.CHANGE_THEME:
      return { ...state, ...action.payload };
    case SettingsActionTypes.CLEAR:
      return { ...state, theme: 'DEFAULT-THEME' };
    default:
      return state;
  }
}
