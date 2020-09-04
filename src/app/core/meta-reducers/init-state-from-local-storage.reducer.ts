import { Action, ActionReducer, INIT, UPDATE } from '@ngrx/store';
import { environment } from '@env/environment';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { AppState } from '@core/core.state';

export function initStateFromLocalStorage(
  reducer: ActionReducer<AppState>
): ActionReducer<AppState> {
  return (state, action) => {
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
}

const logStorageStateChange = (
  action: Action,
  state: AppState,
  newState: AppState,
  mergedState: AppState
) => {
  const { type, ...payload } = action;

  console.log(`[DEBUG - InitStateFromLocalStorage] action: ${type}`, {
    payload,
    oldState: state,
    newState,
    mergedState,
  });
};
