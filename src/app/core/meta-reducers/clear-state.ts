import { Action, ActionReducer } from '@ngrx/store';
import { logout } from '../auth/store/auth.actions';
import { AppState } from '../core.state';

export function clearState(
  reducer: ActionReducer<AppState>
): ActionReducer<AppState> {
  return (state: AppState, action: Action) => {
    if (action.type === logout.type) {
      state = undefined;
    }

    return reducer(state, action);
  };
}
