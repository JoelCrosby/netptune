import { EntityState, createEntityAdapter, EntityAdapter } from '@ngrx/entity';
import { AppUser } from '@app/core/models/appuser';
import { UsersActions, UsersActionTypes } from './users.actions';

export interface UsersState {
  users: Users;
  loading: boolean;
  loaded: boolean;
  loadUsersError?: any;
  loadingCreateUser: boolean;
}

export const initialState: UsersState = {
  users: { ids: [], entities: {} },
  loading: false,
  loaded: false,
  loadingCreateUser: false,
};

export interface Users extends EntityState<AppUser> {}

export const adapter: EntityAdapter<AppUser> = createEntityAdapter<AppUser>();

export function usersReducer(state = initialState, action: UsersActions): UsersState {
  switch (action.type) {
    case UsersActionTypes.LoadUsers:
      return { ...state, loading: true };
    case UsersActionTypes.LoadUsersFail:
      return { ...state, loading: false, loadUsersError: action.payload };
    case UsersActionTypes.LoadUsersSuccess:
      return {
        ...state,
        loading: false,
        loaded: true,
        users: adapter.addAll(action.payload, state.users),
      };
    default:
      return state;
  }
}

const { selectIds, selectEntities, selectAll, selectTotal } = adapter.getSelectors();

export const selectUserIds = selectIds;
export const selectUserEntities = selectEntities;
export const selectAllUsers = selectAll;
export const selectUsersTotal = selectTotal;
