import { Action, createReducer, on } from '@ngrx/store';
import { initialState, ActivityState } from './activity.model';
import * as actions from './activity.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): ActivityState => initialState),

  // Load Projects

  on(
    actions.loadActivity,
    (state): ActivityState => ({ ...state, loading: true })
  ),
  on(
    actions.loadActivityFail,
    (state, { error }): ActivityState => ({
      ...state,
      loading: false,
      loaded: true,
      loadingError: error,
    })
  ),
  on(
    actions.loadActivitySuccess,
    (state, { activities }): ActivityState => ({
      ...state,
      loading: false,
      loaded: true,
      activities,
    })
  )
);

export const activityReducer = (
  state: ActivityState | undefined,
  action: Action
): ActivityState => reducer(state, action);
