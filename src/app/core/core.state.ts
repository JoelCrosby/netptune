import { environment } from '@env/environment';
import { routerReducer, RouterReducerState } from '@ngrx/router-store';
import {
  ActionReducerMap,
  createFeatureSelector,
  createSelector,
  MetaReducer,
  MemoizedSelector,
} from '@ngrx/store';
import { authReducer } from './auth/store/auth.reducer';
import { AuthState } from './auth/store/auth.models';
import { clearState } from './meta-reducers/clear-state';
import { debug } from './meta-reducers/debug.reducer';
import { initStateFromLocalStorage } from './meta-reducers/init-state-from-local-storage.reducer';
import { RouterStateUrl } from './router/router.state';
import { SettingsState } from './store/settings/settings.model';
import { settingsReducer } from './store/settings/settings.reducer';
import { CoreState } from './store/core/core.model';
import { coreReducer } from './store/core/core.reducer';
import { WorkspacesState } from './store/workspaces/workspaces.model';
import { workspacesReducer } from './store/workspaces/workspaces.reducer';
import { ProjectsState } from './store/projects/projects.model';
import { projectsReducer } from './store/projects/projects.reducer';
import { TasksState } from './store/tasks/tasks.model';
import { projectTasksReducer } from './store/tasks/tasks.reducer';

export const reducers: ActionReducerMap<AppState> = {
  auth: authReducer,
  router: routerReducer,
  core: coreReducer,
  settings: settingsReducer,
  workspaces: workspacesReducer,
  projects: projectsReducer,
  tasks: projectTasksReducer,
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

export const selectProjectsFeature = createFeatureSelector<
  AppState,
  ProjectsState
>('projects');

export const selectTasksFeature = createFeatureSelector<AppState, TasksState>(
  'tasks'
);

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
  projects: ProjectsState;
  tasks: TasksState;
}
