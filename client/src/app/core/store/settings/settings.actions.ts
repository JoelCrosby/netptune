import { createAction, props } from '@ngrx/store';

export const changeTheme = createAction(
  '[Settings] Change Theme',
  props<{ theme: string }>()
);
