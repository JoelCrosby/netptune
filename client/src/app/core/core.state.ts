import { environment } from '@env/environment';
import { routerReducer, RouterReducerState } from '@ngrx/router-store';
import {
  ActionReducerMap,
  createFeatureSelector,
  MetaReducer,
} from '@ngrx/store';
import { clearState } from './meta-reducers/clear-state';
import { debug } from './meta-reducers/debug.reducer';
import { initStateFromLocalStorage } from './meta-reducers/init-state-from-local-storage.reducer';
import { RouterStateUrl } from './router/router.state';
import type { AuthState } from './store/auth/auth.models';
import { authReducer } from './store/auth/auth.reducer';
import type { SettingsState } from './store/settings/settings.model';
import { settingsReducer } from './store/settings/settings.reducer';
import type { WorkspacesState } from './store/workspaces/workspaces.model';
import { workspacesReducer } from './store/workspaces/workspaces.reducer';

export const reducers: ActionReducerMap<Partial<AppState>> = {
  auth: authReducer,
  router: routerReducer,
  settings: settingsReducer,
  workspaces: workspacesReducer,
};

export const metaReducers: MetaReducer<Partial<AppState>>[] = [
  initStateFromLocalStorage,
  clearState,
];

if (!environment.production) {
  metaReducers.unshift(debug);
}

const selectFeature = <TState>(name: keyof AppState) =>
  createFeatureSelector<TState>(name);

export const selectAuthFeature = selectFeature<AuthState>('auth');
export const selectSettingsFeature = selectFeature<SettingsState>('settings');
export const selectWorkspacesFeature =
  selectFeature<WorkspacesState>('workspaces');

export interface AppState {
  auth: AuthState;
  router: RouterReducerState<RouterStateUrl>;
  settings: SettingsState;
  workspaces: WorkspacesState;
}
