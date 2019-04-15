import { Workspace } from '../models/workspace';
import { CoreActionTypes, CoreActions } from './core.actions';
import { Project } from '../models/project';

export interface CoreState {
  currentWorksapce?: Workspace;
  currentProject?: Project;
}

export const initialState: CoreState = {};

export function coreReducer(state = initialState, action: CoreActions): CoreState {
  switch (action.type) {
    case CoreActionTypes.SelectWorkspace:
      return { ...state, currentWorksapce: action.payload };
    case CoreActionTypes.SelectProject:
      return { ...state, currentProject: action.payload };
    default:
      return state;
  }
}
