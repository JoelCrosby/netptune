import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './users.actions';
import { adapter, initialState } from './users.model';
import { UsersState } from './users.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),
  on(actions.loadUsers, (state) => ({ ...state, loading: true })),
  on(actions.loadUsersFail, (state, { error }) => ({
    ...state,
    loading: false,
    loadUsersError: error,
  })),
  on(actions.loadUsersSuccess, (state, { users }) =>
    adapter.setAll(users, { ...state, loading: false, loaded: true })
  ),
  on(actions.removeUsersFromWorkspaceSuccess, (state, { emailAddresses }) =>
    adapter.removeMany(emailAddresses, {
      ...state,
      loading: false,
      loaded: true,
    })
  )
);

export function usersReducer(
  state: UsersState | undefined,
  action: Action
): UsersState {
  return reducer(state, action);
}
