import { selectSettingsFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { SettingsState } from './settings.model';

export const selectSettings = createSelector(
  selectSettingsFeature,
  (state: SettingsState) => state
);

export const selectSettingsTheme = createSelector(
  selectSettings,
  (settings) => settings.theme
);

export const selectEffectiveTheme = createSelector(
  selectSettingsTheme,
  (theme) => theme.toLowerCase()
);
