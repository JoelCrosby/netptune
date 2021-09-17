import { Action, ActionReducer, INIT, UPDATE } from '@ngrx/store';
import { environment } from '@env/environment';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { AppState } from '@core/core.state';
import { Logger } from '@core/util/logger';

export const initStateFromLocalStorage =
  (
    reducer: ActionReducer<Partial<AppState>>
  ): ActionReducer<Partial<AppState>> =>
  (state, action) => {
    const newState = reducer(state, action);

    if ([INIT.toString(), UPDATE.toString()].includes(action.type)) {
      const mergedState = {
        ...newState,
        ...LocalStorageService.loadInitialState(),
      };

      if (!environment.production) {
        logStorageStateChange(action, state, newState, mergedState);
      }

      return mergedState;
    }

    return newState;
  };

const logStorageStateChange = (
  action: Action,
  state: Partial<AppState> | undefined,
  newState: Partial<AppState>,
  mergedState: Partial<AppState>
) => {
  const { type, ...payload } = action;

  Logger.log(
    `%c[NGRX][LocalStorage] %c${type}`,
    'color: #D171E1',
    'color: inherit',
    {
      payload,
      oldState: state,
      newState,
      mergedState,
    }
  );
};
