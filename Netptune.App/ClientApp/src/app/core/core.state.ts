import { environment } from '@env/environment';
import { routerReducer, RouterReducerState } from '@ngrx/router-store';
import {
  ActionReducerMap,
  createFeatureSelector,
  createSelector,
  MetaReducer,
} from '@ngrx/store';
import { authReducer } from './auth/store/auth.reducer';
import { AuthState } from './auth/store/auth.models';
import { clearState } from './meta-reducers/clear-state';
import { debug } from './meta-reducers/debug.reducer';
import { initStateFromLocalStorage } from './meta-reducers/init-state-from-local-storage.reducer';
import { RouterStateUrl } from './router/router.state';
import { SettingsState } from './settings/settings.model';
import { settingsReducer } from './settings/settings.reducer';
import { CoreState } from './state/core.model';
import { coreReducer } from './state/core.reducer';
import { WorkspacesState } from './workspaces/workspaces.model';
import { workspacesReducer } from './workspaces/workspaces.reducer';

export const reducers: ActionReducerMap<AppState> = {
  auth: authReducer,
  router: routerReducer,
  core: coreReducer,
  settings: settingsReducer,
  workspaces: workspacesReducer,
};

export const metaReducers: MetaReducer<AppState>[] = [
  initStateFromLocalStorage,
  clearState,
];

if (!environment.production) {
  metaReducers.unshift(debug);
}

export const selectAuthState = createFeatureSelector<AppState, AuthState>(
  'auth'
);

export const selectCoreState = createFeatureSelector<AppState, CoreState>(
  'core'
);

export const selectSettingsState = createFeatureSelector<
  AppState,
  SettingsState
>('settings');

export const selectWorkspacesFeature = createFeatureSelector<
  AppState,
  WorkspacesState
>('workspaces');

export const selectRouterState = createFeatureSelector<
  RouterReducerState<RouterStateUrl>
>('router');

export const selectPageTitle = createSelector(
  selectRouterState,
  (state: RouterReducerState<RouterStateUrl>) =>
    state && state.state && state.state.title
);

export interface AppState {
  auth: AuthState;
  router: RouterReducerState<RouterStateUrl>;
  core: CoreState;
  settings: SettingsState;
  workspaces: WorkspacesState;
}
