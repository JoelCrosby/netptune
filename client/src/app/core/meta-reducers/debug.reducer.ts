import { ActionReducer } from '@ngrx/store';
import { AppState } from '../core.state';

export const debug =
  (
    reducer: ActionReducer<Partial<AppState>>
  ): ActionReducer<Partial<AppState>> =>
  (state, action) => {
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
