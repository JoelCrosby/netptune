import { createReducer, on, Action } from '@ngrx/store';
import * as actions from './workspaces.actions';
import { adapter, initialState, WorkspacesState } from './workspaces.model';

const reducer = createReducer(
  initialState,
  on(actions.loadWorkspaces, state => ({ ...state, loading: true })),
  on(actions.loadWorkspacesFail, (state, { error }) => ({
    ...state,
    loadWorkspacesError: error,
  })),
  on(actions.loadWorkspacesSuccess, (state, { workspaces }) => ({
    ...state,
    loading: false,
    loaded: true,
    Workspaces: adapter.addAll(workspaces, state.Workspaces),
  })),
  on(actions.createWorkspace, state => ({ ...state, loading: true })),
  on(actions.createWorkspaceFail, (state, { error }) => ({
    ...state,
    loadWorkspacesError: error,
  })),
  on(actions.createWorkspaceSuccess, (state, { workspace }) => ({
    ...state,
    loadingCreateWorkspace: false,
    Workspaces: adapter.addOne(workspace, state.Workspaces),
  }))
);

export function workspacesReducer(
  state: WorkspacesState | undefined,
  action: Action
) {
  return reducer(state, action);
}
