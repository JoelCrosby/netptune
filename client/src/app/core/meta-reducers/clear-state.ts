import { Action, ActionReducer } from '@ngrx/store';
import { logoutSuccess } from '@core/auth/store/auth.actions';
import { AppState } from '@core/core.state';

export const clearState =
  (
    reducer: ActionReducer<Partial<AppState>>
  ): ActionReducer<Partial<AppState>> =>
  (state: Partial<AppState> | undefined, action: Action) => {
    if (action.type === logoutSuccess.type) {
      state = undefined;
    }

    return reducer(state, action);
  };
