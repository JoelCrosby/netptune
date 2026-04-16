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
    actions.loadUser,
    (state): UsersState => ({
      ...state,
      userDetailLoading: true,
    })
  ),
  on(
    actions.loadUserSuccess,
    (state, { user }): UsersState => ({
      ...state,
      userDetail: user,
      userDetailLoading: false,
    })
  ),
  on(
    actions.loadUserFail,
    (state, { error }): UsersState => ({
      ...state,
      userDetail: undefined,
      userDetailLoading: false,
      userDetailLoadingError: error,
    })
  ),
  on(
    actions.removeUsersFromWorkspaceSuccess,
    (state, { emailAddresses }): UsersState =>
      adapter.removeMany(emailAddresses, {
        ...state,
        loading: false,
        loaded: true,
      })
  ),
  on(
    actions.toggleUserPermission,
    (state): UsersState => ({ ...state, loading: true })
  ),
  on(
    actions.toggleUserPermissionSuccess,
    (state): UsersState => ({ ...state, loading: false })
  ),
  on(
    actions.toggleUserPermissionFail,
    (state, { error }): UsersState => ({
      ...state,
      loading: false,
      loadingError: error,
    })
  )
);

export const usersReducer = (
  state: UsersState | undefined,
  action: Action
): UsersState => reducer(state, action);
