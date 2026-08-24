import { Routes } from '@angular/router';
import { assistantGuard } from './core/auth/assistant.guard';
import { authGuard } from './core/auth/auth.guard';
import { workspaceGuard } from './core/auth/workspace.guard';
import { workspaceResovler } from './core/resolvers/workspace-resolver';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    canActivate: [workspaceGuard],
    resolve: [workspaceResovler],
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
        path: 'views',
        loadChildren: () => import('./features/task-views/task-views.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the saved task view list:Views` },
      },
      {
        path: 'pinned',
        loadChildren: () => import('./features/pins/pins.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the pinned task list:Pinned` },
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
