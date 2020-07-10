import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { Workspace } from '@core/models/workspace';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<Workspace>();

export const initialState: WorkspacesState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
});

export interface WorkspacesState extends AsyncEntityState<Workspace> {
  currentWorkspace?: Workspace;
}
