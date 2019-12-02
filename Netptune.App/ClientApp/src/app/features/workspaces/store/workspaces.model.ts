import { createEntityAdapter, EntityAdapter, EntityState } from '@ngrx/entity';
import { Workspace } from '@core/models/workspace';

export interface WorkspacesState {
  Workspaces: Workspaces;
  loading: boolean;
  loaded: boolean;
  loadWorkspacesError?: any;
  loadingCreateWorkspace: boolean;
}

export const initialState: WorkspacesState = {
  Workspaces: { ids: [], entities: {} },
  loading: false,
  loaded: false,
  loadingCreateWorkspace: false,
};

export interface Workspaces extends EntityState<Workspace> {}

export const adapter: EntityAdapter<Workspace> = createEntityAdapter<
  Workspace
>();
