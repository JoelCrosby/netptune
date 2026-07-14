import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './users.actions';
import { adapter, initialState } from './users.model';
import { UsersState } from './users.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): UsersState => initialState),
  on(actions.loadUsers.init, (state): UsersState => ({
    ...state,
    loading: true,
  })),
  on(actions.setUsersPageSize, (state, { pageSize }): UsersState => ({
    ...state,
    pageSize,
    page: 1,
  })),
  on(actions.setUsersPage, (state, { page }): UsersState => ({
    ...state,
    page,
  })),
  on(actions.loadUsers.fail, (state, { error }): UsersState => ({
    ...state,
    loading: false,
    loadingError: error,
  })),
  on(
    actions.loadUsers.success,
    (state, { users, page, pageSize, totalCount, totalPages }): UsersState =>
      adapter.setAll(users, {
        ...state,
        loading: false,
        loaded: true,
        page,
        pageSize,
        totalCount,
        totalPages,
      })
  ),
  on(actions.loadUser.init, (state): UsersState => ({
    ...state,
    userDetailLoading: true,
  })),
  on(actions.loadUser.success, (state, { user }): UsersState => ({
    ...state,
    userDetail: user,
    userDetailLoading: false,
  })),
  on(actions.loadUser.fail, (state, { error }): UsersState => ({
    ...state,
    userDetail: undefined,
    userDetailLoading: false,
    userDetailLoadingError: error,
  })),
  on(
    actions.removeUsersFromWorkspace.success,
    (state, { emailAddresses }): UsersState =>
      adapter.removeMany(emailAddresses, {
        ...state,
        loading: false,
        loaded: true,
      })
  ),
  on(actions.toggleUserPermission.init, (state): UsersState => ({
    ...state,
    loading: true,
  })),
  on(actions.toggleUserPermission.success, (state): UsersState => ({
    ...state,
    loading: false,
  })),
  on(actions.toggleUserPermission.fail, (state, { error }): UsersState => ({
    ...state,
    loading: false,
    loadingError: error,
  })),
  on(actions.updateWorkspaceRole.init, (state): UsersState => ({
    ...state,
    loading: true,
  })),
  on(
    actions.updateWorkspaceRole.success,
    (state, { userId, role, permissions }): UsersState => ({
      ...state,
      loading: false,
      userDetail:
        state.userDetail?.id === userId
          ? { ...state.userDetail, role, permissions }
          : state.userDetail,
    })
  ),
  on(actions.updateWorkspaceRole.fail, (state, { error }): UsersState => ({
    ...state,
    loading: false,
    loadingError: error,
  }))
);

export const usersReducer = (
  state: UsersState | undefined,
  action: Action
): UsersState => reducer(state, action);
