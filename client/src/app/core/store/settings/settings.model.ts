export type Language = 'en' | 'de' | 'fr';

export interface SettingsState {
  language: string;
  theme: string;
}

export const initialState: SettingsState = {
  language: 'en',
  theme: 'LIGHT-THEME',
};
