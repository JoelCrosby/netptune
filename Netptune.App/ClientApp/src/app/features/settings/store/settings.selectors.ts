import { createFeatureSelector, createSelector } from '@ngrx/store';
import { SettingsState } from './settings.model';

export const selectSettingsState = createFeatureSelector<SettingsState>('settings');

export const selectSettings = createSelector(
  selectSettingsState,
  (state: SettingsState) => state
);

export const selectSettingsLanguage = createSelector(
  selectSettings,
  (state: SettingsState) => state.language
);

export const selectSettingsTheme = createSelector(
  selectSettings,
  settings => settings.theme
);

export const selectEffectiveTheme = createSelector(
  selectSettingsTheme,
  theme => theme.toLowerCase()
);
