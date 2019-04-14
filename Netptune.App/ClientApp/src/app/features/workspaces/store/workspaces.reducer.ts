import { WorkspacesActions, WorkspacesActionTypes } from './workspaces.actions';
import { Workspace } from '@app/core/models/workspace';
import { Maybe } from '@app/core/types/nothing';

export interface WorkspacesState {
  Workspaces: Workspace[];
  loading: boolean;
  loaded: boolean;
  loadWorkspacesError?: any;
  currentWorkspace?: Workspace;
  loadingCreateWorkspace: boolean;
}

export const initialState: WorkspacesState = {
  Workspaces: [],
  loading: false,
  loaded: false,
  loadingCreateWorkspace: false,
};

export function workspacesReducer(
  state = initialState,
  action: WorkspacesActions
): WorkspacesState {
  switch (action.type) {
    case WorkspacesActionTypes.SelectWorkspace:
      return { ...state, currentWorkspace: action.payload };
    case WorkspacesActionTypes.LoadWorkspaces:
      return { ...state, loading: true };
    case WorkspacesActionTypes.LoadWorkspacesFail:
      return { ...state, loading: false, loadWorkspacesError: action.payload };
    case WorkspacesActionTypes.LoadWorkspacesSuccess:
      return { ...state, loading: false, loaded: true, Workspaces: action.payload };
    case WorkspacesActionTypes.CreateWorkspace:
      return { ...state, loadingCreateWorkspace: true };
    case WorkspacesActionTypes.CreateWorkspaceFail:
      return { ...state, loadingCreateWorkspace: false, loadWorkspacesError: action.payload };
    case WorkspacesActionTypes.CreateWorkspaceSuccesss:
      return {
        ...state,
        loadingCreateWorkspace: false,
        Workspaces: [...state.Workspaces, action.payload],
      };
    default:
      return state;
  }
}
