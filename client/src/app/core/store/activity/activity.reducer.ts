import { Action, createReducer, on } from '@ngrx/store';
import { initialState, ActivityState } from './activity.model';
import * as actions from './activity.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),

  // Load Projects

  on(actions.loadActivity, (state) => ({ ...state, loading: true })),
  on(actions.loadActivityFail, (state, { error }) => ({
    ...state,
    loading: false,
    loaded: true,
    loadingError: error,
  })),
  on(actions.loadActivitySuccess, (state, { activities }) => ({
    ...state,
    loading: false,
    loaded: true,
    activities,
  }))
);

export const activityReducer = (
  state: ActivityState | undefined,
  action: Action
): ActivityState => reducer(state, action);
