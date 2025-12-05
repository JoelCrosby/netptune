import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './users.actions';
import { adapter, initialState } from './users.model';
import { UsersState } from './users.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): UsersState => initialState),
  on(actions.loadUsers, (state): UsersState => ({ ...state, loading: true })),
  on(
    actions.loadUsersFail,
    (state, { error }): UsersState => ({
      ...state,
      loading: false,
      loadingError: error,
    })
  ),
  on(
    actions.loadUsersSuccess,
    (state, { users }): UsersState =>
      adapter.setAll(users, { ...state, loading: false, loaded: true })
  ),
  on(
    actions.removeUsersFromWorkspaceSuccess,
    (state, { emailAddresses }): UsersState =>
      adapter.removeMany(emailAddresses, {
        ...state,
        loading: false,
        loaded: true,
      })
  )
);

export const usersReducer = (
  state: UsersState | undefined,
  action: Action
): UsersState => reducer(state, action);
