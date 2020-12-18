import { WorkspaceAppUser } from '@core/models/appuser';
import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<WorkspaceAppUser>({
  selectId: (user: WorkspaceAppUser) => user.email,
});

export const initialState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
});

export type UsersState = AsyncEntityState<WorkspaceAppUser>;
