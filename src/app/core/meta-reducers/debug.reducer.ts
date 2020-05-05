import { ActionReducer } from '@ngrx/store';
import { AppState } from '../core.state';

export function debug(
  reducer: ActionReducer<AppState>
): ActionReducer<AppState> {
  return (state, action) => {
    const newState = reducer(state, action);

    if (action.type === '@ngrx/store-devtools/recompute') {
      return newState;
    }

    const { type, ...payload } = action;

    console.log(`[DEBUG] action: ${type}`, {
      payload,
      oldState: state,
      newState,
    });
    return newState;
  };
}
