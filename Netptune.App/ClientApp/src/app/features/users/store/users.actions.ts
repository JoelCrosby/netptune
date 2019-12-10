import { AppUser } from '@core/models/appuser';
import { props, createAction } from '@ngrx/store';

export const loadUsers = createAction('[Users] Load Users');

export const loadUsersSuccess = createAction(
  '[Users] Load Users Success ',
  props<{ users: AppUser[] }>()
);

export const loadUsersFail = createAction(
  '[Users] Load Users Fail',
  props<{ error: any }>()
);
