import { Action, createReducer, on } from '@ngrx/store';
import { initialState, MetaState } from './meta.model';
import * as actions from './meta.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),

  // Load Projects

  on(actions.loadBuildInfo, (state) => ({ ...state, loading: true })),
  on(actions.loadBuildInfoFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.loadBuildInfoSuccess, (state, { buildInfo }) => ({
    ...state,
    buildInfo,
  }))
);

export function metaReducer(
  state: MetaState | undefined,
  action: Action
): MetaState {
  return reducer(state, action);
}
