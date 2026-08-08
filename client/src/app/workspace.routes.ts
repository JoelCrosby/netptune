import { Routes } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';
import { assistantGuard } from './core/auth/assistant.guard';
import { authGuard } from './core/auth/auth.guard';
import { workspaceGuard } from './core/auth/workspace.guard';
import { workspaceResovler } from './core/resolvers/workspace-resolver';
import { BoardGroupsEffects } from './core/store/groups/board-groups.effects';
import { boardGroupsReducer } from './core/store/groups/board-groups.reducer';
import { hubContextReducer } from './core/store/hub-context/hub-context.reducer';
import { SprintsEffects } from './core/store/sprints/sprints.effects';
import { sprintsReducer } from './core/store/sprints/sprints.reducer';
import { TagsEffects } from './core/store/tags/tags.effects';
import { tagsReducer } from './core/store/tags/tags.reducer';
import { ProjectTasksEffects } from './core/store/tasks/tasks.effects';
import { projectTasksReducer } from './core/store/tasks/tasks.reducer';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    canActivate: [workspaceGuard],
    resolve: [workspaceResovler],
    providers: [
      provideState('tasks', projectTasksReducer),
      provideState('tags', tagsReducer),
      provideState('hub', hubContextReducer),
      provideState('sprints', sprintsReducer),
      provideState('boardgroups', boardGroupsReducer),
      provideEffects([
        ProjectTasksEffects,
        TagsEffects,
        BoardGroupsEffects,
        SprintsEffects,
      ]),
    ],
    loadComponent: () => import('./shell/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the workspace dashboard:Dashboard` },
      },
      {
        path: 'projects',
        loadChildren: () => import('./features/projects/projects.routes').then((m) => m.routes),
        data: { title: $localize`:Page title for the project list:Projects` },
      },
      {
        path: 'tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the task list:Tasks` },
      },
      {
        path: 'automations',
        loadChildren: () => import('./features/automations/automations.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the workspace automation rules:Automations` },
      },
      {
        path: 'boards',
        loadChildren: () => import('./features/boards/boards.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the kanban board list:Boards` },
      },
      {
        path: 'sprints',
        loadChildren: () => import('./features/sprints/sprints.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the sprint list:Sprints` },
      },
      {
        path: 'reports',
        loadChildren: () => import('./features/reporting/reporting.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the workspace reporting views:Reports` },
      },
      {
        path: 'roadmap',
        loadChildren: () => import('./features/roadmap/roadmap.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the roadmap timeline:Roadmap` },
      },
      {
        path: 'calendar',
        loadChildren: () => import('./features/calendar/calendar.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the calendar view:Calendar` },
      },
      {
        path: 'assistant',
        loadChildren: () => import('./features/assistant/assistant.routes').then((m) => m.routes),
        canActivate: [authGuard, assistantGuard],
        data: { title: $localize`:Page title for the assistant chat page:Assistant` },
      },
      {
        path: 'notifications',
        loadChildren: () => import('./features/notifications/notifications.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for the notification list:Notifications` },
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for the workspace member list:Users` },
      },
      {
        path: 'audit',
        loadChildren: () => import('./features/audit/audit.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for the workspace audit log:Audit Log` },
      },
      {
        path: 'storage',
        loadChildren: () => import('./features/storage/storage.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for uploaded file storage:Storage` },
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for workspace settings:Settings` },
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for the signed-in user profile:Profile` },
      },
    ],
  },
];
