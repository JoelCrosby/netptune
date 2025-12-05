import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './workspaces.actions';
import { adapter, initialState, WorkspacesState } from './workspaces.model';

const reducer = createReducer(
  initialState,

  // Load Workspaces

  on(
    actions.loadWorkspaces,
    (state): WorkspacesState => ({ ...state, loading: true })
  ),
  on(
    actions.loadWorkspacesFail,
    (state, { error }): WorkspacesState => ({
      ...state,
      loadingError: error,
      loading: false,
    })
  ),
  on(
    actions.loadWorkspacesSuccess,
    (state, { workspaces }): WorkspacesState =>
      adapter.setAll(workspaces, { ...state, loading: false, loaded: true })
  ),

  // Create Workspaces

  on(
    actions.createWorkspace,
    (state): WorkspacesState => ({ ...state, loadingCreate: true })
  ),
  on(
    actions.createWorkspaceFail,
    (state, { error }): WorkspacesState => ({
      ...state,
      loadingError: error,
    })
  ),
  on(
    actions.createWorkspaceSuccess,
    (state, { workspace }): WorkspacesState =>
      adapter.addOne(workspace, {
        ...state,
        loadingCreate: false,
      })
  ),

  // Edit Workspaces

  on(
    actions.editWorkspace,
    (state): WorkspacesState => ({ ...state, loadingEdit: true })
  ),
  on(
    actions.editWorkspaceFail,
    (state, { error }): WorkspacesState => ({
      ...state,
      loadingEditError: error,
    })
  ),
  on(
    actions.editWorkspaceSuccess,
    (state, { workspace }): WorkspacesState =>
      adapter.upsertOne(workspace, {
        ...state,
        loadingEdit: false,
      })
  ),

  // Select Workspaces

  on(
    actions.selectWorkspace,
    (state, { workspace }): WorkspacesState => ({
      ...state,
      currentWorkspace: workspace,
    })
  ),

  // Delete Workspace

  on(
    actions.deleteWorkspaceSuccess,
    (state, { workspace }): WorkspacesState => {
      if (workspace.id === undefined || workspace.id === null) {
        return state;
      }

      return adapter.removeOne(workspace.id, state);
    }
  ),

  // Is Slug Unique

  on(
    actions.isSlugUniue,
    (state): WorkspacesState => ({ ...state, isSlugUniqueLoading: true })
  ),
  on(
    actions.isSlugUniueFail,
    (state, { error }): WorkspacesState => ({
      ...state,
      isSlugUniqueError: error,
      isSlugUniqueLoading: false,
    })
  ),
  on(
    actions.isSlugUniueSuccess,
    (state, { response }): WorkspacesState => ({
      ...state,
      isSlugUnique: response,
      isSlugUniqueLoading: false,
    })
  )
);

export const workspacesReducer = (
  state: WorkspacesState | undefined,
  action: Action
): WorkspacesState => reducer(state, action);
