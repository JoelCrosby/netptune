import { WorkspaceAppUser } from '@core/models/appuser';
import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<WorkspaceAppUser>({
  selectId: (user: WorkspaceAppUser) => user.email,
});

export const initialState: UsersState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
  userDetailLoading: true,
});

export interface UsersState extends AsyncEntityState<WorkspaceAppUser> {
  userDetail?: WorkspaceAppUser;
  userDetailLoading: boolean;
  userDetailLoadingError?: Error;
}
