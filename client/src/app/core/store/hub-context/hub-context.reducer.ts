import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './hub-context.actions';

const initialState: HubContextState = {};

export interface HubContextState {
  groupId?: string;
}

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),

  on(actions.setCurrentGroupId, (state, { groupId: identifier }) => ({
    ...state,
    groupId: identifier,
  }))
);

export const hubContextReducer = (
  state: HubContextState | undefined,
  action: Action
): HubContextState => reducer(state, action);
