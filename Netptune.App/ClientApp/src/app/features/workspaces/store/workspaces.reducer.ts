import { WorkspacesActions, WorkspacesActionTypes } from './workspaces.actions';
import { Workspace } from '@core/models/workspace';
import { EntityState, createEntityAdapter, EntityAdapter } from '@ngrx/entity';

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

export const adapter: EntityAdapter<Workspace> = createEntityAdapter<Workspace>();

export function workspacesReducer(
  state = initialState,
  action: WorkspacesActions
): WorkspacesState {
  switch (action.type) {
    case WorkspacesActionTypes.LoadWorkspaces:
      return { ...state, loading: true };
    case WorkspacesActionTypes.LoadWorkspacesFail:
      return { ...state, loading: false, loadWorkspacesError: action.payload };
    case WorkspacesActionTypes.LoadWorkspacesSuccess:
      return {
        ...state,
        loading: false,
        loaded: true,
        Workspaces: adapter.addAll(action.payload, state.Workspaces),
      };
    case WorkspacesActionTypes.CreateWorkspace:
      return { ...state, loadingCreateWorkspace: true };
    case WorkspacesActionTypes.CreateWorkspaceFail:
      return { ...state, loadingCreateWorkspace: false, loadWorkspacesError: action.payload };
    case WorkspacesActionTypes.CreateWorkspaceSuccesss:
      return {
        ...state,
        loadingCreateWorkspace: false,
        Workspaces: adapter.addOne(action.payload, state.Workspaces),
      };
    default:
      return state;
  }
}

const { selectIds, selectEntities, selectAll, selectTotal } = adapter.getSelectors();

export const selectWorkspaceIds = selectIds;
export const selectWorkspaceEntities = selectEntities;
export const selectAllWorkspaces = selectAll;
export const selectWorkspacesTotal = selectTotal;
