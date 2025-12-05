import { Action, createReducer, on } from '@ngrx/store';
import { initialState, MetaState } from './meta.model';
import * as actions from './meta.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): MetaState => initialState),

  // Load Projects

  on(
    actions.loadBuildInfo,
    (state): MetaState => ({ ...state, loading: true })
  ),
  on(
    actions.loadBuildInfoFail,
    (state, { error }): MetaState => ({
      ...state,
      loadingError: error,
    })
  ),
  on(
    actions.loadBuildInfoSuccess,
    (state, { buildInfo }): MetaState => ({
      ...state,
      buildInfo,
    })
  )
);

export const metaReducer = (
  state: MetaState | undefined,
  action: Action
): MetaState => reducer(state, action);
