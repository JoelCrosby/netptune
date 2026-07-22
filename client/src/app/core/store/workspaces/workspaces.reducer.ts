import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './workspaces.actions';
import { adapter, initialState, WorkspacesState } from './workspaces.model';

const reducer = createReducer(
  initialState,

  // Load Workspaces

  on(actions.loadWorkspaces.init, (state): WorkspacesState => ({
    ...state,
    loading: true,
  })),
  on(actions.loadWorkspaces.fail, (state, { error }): WorkspacesState => ({
    ...state,
    loadingError: error,
    loading: false,
  })),
  on(actions.loadWorkspaces.success, (state, { workspaces }): WorkspacesState =>
    adapter.setAll(workspaces, { ...state, loading: false, loaded: true })
  ),

  // Create Workspaces

  on(actions.createWorkspace.init, (state): WorkspacesState => ({
    ...state,
    loadingCreate: true,
  })),
  on(actions.createWorkspace.fail, (state, { error }): WorkspacesState => ({
    ...state,
    loadingError: error,
  })),
  on(actions.createWorkspace.success, (state, { workspace }): WorkspacesState =>
    adapter.addOne(workspace, {
      ...state,
      loadingCreate: false,
    })
  ),

  // Edit Workspaces

  on(actions.editWorkspace.init, (state): WorkspacesState => ({
    ...state,
    loadingEdit: true,
  })),
  on(actions.editWorkspace.fail, (state, { error }): WorkspacesState => ({
    ...state,
    loadingEditError: error,
  })),
  on(actions.editWorkspace.success, (state, { workspace }): WorkspacesState =>
    adapter.upsertOne(workspace, {
      ...state,
      loadingEdit: false,
      currentWorkspace:
        state.currentWorkspace?.slug === workspace.slug
          ? workspace
          : state.currentWorkspace,
    })
  ),

  // Set Current Workspace

  on(actions.setCurrentWorkspace, (state, { workspace }): WorkspacesState => ({
    ...state,
    currentWorkspace: workspace,
  })),

  // Select Workspace

  on(actions.selectWorkspace, (state, { workspace }): WorkspacesState => ({
    ...state,
    currentWorkspace: workspace,
  })),

  // Delete Workspace

  on(
    actions.deleteWorkspace.success,
    (state, { workspace }): WorkspacesState => {
      if (workspace.id === undefined || workspace.id === null) {
        return state;
      }

      return adapter.removeOne(workspace.id, {
        ...state,
        currentWorkspace:
          state.currentWorkspace?.id === workspace.id
            ? undefined
            : state.currentWorkspace,
      });
    }
  ),

  // Leave Workspace

  on(
    actions.leaveWorkspace.success,
    (state, { workspace }): WorkspacesState => {
      if (workspace.id === undefined || workspace.id === null) {
        return state;
      }

      return adapter.removeOne(workspace.id, {
        ...state,
        currentWorkspace:
          state.currentWorkspace?.id === workspace.id
            ? undefined
            : state.currentWorkspace,
      });
    }
  ),

  // Is Slug Unique

  on(actions.isSlugUniue.init, (state): WorkspacesState => ({
    ...state,
    isSlugUniqueLoading: true,
  })),
  on(actions.isSlugUniue.fail, (state, { error }): WorkspacesState => ({
    ...state,
    isSlugUniqueError: error,
    isSlugUniqueLoading: false,
  })),
  on(actions.isSlugUniue.success, (state, { response }): WorkspacesState => ({
    ...state,
    isSlugUnique: response,
    isSlugUniqueLoading: false,
  }))
);

export const workspacesReducer = (
  state: WorkspacesState | undefined,
  action: Action
): WorkspacesState => reducer(state, action);
