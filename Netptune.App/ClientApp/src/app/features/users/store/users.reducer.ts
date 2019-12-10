import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './users.actions';
import { adapter, initialState } from './users.model';
import { UsersState } from './users.model';

const reducer = createReducer(
  initialState,
  on(actions.loadUsers, state => ({ ...state, loading: true })),
  on(actions.loadUsersFail, (state, { error }) => ({
    ...state,
    loading: false,
    loadUsersError: error,
  })),
  on(actions.loadUsersSuccess, (state, { users }) =>
    adapter.addAll(users, { ...state, loading: false, loaded: true })
  )
);

export function usersReducer(state: UsersState | undefined, action: Action) {
  return reducer(state, action);
}
