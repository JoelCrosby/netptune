import { ActivityState } from './store/activity/activity.model';
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
import {
  hubContextReducer,
  HubContextState,
} from './store/hub-context/hub-context.reducer';
import { layoutReducer, LayoutState } from './store/layout/layout.reducer';
import { MetaState } from './store/meta/meta.model';
import { metaReducer } from './store/meta/meta.reducer';
import { ProjectsState } from './store/projects/projects.model';
import { projectsReducer } from './store/projects/projects.reducer';
import { SettingsState } from './store/settings/settings.model';
import { settingsReducer } from './store/settings/settings.reducer';
import { TagsState } from './store/tags/tags.model';
import { tagsReducer } from './store/tags/tags.reducer';
import { TasksState } from './store/tasks/tasks.model';
import { projectTasksReducer } from './store/tasks/tasks.reducer';
import { UsersState } from './store/users/users.model';
import { usersReducer } from './store/users/users.reducer';
import { WorkspacesState } from './store/workspaces/workspaces.model';
import { workspacesReducer } from './store/workspaces/workspaces.reducer';
import { activityReducer } from './store/activity/activity.reducer';

export const reducers: ActionReducerMap<Partial<AppState>> = {
  auth: authReducer,
  meta: metaReducer,
  activites: activityReducer,
  router: routerReducer,
  layout: layoutReducer,
  settings: settingsReducer,
  workspaces: workspacesReducer,
  projects: projectsReducer,
  tasks: projectTasksReducer,
  users: usersReducer,
  tags: tagsReducer,
  hub: hubContextReducer,
};

export const metaReducers: MetaReducer<AppState>[] = [
  initStateFromLocalStorage,
  clearState,
];

if (!environment.production) {
  metaReducers.unshift(debug);
}

const feature = <TState>(name: keyof AppState) =>
  createFeatureSelector<AppState, TState>(name);

export const selectAuthFeature = feature<AuthState>('auth');
export const selectMetaFeature = feature<MetaState>('meta');
export const selectActivitesFeature = feature<ActivityState>('activites');
export const selectLayoutFeature = feature<LayoutState>('layout');
export const selectSettingsFeature = feature<SettingsState>('settings');
export const selectWorkspacesFeature = feature<WorkspacesState>('workspaces');
export const selectProjectsFeature = feature<ProjectsState>('projects');
export const selectTasksFeature = feature<TasksState>('tasks');
export const selectUsersFeature = feature<UsersState>('users');
export const selectTagsFeature = feature<TagsState>('tags');
export const selectHubContextFeature = feature<HubContextState>('hub');

export interface AppState {
  auth: AuthState;
  meta: MetaState;
  activites: ActivityState;
  router: RouterReducerState<RouterStateUrl>;
  layout: LayoutState;
  settings: SettingsState;
  workspaces: WorkspacesState;
  projects: ProjectsState;
  tasks: TasksState;
  users: UsersState;
  tags: TagsState;
  hub: HubContextState;
}
