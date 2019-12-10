import { AsyncEntityState } from '@core/entity/async-entity-state';
import { Workspace } from '@core/models/workspace';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<Workspace>();

export const initialState = adapter.getInitialState({
  loading: false,
  loaded: false,
  loadingCreate: false,
});

export interface WorkspacesState extends AsyncEntityState<Workspace> {}
