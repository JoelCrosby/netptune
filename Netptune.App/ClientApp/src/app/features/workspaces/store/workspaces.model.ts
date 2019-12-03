import { createEntityAdapter, EntityAdapter, EntityState } from '@ngrx/entity';
import { Workspace } from '@core/models/workspace';
import { AsyncEntityState } from '@core/entity/async-entity-state';

export const adapter: EntityAdapter<Workspace> = createEntityAdapter<
  Workspace
>();

export const initialState = adapter.getInitialState({
  loading: false,
  loaded: false,
  loadingCreate: false,
});

export interface WorkspacesState extends AsyncEntityState<Workspace> {}
