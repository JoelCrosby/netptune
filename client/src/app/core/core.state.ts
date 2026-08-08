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
import type { BoardGroupsState } from './store/groups/board-groups.model';
import type { HubContextState } from './store/hub-context/hub-context.reducer';
import { layoutReducer, LayoutState } from './store/layout/layout.reducer';
import type { MetaState } from './store/meta/meta.model';
import { metaReducer } from './store/meta/meta.reducer';
import type { SettingsState } from './store/settings/settings.model';
import { settingsReducer } from './store/settings/settings.reducer';
import type { TasksState } from './store/tasks/tasks.model';
import type { WorkspacesState } from './store/workspaces/workspaces.model';
import { workspacesReducer } from './store/workspaces/workspaces.reducer';

export const reducers: ActionReducerMap<Partial<AppState>> = {
  auth: authReducer,
  meta: metaReducer,
  router: routerReducer,
  layout: layoutReducer,
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
export const selectMetaFeature = selectFeature<MetaState>('meta');
export const selectLayoutFeature = selectFeature<LayoutState>('layout');
export const selectSettingsFeature = selectFeature<SettingsState>('settings');
export const selectWorkspacesFeature =
  selectFeature<WorkspacesState>('workspaces');
export const selectTasksFeature = selectFeature<TasksState>('tasks');
export const selectHubContextFeature = selectFeature<HubContextState>('hub');

export interface AppState {
  auth: AuthState;
  meta: MetaState;
  router: RouterReducerState<RouterStateUrl>;
  layout: LayoutState;
  settings: SettingsState;
  workspaces: WorkspacesState;
  tasks: TasksState;
  hub: HubContextState;
  boardgroups: BoardGroupsState;
}
