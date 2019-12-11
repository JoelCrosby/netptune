import { CoreState, initialState } from './core.model';
import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './core.actions';

const reducer = createReducer(
  initialState,
  on(actions.selectWorkspace, (state, { workspace }) => ({
    ...state,
    currentWorksapce: workspace,
  })),
  on(actions.selectProject, (state, { project }) => ({
    ...state,
    currentProject: project,
  }))
);

export function coreReducer(state: CoreState | undefined, action: Action) {
  return reducer(state, action);
}
