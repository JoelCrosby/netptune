import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './workspaces.actions';
import { adapter, initialState, WorkspacesState } from './workspaces.model';

const reducer = createReducer(
  initialState,

  // Load Workspaces

  on(actions.loadWorkspaces, (state) => ({ ...state, loading: true })),
  on(actions.loadWorkspacesFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.loadWorkspacesSuccess, (state, { workspaces }) =>
    adapter.setAll(workspaces, { ...state, loading: false, loaded: true })
  ),

  // Create Workspaces

  on(actions.createWorkspace, (state) => ({ ...state, loadingCreate: true })),
  on(actions.createWorkspaceFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.createWorkspaceSuccess, (state, { workspace }) =>
    adapter.addOne(workspace, {
      ...state,
      loadingCreate: false,
    })
  ),

  // Edit Workspaces

  on(actions.editWorkspace, (state) => ({ ...state, loadingEdit: true })),
  on(actions.editWorkspaceFail, (state, { error }) => ({
    ...state,
    editError: error,
  })),
  on(actions.editWorkspaceSuccess, (state, { workspace }) =>
    adapter.upsertOne(workspace, {
      ...state,
      loadingEdit: false,
    })
  ),

  // Select Workspaces

  on(actions.selectWorkspace, (state, { workspace }) => ({
    ...state,
    currentWorkspace: workspace,
  })),

  // Delete Workspace

  on(actions.deleteWorkspaceSuccess, (state, { workspace }) =>
    adapter.removeOne(workspace.id, state)
  )
);

export function workspacesReducer(
  state: WorkspacesState | undefined,
  action: Action
) {
  return reducer(state, action);
}
