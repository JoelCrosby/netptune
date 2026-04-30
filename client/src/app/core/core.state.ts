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
import { ActivityState } from './store/activity/activity.model';
import { activityReducer } from './store/activity/activity.reducer';
import { AuthState } from './store/auth/auth.models';
import { authReducer } from './store/auth/auth.reducer';
import { BoardsState } from './store/boards/boards.model';
import { boardsReducer } from './store/boards/boards.reducer';
import { BoardGroupsState } from './store/groups/board-groups.model';
import { boardGroupsReducer } from './store/groups/board-groups.reducer';
import {
  hubContextReducer,
  HubContextState,
} from './store/hub-context/hub-context.reducer';
import { layoutReducer, LayoutState } from './store/layout/layout.reducer';
import { MetaState } from './store/meta/meta.model';
import { metaReducer } from './store/meta/meta.reducer';
import { NotificationsState } from './store/notifications/notifications.model';
import { notificationsReducer } from './store/notifications/notifications.reducer';
import { ProfileState } from './store/profile/profile.model';
import { profileReducer } from './store/profile/profile.reducer';
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
  notifications: notificationsReducer,
  boards: boardsReducer,
  boardgroups: boardGroupsReducer,
  profile: profileReducer,
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
export const selectActivitesFeature = selectFeature<ActivityState>('activites');
export const selectLayoutFeature = selectFeature<LayoutState>('layout');
export const selectSettingsFeature = selectFeature<SettingsState>('settings');
export const selectWorkspacesFeature =
  selectFeature<WorkspacesState>('workspaces');
export const selectProjectsFeature = selectFeature<ProjectsState>('projects');
export const selectTasksFeature = selectFeature<TasksState>('tasks');
export const selectUsersFeature = selectFeature<UsersState>('users');
export const selectTagsFeature = selectFeature<TagsState>('tags');
export const selectHubContextFeature = selectFeature<HubContextState>('hub');
export const selectNotificationsFeature =
  selectFeature<NotificationsState>('notifications');

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
  notifications: NotificationsState;
  boards: BoardsState;
  boardgroups: BoardGroupsState;
  profile: ProfileState;
}
