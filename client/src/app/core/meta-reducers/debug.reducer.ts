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
    const error = type.includes('Fail');

    const log = console[error ? 'error' : 'info'];

    log(`%c[NGRX] %c${type}`, 'color: #D171E1', 'color: inherit', {
      payload,
      oldState: state,
      newState,
    });

    return newState;
  };
}
