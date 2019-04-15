import { Action, ActionReducer } from '@ngrx/store';
import { AuthActionTypes } from '../auth/store/auth.actions';
import { AppState } from '../core.state';

export function clearState(reducer: ActionReducer<AppState>): ActionReducer<AppState> {
  return function(state: AppState, action: Action) {
    if (action.type === AuthActionTypes.LOGOUT) {
      state = undefined;
    }

    return reducer(state, action);
  };
}
