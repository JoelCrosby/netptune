import { environment } from '@env/environment';
import { routerReducer, RouterReducerState } from '@ngrx/router-store';
import {
  ActionReducerMap,
  createFeatureSelector,
  MetaReducer,
} from '@ngrx/store';
import { AuthState } from './auth/store/auth.models';
import { authReducer } from './auth/store/auth.reducer';
import { clearState } from './meta-reducers/clear-state';
import { debug } from './meta-reducers/debug.reducer';
import { initStateFromLocalStorage } from './meta-reducers/init-state-from-local-storage.reducer';
import { RouterStateUrl } from './router/router.state';
import { layoutReducer, LayoutState } from './store/layout/layout.reducer';
import { ProjectsState } from './store/projects/projects.model';
import { projectsReducer } from './store/projects/projects.reducer';
import { SettingsState } from './store/settings/settings.model';
import { settingsReducer } from './store/settings/settings.reducer';
import { TasksState } from './store/tasks/tasks.model';
import { projectTasksReducer } from './store/tasks/tasks.reducer';
import { UsersState } from './store/users/users.model';
import { usersReducer } from './store/users/users.reducer';
import { WorkspacesState } from './store/workspaces/workspaces.model';
import { workspacesReducer } from './store/workspaces/workspaces.reducer';

export const reducers: ActionReducerMap<AppState> = {
  auth: authReducer,
  router: routerReducer,
  layout: layoutReducer,
  settings: settingsReducer,
  workspaces: workspacesReducer,
  projects: projectsReducer,
  tasks: projectTasksReducer,
  users: usersReducer,
};

export const metaReducers: MetaReducer<AppState>[] = [
  initStateFromLocalStorage,
  clearState,
];

if (!environment.production) {
  metaReducers.unshift(debug);
}

function feature<TState>(name: keyof AppState) {
  return createFeatureSelector<AppState, TState>(name);
}

export const selectAuthFeature = feature<AuthState>('auth');
export const selectLayoutFeature = feature<LayoutState>('layout');
export const selectSettingsFeature = feature<SettingsState>('settings');
export const selectWorkspacesFeature = feature<WorkspacesState>('workspaces');
export const selectProjectsFeature = feature<ProjectsState>('projects');
export const selectTasksFeature = feature<TasksState>('tasks');
export const selectUsersFeature = feature<UsersState>('users');

export interface AppState {
  auth: AuthState;
  router: RouterReducerState<RouterStateUrl>;
  layout: LayoutState;
  settings: SettingsState;
  workspaces: WorkspacesState;
  projects: ProjectsState;
  tasks: TasksState;
  users: UsersState;
}
