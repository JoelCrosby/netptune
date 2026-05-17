import { Action, createReducer, on } from '@ngrx/store';
import { initialState, ActivityState } from './activity.model';
import * as actions from './activity.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): ActivityState => initialState),

  // Load Projects

  on(
    actions.loadActivity,
    (state): ActivityState => ({
      ...state,
      loading: true,
      nextCursor: undefined,
    })
  ),
  on(
    actions.loadMoreActivity,
    (state): ActivityState => ({ ...state, loadingMore: true })
  ),
  on(
    actions.loadActivityFail,
    (state, { error }): ActivityState => ({
      ...state,
      loading: false,
      loadingMore: false,
      loaded: true,
      loadingError: error,
    })
  ),
  on(
    actions.loadActivitySuccess,
    (state, { activities, nextCursor, pageSize }): ActivityState => ({
      ...state,
      loading: false,
      loaded: true,
      activities,
      nextCursor,
      pageSize,
    })
  ),
  on(
    actions.loadMoreActivitySuccess,
    (state, { activities, nextCursor, pageSize }): ActivityState => ({
      ...state,
      loadingMore: false,
      loaded: true,
      activities: [...state.activities, ...activities],
      nextCursor,
      pageSize,
    })
  )
);

export const activityReducer = (
  state: ActivityState | undefined,
  action: Action
): ActivityState => reducer(state, action);
