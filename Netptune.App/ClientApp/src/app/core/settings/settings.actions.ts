import { createAction, props } from '@ngrx/store';

export const clearSttings = createAction('[Settings] Clear Settings');

export const changeTheme = createAction(
  '[Settings] Change Theme',
  props<{ theme: string }>()
);
