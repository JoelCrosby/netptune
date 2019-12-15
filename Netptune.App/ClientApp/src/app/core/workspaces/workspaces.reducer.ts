import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './workspaces.actions';
import { adapter, initialState, WorkspacesState } from './workspaces.model';

const reducer = createReducer(
  initialState,
  on(actions.loadWorkspaces, state => ({ ...state, loading: true })),
  on(actions.loadWorkspacesFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.loadWorkspacesSuccess, (state, { workspaces }) =>
    adapter.addAll(workspaces, { ...state, loading: false, loaded: true })
  ),
  on(actions.createWorkspace, state => ({ ...state, loading: true })),
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
  on(actions.selectWorkspace, (state, { workspace }) => ({
    ...state,
    currentWorkspace: workspace,
  })),
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
