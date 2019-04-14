import { Workspace } from '../models/workspace';
import { CoreActionTypes, CoreActions } from './core.actions';

export interface CoreState {
  currentWorksapce?: Workspace;
}

export const initialState: CoreState = {};

export function coreReducer(state = initialState, action: CoreActions): CoreState {
  switch (action.type) {
    case CoreActionTypes.SelectWorkspace:
      return { ...state, currentWorksapce: action.payload };
    default:
      return state;
  }
}
