import { Action, ActionReducer } from '@ngrx/store';
import { logoutSuccess } from '@core/auth/store/auth.actions';
import { AppState } from '@core/core.state';

export function clearState(
  reducer: ActionReducer<AppState>
): ActionReducer<AppState> {
  return (state: AppState, action: Action) => {
    if (action.type === logoutSuccess.type) {
      state = undefined;
    }

    return reducer(state, action);
  };
}
