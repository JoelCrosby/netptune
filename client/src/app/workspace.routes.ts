import { Routes } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';
import { authGuard } from './core/auth/auth.guard';
import { workspaceGuard } from './core/auth/workspace.guard';
import { workspaceResovler } from './core/resolvers/workspace-resolver';
import { ActivityEffects } from './core/store/activity/activity.effects';
import { activityReducer } from './core/store/activity/activity.reducer';
import { BoardsEffects } from './core/store/boards/boards.effects';
import { boardsReducer } from './core/store/boards/boards.reducer';
import { BoardGroupsEffects } from './core/store/groups/board-groups.effects';
import { boardGroupsReducer } from './core/store/groups/board-groups.reducer';
import { hubContextReducer } from './core/store/hub-context/hub-context.reducer';
import { NotificationsEffects } from './core/store/notifications/notifications.effects';
import { notificationsReducer } from './core/store/notifications/notifications.reducer';
import { ProfileEffects } from './core/store/profile/profile.effects';
import { profileReducer } from './core/store/profile/profile.reducer';
import { ProjectsEffects } from './core/store/projects/projects.effects';
import { projectsReducer } from './core/store/projects/projects.reducer';
import { SprintsEffects } from './core/store/sprints/sprints.effects';
import { sprintsReducer } from './core/store/sprints/sprints.reducer';
import { TagsEffects } from './core/store/tags/tags.effects';
import { tagsReducer } from './core/store/tags/tags.reducer';
import { ProjectTasksEffects } from './core/store/tasks/tasks.effects';
import { projectTasksReducer } from './core/store/tasks/tasks.reducer';
import { UsersEffects } from './core/store/users/users.effects';
import { usersReducer } from './core/store/users/users.reducer';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    canActivate: [workspaceGuard],
    resolve: [workspaceResovler],
    providers: [
      provideState('activites', activityReducer),
      provideState('projects', projectsReducer),
      provideState('tasks', projectTasksReducer),
      provideState('users', usersReducer),
      provideState('tags', tagsReducer),
      provideState('hub', hubContextReducer),
      provideState('notifications', notificationsReducer),
      provideState('sprints', sprintsReducer),
      provideState('boards', boardsReducer),
      provideState('boardgroups', boardGroupsReducer),
      provideState('profile', profileReducer),
      provideEffects([
        ActivityEffects,
        NotificationsEffects,
        ProjectsEffects,
        ProjectTasksEffects,
        UsersEffects,
        TagsEffects,
        BoardsEffects,
        ProfileEffects,
        BoardGroupsEffects,
        SprintsEffects,
      ]),
    ],
    loadComponent: () => import('./shell/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        redirectTo: 'projects',
        pathMatch: 'full',
      },
      {
        path: 'projects',
        loadChildren: () => import('./features/projects/projects.routes').then((m) => m.routes),
        data: { title: 'Projects' },
      },
      {
        path: 'tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: 'Tasks' },
      },
      {
        path: 'boards',
        loadChildren: () => import('./features/boards/boards.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: 'Boards' },
      },
      {
        path: 'sprints',
        loadChildren: () => import('./features/sprints/sprints.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: 'Sprints' },
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: 'Users' },
      },
      {
        path: 'audit',
        loadChildren: () => import('./features/audit/audit.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: 'Audit Log' },
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: 'Settings' },
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: 'Profile' },
      },
    ],
  },
];
